using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using static LiteSpec.Schemas.JsonSchemaSupport;
using SchemaEvaluationResults = Json.Schema.EvaluationResults;
using SchemaLibrary = Json.Schema.JsonSchema;
using SchemaOptions = Json.Schema.EvaluationOptions;
using SchemaOutput = Json.Schema.OutputFormat;
using SchemaPointer = Json.Pointer.JsonPointer;

namespace LiteSpec.Schemas;

/// <summary>
///     The validation path: each schema compiles once through the shared JSON
///     Schema library and each failing result renders in the house message
///     shape, so every validator reports the same text for the same mismatch.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class SchemaEvaluation
{
    /// <summary>Gets the options every evaluation runs under: the hierarchical output that carries each nested result.</summary>
    private static readonly SchemaOptions Hierarchical = new() { OutputFormat = SchemaOutput.Hierarchical };

    // The keywords whose parent error the walker skips: each applicator below
    // reports through its nested results, and each condition below judges a
    // branch rather than the value, so the walker descends instead of mapping.
    /// <summary>Gets the keywords the walker descends into instead of mapping at the parent.</summary>
    private static readonly HashSet<string> DescendOnly = new(
        ["properties", "patternProperties", "items", "prefixItems", "propertyNames", "dependentSchemas", "allOf", "additionalProperties", "if", "then", "else", "$ref", "$dynamicRef", "$recursiveRef"],
        StringComparer.Ordinal);

    // The keywords the walker renders as one message at the parent: the
    // composites name their branches, and the newer keywords keep the library
    // text, so the walker never descends into their nested results.
    /// <summary>Gets the keywords the walker maps at the parent without descending.</summary>
    private static readonly HashSet<string> RenderOnly = new(
        ["oneOf", "anyOf", "contains", "unevaluatedItems", "unevaluatedProperties", "contentSchema"],
        StringComparer.Ordinal);

    // The keywords whose nested results are never violations: a condition and
    // a negation judge a branch, so the walker never descends into them.
    /// <summary>Gets the keywords whose nested results the walker never descends into.</summary>
    private static readonly HashSet<string> NeverDescend = new(["if", "not"], StringComparer.Ordinal);

    // The positions holding one subschema: the library reports their node
    // with the parent relative path still attached, so the location carries
    // one stale segment that resolves against nothing. The tolerant lookup
    // below skips exactly that segment.
    /// <summary>Gets the positions holding one subschema, whose reported location carries one stale segment.</summary>
    private static readonly HashSet<string> SingleSubschema = new(SchemaGate.ChildValues, StringComparer.Ordinal);

    /// <summary>Gets the compiled schemas, each walked once per schema node.</summary>
    private static readonly ConditionalWeakTable<JsonNode, CompiledSchema> CompiledCache = new();

    /// <summary>One compiled schema: the library schema and the tree its locations resolve against.</summary>
    private sealed class CompiledSchema
    {
        /// <summary>Gets or sets the root the compilation was built for.</summary>
        public JsonNode? Root { get; set; }

        /// <summary>Gets or sets the tree the schema locations resolve against.</summary>
        public JsonNode? Effective { get; set; }

        /// <summary>Gets or sets the compiled library schema.</summary>
        public SchemaLibrary? Library { get; set; }
    }

    /// <summary>
    ///     Validates <paramref name="value"/> against <paramref name="schema"/>, appending
    ///     one <c>{location}: ...</c> message per violation to <paramref name="errors"/>.
    ///     A missing property is distinct from an explicit JSON null.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="schema">The schema to validate against.</param>
    /// <param name="root">The schema root.</param>
    /// <param name="location">The value location.</param>
    /// <param name="errors">The errors to append to.</param>
    internal static void Validate(JsonNode? value, JsonNode? schema, JsonNode root, string location, List<string> errors)
    {
        if (schema is not JsonObject)
        {
            return;
        }

        var (library, effective) = ForSchema(schema, root);

        using var document = JsonDocument.Parse(value?.ToJsonString() ?? "null");
        var results = library.Evaluate(document.RootElement, Hierarchical);

        if (!results.IsValid)
        {
            Collect(results, effective, value, location, errors);
        }
    }

    /// <summary>Compiles a schema against its root once and reuses the compilation.</summary>
    /// <param name="schema">The schema to compile.</param>
    /// <param name="root">The schema root.</param>
    /// <returns>The compiled library schema and the tree its locations resolve against.</returns>
    private static (SchemaLibrary Library, JsonNode Effective) ForSchema(JsonNode schema, JsonNode root)
    {
        var entry = CompiledCache.GetValue(schema, _ => new CompiledSchema());

        lock (entry)
        {
            var library = entry.Library;
            var effective = entry.Effective;

            if (library is null || effective is null || !ReferenceEquals(entry.Root, root))
            {
                effective = EffectiveTree(schema, root);
                // The library compiles the known keywords only: the gate reports each
                // unknown keyword beside the validation, so the library never judges one.
                library = SchemaLibrary.FromText(SchemaGate.WithoutUnknownKeywords(effective).ToJsonString());
                entry.Library = library;
                entry.Effective = effective;
                entry.Root = root;
            }

            return (library, effective);
        }
    }

    /// <summary>
    ///     The tree a schema validates against: the root itself, or a copy of a
    ///     subschema carrying the root definitions its references resolve
    ///     against. The copy leaves the caller trees untouched.
    /// </summary>
    /// <param name="schema">The schema to validate against.</param>
    /// <param name="root">The schema root.</param>
    /// <returns>The effective tree.</returns>
    private static JsonNode EffectiveTree(JsonNode schema, JsonNode root)
    {
        if (ReferenceEquals(schema, root))
        {
            return root;
        }

        var clone = (JsonObject)schema.DeepClone();

        if (!clone.ContainsKey("$defs") && root is JsonObject rootObject &&
            rootObject.TryGetPropertyValue("$defs", out var defs) && defs is not null)
        {
            clone["$defs"] = defs.DeepClone();
        }

        return clone;
    }

    /// <summary>Renders each violation of a failing result in the house message shape.</summary>
    /// <param name="node">The failing result.</param>
    /// <param name="effective">The tree the schema locations resolve against.</param>
    /// <param name="value">The validated value.</param>
    /// <param name="location">The value location.</param>
    /// <param name="errors">The errors to append to.</param>
    private static void Collect(SchemaEvaluationResults node, JsonNode effective, JsonNode? value, string location, List<string> errors)
    {
        if (node.IsValid)
        {
            return;
        }

        var path = node.EvaluationPath.ToString();
        var rendered = new HashSet<string>(StringComparer.Ordinal);

        if (node.Errors is not null)
        {
            foreach (var (keyword, text) in node.Errors)
            {
                if (RenderOnly.Contains(keyword))
                {
                    AddComposite(node, keyword, text, path, effective, value, location, errors);
                    rendered.Add(keyword);
                }
                else if (!DescendOnly.Contains(keyword))
                {
                    AddLeaf(node, keyword, text, effective, value, location, errors);
                }
            }
        }

        if (node.Details is null)
        {
            return;
        }

        foreach (var child in node.Details)
        {
            if (child.IsValid || UnderSkipped(child, path, rendered))
            {
                continue;
            }

            Collect(child, effective, value, location, errors);
        }
    }

    /// <summary>Reports whether a nested result sits under a keyword the walker never descends into.</summary>
    /// <param name="child">The nested result.</param>
    /// <param name="path">The evaluation path of the parent result.</param>
    /// <param name="rendered">The keywords the parent rendered as one message.</param>
    /// <returns>True when the nested result is skipped.</returns>
    private static bool UnderSkipped(SchemaEvaluationResults child, string path, HashSet<string> rendered)
    {
        foreach (var keyword in NeverDescend)
        {
            if (Under(child, path, keyword))
            {
                return true;
            }
        }

        foreach (var keyword in rendered)
        {
            if (Under(child, path, keyword))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Reports whether a nested result sits at or under one keyword of its parent result.</summary>
    /// <param name="child">The nested result.</param>
    /// <param name="path">The evaluation path of the parent result.</param>
    /// <param name="keyword">The keyword to test.</param>
    /// <returns>True when the nested result sits at or under the keyword.</returns>
    private static bool Under(SchemaEvaluationResults child, string path, string keyword)
    {
        var basePath = $"{path}/{keyword}";
        var childPath = child.EvaluationPath.ToString();
        return childPath == basePath || childPath.StartsWith(basePath + "/", StringComparison.Ordinal);
    }

    /// <summary>Renders one keyword the walker maps at the parent without descending.</summary>
    /// <param name="node">The failing result.</param>
    /// <param name="keyword">The keyword to render.</param>
    /// <param name="text">The library message, kept for the keywords with no house shape.</param>
    /// <param name="path">The evaluation path of the result.</param>
    /// <param name="effective">The tree the schema locations resolve against.</param>
    /// <param name="value">The validated value.</param>
    /// <param name="location">The value location.</param>
    /// <param name="errors">The errors to append to.</param>
    private static void AddComposite(SchemaEvaluationResults node, string keyword, string text, string path, JsonNode effective, JsonNode? value, string location, List<string> errors)
    {
        var prefix = location + node.InstanceLocation.ToString();

        if (keyword is "oneOf" or "anyOf")
        {
            AddShapes(node, keyword, path, location, prefix, effective, value, errors);
            return;
        }

        if (keyword == "contains")
        {
            AddContains(node, path, prefix, text, effective, errors);
            return;
        }

        errors.Add($"{prefix}: {text}");
    }

    /// <summary>
    ///     Renders a failed composition as one message. Exactly one branch
    ///     accepts the value for <c>oneOf</c>, and at least one for
    ///     <c>anyOf</c>. The count alone tells an author that the value fits
    ///     no shape without telling them why any shape refused it, so the
    ///     message carries the first reason from each rejected branch.
    /// </summary>
    /// <param name="node">The failing result.</param>
    /// <param name="keyword">The composition keyword.</param>
    /// <param name="path">The evaluation path of the result.</param>
    /// <param name="location">The value location.</param>
    /// <param name="prefix">The message prefix.</param>
    /// <param name="effective">The tree the schema locations resolve against.</param>
    /// <param name="value">The validated value.</param>
    /// <param name="errors">The errors to append to.</param>
    private static void AddShapes(SchemaEvaluationResults node, string keyword, string path, string location, string prefix, JsonNode effective, JsonNode? value, List<string> errors)
    {
        var branches = new List<SchemaEvaluationResults>();

        if (node.Details is not null)
        {
            foreach (var child in node.Details)
            {
                if (Under(child, path, keyword))
                {
                    branches.Add(child);
                }
            }
        }

        var matched = branches.Count(branch => branch.IsValid);

        if (keyword == "oneOf")
        {
            var message = $"value matches {matched} of {branches.Count} allowed shapes, expected exactly 1";

            if (matched == 0)
            {
                var rejections = BranchReasons(branches, effective, value, location);

                if (rejections.Count > 0)
                {
                    message += $" ({string.Join("; ", rejections)})";
                }
            }

            errors.Add($"{prefix}: {message}");
            return;
        }

        var reasons = BranchReasons(branches, effective, value, location);
        errors.Add($"{prefix}: value matches none of the {branches.Count} allowed shapes ({string.Join("; ", reasons)})");
    }

    /// <summary>Renders the first reason from each rejected composition branch.</summary>
    /// <param name="branches">The branch results.</param>
    /// <param name="effective">The tree the schema locations resolve against.</param>
    /// <param name="value">The validated value.</param>
    /// <param name="location">The value location.</param>
    /// <returns>The first reason of each rejected branch.</returns>
    private static List<string> BranchReasons(List<SchemaEvaluationResults> branches, JsonNode effective, JsonNode? value, string location)
    {
        var rejections = new List<string>();

        for (var index = 0; index < branches.Count; index++)
        {
            if (branches[index].IsValid)
            {
                continue;
            }

            var branchErrors = new List<string>();
            Collect(branches[index], effective, value, location, branchErrors);

            if (branchErrors.Count > 0)
            {
                rejections.Add($"shape {index + 1}: {StripLocationPrefix(branchErrors[0], location)}");
            }
        }

        return rejections;
    }

    /// <summary>Renders a failed <c>contains</c> as one message over its matching items.</summary>
    /// <param name="node">The failing result.</param>
    /// <param name="path">The evaluation path of the result.</param>
    /// <param name="prefix">The message prefix.</param>
    /// <param name="text">The library message, kept when the bounds do not read.</param>
    /// <param name="effective">The tree the schema locations resolve against.</param>
    /// <param name="errors">The errors to append to.</param>
    private static void AddContains(SchemaEvaluationResults node, string path, string prefix, string text, JsonNode effective, List<string> errors)
    {
        var schema = SchemaAt(node, effective);

        if (schema is null)
        {
            errors.Add($"{prefix}: {text}");
            return;
        }

        var minimum = 1.0;
        double? maximum = null;

        if (schema.TryGetPropertyValue("minContains", out var minNode) && minNode is not null)
        {
            if (!TrySchemaNumber(minNode, out minimum))
            {
                errors.Add($"{prefix}: {text}");
                return;
            }

            minimum = Math.Truncate(minimum);
        }

        if (schema.TryGetPropertyValue("maxContains", out var maxNode) && maxNode is not null)
        {
            if (!TrySchemaNumber(maxNode, out var ceiling))
            {
                errors.Add($"{prefix}: {text}");
                return;
            }

            maximum = Math.Truncate(ceiling);
        }

        var found = 0;

        if (node.Details is not null)
        {
            foreach (var child in node.Details)
            {
                if (child.IsValid && Under(child, path, "contains"))
                {
                    found++;
                }
            }
        }

        if (found < minimum)
        {
            errors.Add(minimum == 1 && maximum is null
                ? $"{prefix}: array contains no item matching the required shape"
                : $"{prefix}: array contains fewer than {JsNumber(minimum)} matching items");
        }
        else if (maximum.HasValue && found > maximum.Value)
        {
            errors.Add($"{prefix}: array contains more than {JsNumber(maximum.Value)} matching items");
        }
        else
        {
            errors.Add($"{prefix}: {text}");
        }
    }

    /// <summary>Renders one keyword failure in the house message shape.</summary>
    /// <param name="node">The failing result.</param>
    /// <param name="keyword">The keyword to render.</param>
    /// <param name="text">The library message, kept when the house shape does not read.</param>
    /// <param name="effective">The tree the schema locations resolve against.</param>
    /// <param name="value">The validated value.</param>
    /// <param name="location">The value location.</param>
    /// <param name="errors">The errors to append to.</param>
    private static void AddLeaf(SchemaEvaluationResults node, string keyword, string text, JsonNode effective, JsonNode? value, string location, List<string> errors)
    {
        var prefix = location + node.InstanceLocation.ToString();

        if (keyword == "not")
        {
            errors.Add($"{prefix}: value matches prohibited schema");
            return;
        }

        if (keyword == "enum")
        {
            errors.Add($"{prefix}: value is outside the allowed enum");
            return;
        }

        if (keyword == "uniqueItems")
        {
            errors.Add($"{prefix}: array items are not unique");
            return;
        }

        if (keyword.Length == 0)
        {
            AddBooleanSchema(node, prefix, text, location, errors);
            return;
        }

        var schema = SchemaAt(node, effective);

        if (schema is null)
        {
            errors.Add($"{prefix}: {text}");
            return;
        }

        switch (keyword)
        {
            case "type":
                AddType(schema, prefix, text, errors);
                break;
            case "const":
                if (schema.TryGetPropertyValue("const", out var constant))
                {
                    errors.Add($"{prefix}: expected constant {Stringify(constant)}");
                }
                else
                {
                    errors.Add($"{prefix}: {text}");
                }

                break;
            case "pattern":
                if (schema.TryGetPropertyValue("pattern", out var patternNode) && patternNode is JsonValue patternValue &&
                    patternValue.TryGetValue<string>(out var pattern) && pattern is not null)
                {
                    errors.Add($"{prefix}: string does not match {pattern}");
                }
                else
                {
                    errors.Add($"{prefix}: {text}");
                }

                break;
            case "required":
                AddRequired(schema, InstanceAt(node, value), prefix, text, errors);
                break;
            case "dependentRequired":
                AddDependentRequired(schema, InstanceAt(node, value), prefix, text, errors);
                break;
            case "minLength":
            case "maxLength":
            case "minimum":
            case "maximum":
            case "exclusiveMinimum":
            case "exclusiveMaximum":
            case "multipleOf":
            case "minItems":
            case "maxItems":
            case "minProperties":
            case "maxProperties":
                if (schema.TryGetPropertyValue(keyword, out var boundNode) && TrySchemaNumber(boundNode, out var bound) &&
                    BoundMessage(keyword, bound) is string message)
                {
                    errors.Add($"{prefix}: {message}");
                }
                else
                {
                    errors.Add($"{prefix}: {text}");
                }

                break;
            default:
                errors.Add($"{prefix}: {text}");
                break;
        }
    }

    /// <summary>Renders a failed <c>type</c> with the expected name or names.</summary>
    /// <param name="schema">The failing schema.</param>
    /// <param name="prefix">The message prefix.</param>
    /// <param name="text">The library message, kept when the names do not read.</param>
    /// <param name="errors">The errors to append to.</param>
    private static void AddType(JsonObject schema, string prefix, string text, List<string> errors)
    {
        if (!schema.TryGetPropertyValue("type", out var typeNode))
        {
            errors.Add($"{prefix}: {text}");
            return;
        }

        if (typeNode is JsonValue typeValue && typeValue.TryGetValue<string>(out var typeName) && typeName is not null)
        {
            errors.Add($"{prefix}: expected {typeName}");
            return;
        }

        if (typeNode is JsonArray union)
        {
            var names = new List<string>();

            foreach (var entry in union)
            {
                if (entry is JsonValue nameValue && nameValue.TryGetValue<string>(out var name) && name is not null)
                {
                    names.Add(name);
                }
                else
                {
                    errors.Add($"{prefix}: {text}");
                    return;
                }
            }

            if (names.Count > 0)
            {
                errors.Add($"{prefix}: expected {string.Join(" or ", names)}");
                return;
            }
        }

        errors.Add($"{prefix}: {text}");
    }

    /// <summary>Renders each missing required property of a failed <c>required</c>.</summary>
    /// <param name="schema">The failing schema.</param>
    /// <param name="instance">The failing value.</param>
    /// <param name="prefix">The message prefix.</param>
    /// <param name="text">The library message, kept when the names do not read.</param>
    /// <param name="errors">The errors to append to.</param>
    private static void AddRequired(JsonObject schema, JsonNode? instance, string prefix, string text, List<string> errors)
    {
        if (schema.TryGetPropertyValue("required", out var requiredNode) && requiredNode is JsonArray names && instance is JsonObject fields)
        {
            var missing = 0;

            foreach (var name in names)
            {
                var property = RequiredName(name);

                if (!fields.ContainsKey(property))
                {
                    errors.Add($"{prefix}: missing required property '{property}'");
                    missing++;
                }
            }

            if (missing > 0)
            {
                return;
            }
        }

        errors.Add($"{prefix}: {text}");
    }

    /// <summary>Renders each missing dependent of a failed <c>dependentRequired</c>.</summary>
    /// <param name="schema">The failing schema.</param>
    /// <param name="instance">The failing value.</param>
    /// <param name="prefix">The message prefix.</param>
    /// <param name="text">The library message, kept when the names do not read.</param>
    /// <param name="errors">The errors to append to.</param>
    private static void AddDependentRequired(JsonObject schema, JsonNode? instance, string prefix, string text, List<string> errors)
    {
        if (schema.TryGetPropertyValue("dependentRequired", out var dependentNode) && dependentNode is JsonObject triggers && instance is JsonObject fields)
        {
            var missing = 0;

            foreach (var (trigger, dependents) in triggers)
            {
                if (!fields.ContainsKey(trigger) || dependents is not JsonArray needs)
                {
                    continue;
                }

                foreach (var dependent in needs)
                {
                    var property = RequiredName(dependent);

                    if (!fields.ContainsKey(property))
                    {
                        errors.Add($"{prefix}: property '{trigger}' requires '{property}'");
                        missing++;
                    }
                }
            }

            if (missing > 0)
            {
                return;
            }
        }

        errors.Add($"{prefix}: {text}");
    }

    /// <summary>Renders a failed boolean schema: only <c>additionalProperties: false</c> names its property.</summary>
    /// <param name="node">The failing result.</param>
    /// <param name="prefix">The message prefix.</param>
    /// <param name="text">The library message, kept when the property does not read.</param>
    /// <param name="location">The value location.</param>
    /// <param name="errors">The errors to append to.</param>
    private static void AddBooleanSchema(SchemaEvaluationResults node, string prefix, string text, string location, List<string> errors)
    {
        // Every other boolean subschema constrained nothing in the previous
        // check, so it still reports nothing.
        var path = node.EvaluationPath.ToString();
        var slash = path.LastIndexOf('/');
        var last = slash < 0 ? path : path[(slash + 1)..];

        if (last != "additionalProperties")
        {
            return;
        }

        var head = slash < 0 ? "" : path[..slash];
        var previousSlash = head.LastIndexOf('/');
        var previous = previousSlash < 0 ? head : head[(previousSlash + 1)..];

        if (previous is "properties" or "patternProperties" or "dependentSchemas")
        {
            return;
        }

        var instancePath = node.InstanceLocation.ToString();
        var propSlash = instancePath.LastIndexOf('/');
        var encoded = propSlash < 0 ? instancePath : instancePath[(propSlash + 1)..];

        if (encoded.Length == 0)
        {
            errors.Add($"{prefix}: {text}");
            return;
        }

        var property = encoded.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);
        var parent = propSlash < 0 ? "" : instancePath[..propSlash];
        errors.Add($"{location + parent}: unknown property '{property}'");
    }

    /// <summary>Renders one bound keyword in the house message shape.</summary>
    /// <param name="keyword">The bound keyword.</param>
    /// <param name="bound">The bound.</param>
    /// <returns>The message, or null when the keyword holds no bound.</returns>
    private static string? BoundMessage(string keyword, double bound) => keyword switch
    {
        "minLength" => $"string is shorter than {JsNumber(bound)}",
        "maxLength" => $"string is longer than {JsNumber(bound)}",
        "minimum" => $"number is below {JsNumber(bound)}",
        "maximum" => $"number is above {JsNumber(bound)}",
        "exclusiveMinimum" => $"number is not above {JsNumber(bound)}",
        "exclusiveMaximum" => $"number is not below {JsNumber(bound)}",
        "multipleOf" => $"number is not a multiple of {JsNumber(bound)}",
        "minItems" => $"array has fewer than {JsNumber(bound)} items",
        "maxItems" => $"array has more than {JsNumber(bound)} items",
        "minProperties" => $"object has fewer than {JsNumber(bound)} properties",
        "maxProperties" => $"object has more than {JsNumber(bound)} properties",
        _ => null,
    };

    /// <summary>Reads the failing schema of a result through its schema location.</summary>
    /// <param name="node">The failing result.</param>
    /// <param name="effective">The tree the schema locations resolve against.</param>
    /// <returns>The failing schema, or null when it does not resolve.</returns>
    private static JsonObject? SchemaAt(SchemaEvaluationResults node, JsonNode effective)
    {
        var fragment = SchemaFragment(node);

        if (SchemaPointer.TryParse(fragment, out var pointer) && pointer.TryEvaluate(effective, out var found))
        {
            return found as JsonObject;
        }

        return ResolveTolerant(fragment, effective);
    }

    /// <summary>Reads a schema through a location carrying the stale single-subschema segment.</summary>
    /// <param name="fragment">The schema location fragment.</param>
    /// <param name="effective">The tree the schema locations resolve against.</param>
    /// <returns>The failing schema, or null when it does not resolve.</returns>
    private static JsonObject? ResolveTolerant(string fragment, JsonNode effective)
    {
        var path = fragment.StartsWith('#') ? fragment[1..] : fragment;

        if (path.Length == 0 || path[0] != '/')
        {
            return null;
        }

        JsonNode? current = effective;
        string? previous = null;
        var segments = path.Split('/');

        for (var index = 1; index < segments.Length; index++)
        {
            var key = segments[index].Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);
            JsonNode? next;

            if (current is JsonObject fields)
            {
                next = fields.TryGetPropertyValue(key, out var child) ? child : null;
            }
            else if (current is JsonArray items && SchemaReferences.TryArrayIndex(key, items, out var element))
            {
                next = element;
            }
            else
            {
                next = null;
            }

            if (next is null)
            {
                if (previous is null || !SingleSubschema.Contains(previous))
                {
                    return null;
                }

                previous = null;
                continue;
            }

            current = next;
            previous = key;
        }

        return current as JsonObject;
    }

    /// <summary>Reads the schema location fragment of a result, falling back to the root.</summary>
    /// <param name="node">The failing result.</param>
    /// <returns>The fragment, or the empty pointer when it does not read.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "SchemaFragment falls back to the schema root when the library cannot build a schema location, so one exotic schema never fails the whole validation run.")]
    private static string SchemaFragment(SchemaEvaluationResults node)
    {
        try
        {
            return node.SchemaLocation.Fragment;
        }
        catch (Exception)
        {
            return "";
        }
    }

    /// <summary>Reads the failing value of a result through its instance location.</summary>
    /// <param name="node">The failing result.</param>
    /// <param name="value">The validated value.</param>
    /// <returns>The failing value, or null when it does not resolve.</returns>
    private static JsonNode? InstanceAt(SchemaEvaluationResults node, JsonNode? value) =>
        node.InstanceLocation.TryEvaluate(value, out var found) ? found : null;
}
