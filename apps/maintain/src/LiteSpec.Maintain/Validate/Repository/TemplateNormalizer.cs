using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Standards;
using static LiteSpec.Maintain.Validate.Repository.ManifestValidator;

namespace LiteSpec.Maintain.Validate.Repository;

/// <summary>Fills a consumer template with values its schema accepts: each placeholder becomes an example of its field type, removable placeholders are dropped, and conditional branches are chosen, so the template can be validated as a record.</summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class TemplateNormalizer
{
    /// <summary>Reports whether a template value is a placeholder.</summary>
    /// <param name="value">The template value.</param>
    /// <returns>True when the value is a placeholder.</returns>
    internal static bool IsTemplatePlaceholder(string? value) =>
        value is not null && (value.Contains('<') || value.Contains("__"));

    // A removable placeholder tells the author the line may go. It drops unless the
    // schema requires it, in which case it fills like any other placeholder.
    /// <summary>Reports whether a template value is a removable placeholder.</summary>
    /// <param name="value">The template value.</param>
    /// <returns>True when the value is a removable placeholder.</returns>
    private static bool IsRemovablePlaceholder(string value) =>
        IsTemplatePlaceholder(value) && (value.Contains("remove this ") || value.Contains("remove the "));

    // A bare removal note carries no placeholder form, so it always drops. It sits
    // beside a worked example in the same array.
    /// <summary>Reports whether a template value is a drop element.</summary>
    /// <param name="value">The template value.</param>
    /// <returns>True when the value always drops.</returns>
    private static bool IsTemplateDropElement(string value) =>
        !IsTemplatePlaceholder(value) && (value.Contains("remove this ") || value.Contains("remove the "));

    // One worked example per schema definition whose values follow a pattern no
    // generic word meets: identifiers, dates, durations, and the fixed vocabularies.
    // (litespec/core/authoring#run-repeatable-authoring-checks)
    /// <summary>Gets the worked example for each schema definition whose values follow a fixed pattern.</summary>
    private static readonly Dictionary<string, string> TemplateExamples = new(StringComparer.Ordinal)
    {
        ["isoDate"] = "2026-11-30",
        ["isoDuration"] = "P30D",
        ["nonEmptyString"] = "sample",
        ["productId"] = "product/sample",
        ["goalId"] = "goal/sample",
        ["visionId"] = "vision/sample",
        ["constraintId"] = "constraint/sample",
        ["researchId"] = "research/sample",
        ["decisionId"] = "decision/0001-sample",
        ["problemId"] = "problem/sample",
        ["featureId"] = "feature/sample",
        ["requirementId"] = "requirement/sample.entry",
        ["actorId"] = "actor/sample",
        ["roleId"] = "role/sample",
        ["permissionId"] = "permission/sample",
        ["journeyId"] = "journey/sample",
        ["journeyStepId"] = "journey/sample#s1",
        ["moduleId"] = "module/sample",
        ["aggregateId"] = "aggregate/sample.entry",
        ["ruleId"] = "rule/sample.entry",
        ["useCaseId"] = "use-case/sample.entry",
        ["resultId"] = "result/sample.entry",
        ["derivedResultId"] = "result/sample.forbidden",
        ["scenarioId"] = "scenario/sample.entry",
        ["eventId"] = "event/sample.entry",
        ["entryPointId"] = "entry-point/sample",
        ["screenId"] = "screen/sample.entry",
        ["externalSystemId"] = "external-system/sample",
        ["externalOperationRef"] = "external-system/sample#operation",
        ["reactionId"] = "reaction/sample.entry",
        ["jobId"] = "job/sample.entry",
        ["notificationId"] = "notification/sample.entry",
        ["settingId"] = "setting/sample.entry",
        ["integrationEventId"] = "integration-event/sample",
        ["runbookId"] = "runbook/sample",
        ["departureId"] = "departure/sample",
        ["termId"] = "term/sample",
        ["refusalCode"] = "sample.entry",
        ["standardsProvisionId"] = "litespec/core/scope#keep-one-bounded-context",
        ["genericRecordId"] = "sample/entry",
        ["grantValue"] = "permission/sample",
        ["configurationKey"] = "Sample:Key",
    };

    // The fallback pool for an inline pattern the map names no definition for. The
    // first candidate the pattern accepts wins; a pattern no candidate meets keeps
    // the generic word and reports.
    /// <summary>Gets the fallback example pool for an inline pattern.</summary>
    private static readonly string[] TemplatePatternPool =
        ["sample", .. TemplateExamples.Values, "s1", "P30D", "nuget:Sample", "Sample:Key"];

    /// <summary>Finds a worked example that a pattern accepts.</summary>
    /// <param name="pattern">The pattern to satisfy.</param>
    /// <returns>The first accepted example.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "TemplatePatternExample tries candidate examples against an arbitrary schema pattern and falls back to the generic word when the pattern cannot run, instead of crashing template validation.")]
    private static string TemplatePatternExample(string pattern)
    {
        foreach (var candidate in TemplatePatternPool)
        {
            try
            {
                if (Regex.IsMatch(candidate, pattern))
                {
                    return candidate;
                }
            }
            catch (Exception)
            {
                break;
            }
        }

        return "sample";
    }

    /// <summary>Resolves a template schema reference to its definition.</summary>
    /// <param name="schema">The schema to resolve.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <returns>The resolved schema and its definition name.</returns>
    private static (JsonObject? Resolved, string? DefName) ResolveTemplateRef(JsonNode? schema, JsonObject defs)
    {
        const string prefix = "#/$defs/";
        var current = schema;
        string? defName = null;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (current is JsonObject obj
            && JsonString(obj["$ref"]) is string reference
            && reference.StartsWith(prefix, StringComparison.Ordinal)
            && visited.Add(reference))
        {
            defName = reference[prefix.Length..];
            current = defs[defName];
        }

        return (current as JsonObject, defName);
    }

    /// <summary>Normalizes one template value against its schema.</summary>
    /// <param name="value">The template value.</param>
    /// <param name="schema">The value schema.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="root">The schema root.</param>
    internal static void NormalizeTemplateNode(JsonNode? value, JsonNode? schema, JsonObject defs, JsonNode root)
    {
        var (resolved, _) = ResolveTemplateRef(schema, defs);
        if (resolved is null)
        {
            return;
        }

        if (value is JsonObject obj)
        {
            NormalizeTemplateObject(obj, resolved, defs, root);
        }
        else if (value is JsonArray array)
        {
            NormalizeTemplateArray(array, resolved, defs, root);
        }
    }

    /// <summary>Normalizes one template object against its schema.</summary>
    /// <param name="obj">The template object.</param>
    /// <param name="schema">The object schema.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="root">The schema root.</param>
    private static void NormalizeTemplateObject(JsonObject obj, JsonObject schema, JsonObject defs, JsonNode root)
    {
        // An entry reads inside one branch of a shared list, which carries no shape
        // of its own, so the branch follows the entry's own type. A schema with its
        // own properties keeps them: its branches only constrain.
        if (schema["properties"] is null
            && (schema["oneOf"] as JsonArray ?? schema["anyOf"] as JsonArray) is JsonArray branches
            && branches.Count > 0)
        {
            var selected = SelectTemplateBranch(obj, branches, defs);
            var (resolved, _) = ResolveTemplateRef(selected, defs);
            if (resolved is not null)
            {
                schema = resolved;
            }
        }

        var properties = schema["properties"] as JsonObject;
        var additional = schema["additionalProperties"] as JsonObject;

        // Children normalize before their parent's enum and removal choices, so a
        // conditional rule reads the values it judges rather than placeholders.
        foreach (var key in obj.Select(entry => entry.Key).ToList())
        {
            if (obj[key] is not JsonObject && obj[key] is not JsonArray)
            {
                continue;
            }

            var propSchema = properties?[key];
            if (propSchema is null)
            {
                if (additional is not null && obj[key] is JsonObject)
                {
                    NormalizeTemplateNode(obj[key], additional, defs, root);
                }

                continue;
            }

            NormalizeTemplateNode(obj[key], propSchema, defs, root);
        }

        // An enum placeholder takes the first value under which the object's
        // conditional rules accept the object, so a template that teaches one shape
        // reads under the value that shape needs.
        var enums = new List<string>();
        foreach (var key in obj.Select(entry => entry.Key).ToList())
        {
            if (obj[key] is not JsonValue || JsonString(obj[key]) is not string text || !IsTemplatePlaceholder(text))
            {
                continue;
            }

            var (resolved, _) = ResolveTemplateRef(properties?[key], defs);
            if (resolved is null)
            {
                continue;
            }

            if (resolved["enum"] as JsonArray is { Count: > 0 })
            {
                enums.Add(key);
            }
            else if (!IsRemovablePlaceholder(text))
            {
                obj[key] = SubstituteTemplateScalar(resolved, defs, root);
            }
        }

        foreach (var key in enums)
        {
            var (resolved, _) = ResolveTemplateRef(properties?[key], defs);
            var entries = (resolved?["enum"] as JsonArray)?.Where(entry => entry is not null).ToList();
            if (entries is null || entries.Count == 0)
            {
                continue;
            }

            var picked = entries[0]!.DeepClone();
            foreach (var candidate in entries)
            {
                obj[key] = candidate!.DeepClone();
                if (TemplateAcceptsConditionals(obj, key, schema, root))
                {
                    picked = candidate.DeepClone();
                    break;
                }
            }

            obj[key] = picked;
        }

        foreach (var key in obj.Select(entry => entry.Key).ToList())
        {
            if (obj[key] is not JsonValue || JsonString(obj[key]) is not string text || !IsRemovablePlaceholder(text))
            {
                continue;
            }

            var propSchema = properties?[key];
            if (propSchema is null)
            {
                continue;
            }

            var (resolved, _) = ResolveTemplateRef(propSchema, defs);
            if (resolved?["enum"] as JsonArray is { Count: > 0 })
            {
                continue;
            }

            if (IsTemplateRequired(schema, key, obj, root))
            {
                obj[key] = SubstituteTemplateScalar(resolved, defs, root);
            }
            else
            {
                obj.Remove(key);
            }
        }
    }

    /// <summary>Normalizes one template array against its schema.</summary>
    /// <param name="array">The template array.</param>
    /// <param name="schema">The array schema.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="root">The schema root.</param>
    private static void NormalizeTemplateArray(JsonArray array, JsonObject schema, JsonObject defs, JsonNode root)
    {
        var items = schema["items"];
        if (items is null)
        {
            return;
        }

        var minItems = schema["minItems"] is JsonValue count && count.TryGetValue<int>(out var minimum) ? minimum : 0;
        var dropped = new List<int>();
        for (var index = array.Count - 1; index >= 0; index--)
        {
            if (array[index] is JsonValue && JsonString(array[index]) is string text)
            {
                if (IsTemplateDropElement(text))
                {
                    array.RemoveAt(index);
                }
                else if (IsTemplatePlaceholder(text))
                {
                    if (IsRemovablePlaceholder(text))
                    {
                        dropped.Add(index);
                        array.RemoveAt(index);
                    }
                    else
                    {
                        array[index] = SubstituteTemplateScalar(items, defs, root);
                    }
                }
            }
            else
            {
                NormalizeTemplateNode(array[index], items, defs, root);
            }
        }

        // A dropped element returns when the array would fall below its minimum, so
        // an optional example still teaches the shape the schema insists on.
        dropped.Reverse();
        foreach (var index in dropped)
        {
            if (array.Count >= minItems)
            {
                break;
            }

            array.Insert(Math.Min(index, array.Count), SubstituteTemplateScalar(items, defs, root));
        }
    }

    /// <summary>Substitutes a valid scalar example for a template placeholder.</summary>
    /// <param name="schema">The scalar schema.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="root">The schema root.</param>
    /// <returns>The substitute value.</returns>
    private static JsonNode SubstituteTemplateScalar(JsonNode? schema, JsonObject defs, JsonNode root) =>
        SubstituteTemplateScalar(schema, defs, root, new HashSet<string>(StringComparer.Ordinal));

    /// <summary>Substitutes a valid scalar example for a template placeholder.</summary>
    /// <param name="schema">The scalar schema.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <param name="root">The schema root.</param>
    /// <param name="visited">The definitions already visited.</param>
    /// <returns>The substitute value.</returns>
    private static JsonNode SubstituteTemplateScalar(JsonNode? schema, JsonObject defs, JsonNode root, HashSet<string> visited)
    {
        const string prefix = "#/$defs/";
        var current = schema;
        string? defName = null;
        while (current is JsonObject obj
            && JsonString(obj["$ref"]) is string reference
            && reference.StartsWith(prefix, StringComparison.Ordinal)
            && visited.Add(reference))
        {
            defName = reference[prefix.Length..];
            current = defs[defName];
        }

        if (current is not JsonObject resolved)
        {
            return JsonValue.Create("sample")!;
        }

        if (resolved.TryGetPropertyValue("const", out var constant) && constant is not null)
        {
            return constant.DeepClone();
        }

        if (resolved["enum"] as JsonArray is { Count: > 0 } entries && entries[0] is not null)
        {
            return entries[0]!.DeepClone();
        }

        if ((resolved["oneOf"] as JsonArray ?? resolved["anyOf"] as JsonArray) is JsonArray branches && branches.Count > 0)
        {
            return SubstituteTemplateScalar(SelectTemplateStringBranch(branches, defs), defs, root, visited);
        }

        var type = JsonString(resolved["type"]);
        if (type is "integer" or "number")
        {
            var value = 1;
            if (resolved["minimum"] is JsonValue floor && floor.TryGetValue<int>(out var lowest))
            {
                value = lowest;
            }

            if (resolved["maximum"] is JsonValue ceiling && ceiling.TryGetValue<int>(out var highest) && value > highest)
            {
                value = highest;
            }

            // The schema check reads every number from its JSON text, where 1
            // and 1.0 validate the same, so the substitute stays a double.
            return JsonValue.Create((double)value)!;
        }

        if (type == "boolean")
        {
            return JsonValue.Create(true)!;
        }

        if (type == "array")
        {
            var items = resolved["items"];
            var least = resolved["minItems"] is JsonValue count && count.TryGetValue<int>(out var fewest) ? Math.Max(fewest, 1) : 1;
            var filled = new JsonArray();
            for (var index = 0; index < least; index++)
            {
                filled.Add(SubstituteTemplateScalar(items, defs, root, visited));
            }

            return filled;
        }

        if (type == "object")
        {
            return new JsonObject();
        }

        if (defName is not null && TemplateExamples.TryGetValue(defName, out var example))
        {
            return JsonValue.Create(example)!;
        }

        if (JsonString(resolved["pattern"]) is string pattern)
        {
            return JsonValue.Create(TemplatePatternExample(pattern))!;
        }

        return JsonValue.Create("sample")!;
    }

    // The branch an object entry reads under: its own type const first, then the
    // first object shape whose required keys the value carries, then the first
    // object shape. A value that fits none still normalizes under the first branch
    // and reports.
    /// <summary>Selects the branch a template object entry reads under.</summary>
    /// <param name="obj">The template object.</param>
    /// <param name="branches">The candidate branches.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <returns>The selected branch.</returns>
    private static JsonNode? SelectTemplateBranch(JsonObject obj, JsonArray branches, JsonObject defs)
    {
        var type = JsonString(obj["type"]);
        if (type is not null)
        {
            foreach (var branch in branches)
            {
                var (resolved, _) = ResolveTemplateRef(branch, defs);
                if ((resolved?["properties"] as JsonObject)?["type"] is JsonObject typeSchema
                    && JsonString(typeSchema["const"]) == type)
                {
                    return branch;
                }
            }
        }

        JsonNode? fallback = null;
        foreach (var branch in branches)
        {
            var (resolved, _) = ResolveTemplateRef(branch, defs);
            if (resolved is null || TemplateBranchIsScalar(resolved))
            {
                continue;
            }

            fallback ??= branch;
            var required = resolved["required"] as JsonArray;
            if (required is null || required.All(node => JsonString(node) is string name && obj.ContainsKey(name)))
            {
                return branch;
            }
        }

        return fallback ?? branches[0];
    }

    /// <summary>Reports whether a template branch schema is scalar.</summary>
    /// <param name="schema">The branch schema.</param>
    /// <returns>True when the branch is scalar.</returns>
    private static bool TemplateBranchIsScalar(JsonObject schema)
    {
        if (schema.TryGetPropertyValue("const", out _)
            || schema["enum"] is JsonArray
            || JsonString(schema["pattern"]) is not null)
        {
            return true;
        }

        return JsonString(schema["type"]) is string type && type is "string" or "integer" or "number" or "boolean";
    }

    /// <summary>Selects the string branch of a template scalar schema.</summary>
    /// <param name="branches">The candidate branches.</param>
    /// <param name="defs">The schema definitions.</param>
    /// <returns>The selected branch.</returns>
    private static JsonNode? SelectTemplateStringBranch(JsonArray branches, JsonObject defs)
    {
        foreach (var branch in branches)
        {
            var (resolved, _) = ResolveTemplateRef(branch, defs);
            if (resolved is null)
            {
                continue;
            }

            if (resolved["enum"] as JsonArray is { Count: > 0 }
                || resolved.TryGetPropertyValue("const", out _)
                || JsonString(resolved["pattern"]) is not null
                || JsonString(resolved["type"]) == "string")
            {
                return branch;
            }
        }

        foreach (var branch in branches)
        {
            var (resolved, _) = ResolveTemplateRef(branch, defs);
            if (resolved is null || TemplateBranchIsScalar(resolved))
            {
                continue;
            }

            return branch;
        }

        return branches[0];
    }

    /// <summary>Reports whether a template object satisfies the conditional rules that mention one property.</summary>
    /// <param name="obj">The template object.</param>
    /// <param name="prop">The property the rules must mention.</param>
    /// <param name="schema">The object schema.</param>
    /// <param name="root">The schema root.</param>
    /// <returns>True when the conditional rules accept the object.</returns>
    private static bool TemplateAcceptsConditionals(JsonObject obj, string prop, JsonObject schema, JsonNode root)
    {
        if (schema["allOf"] is not JsonArray conditional)
        {
            return true;
        }

        foreach (var branch in conditional)
        {
            if (branch is not JsonObject rule || rule["if"] is not JsonObject condition)
            {
                continue;
            }

            var mentions = (condition["required"] as JsonArray)?.Any(node => JsonString(node) == prop) == true
                || (condition["properties"] as JsonObject)?.ContainsKey(prop) == true;
            if (!mentions)
            {
                continue;
            }

            var matched = TemplateMatches(obj, condition, root);
            if (matched && rule["then"] is JsonObject then && !TemplateMatches(obj, then, root))
            {
                return false;
            }

            if (!matched && rule["else"] is JsonObject otherwise && !TemplateMatches(obj, otherwise, root))
            {
                return false;
            }
        }

        return true;
    }

    // A removable placeholder fills when the schema insists: a direct requirement,
    // a conditional requirement whose condition holds, or an else requirement whose
    // condition does not.
    /// <summary>Reports whether the schema requires one template property.</summary>
    /// <param name="schema">The object schema.</param>
    /// <param name="prop">The property name.</param>
    /// <param name="obj">The template object.</param>
    /// <param name="root">The schema root.</param>
    /// <returns>True when the property is required.</returns>
    private static bool IsTemplateRequired(JsonObject schema, string prop, JsonObject obj, JsonNode root)
    {
        if ((schema["required"] as JsonArray)?.Any(node => JsonString(node) == prop) == true)
        {
            return true;
        }

        if (schema["allOf"] is not JsonArray conditional)
        {
            return false;
        }

        foreach (var branch in conditional)
        {
            if (branch is not JsonObject rule || rule["if"] is not JsonObject condition)
            {
                continue;
            }

            var matched = TemplateMatches(obj, condition, root);
            var required = matched
                ? (rule["then"] as JsonObject)?["required"] as JsonArray
                : (rule["else"] as JsonObject)?["required"] as JsonArray;
            if (required?.Any(node => JsonString(node) == prop) == true)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Reports whether a template value matches a schema.</summary>
    /// <param name="value">The template value.</param>
    /// <param name="schema">The schema.</param>
    /// <param name="root">The schema root.</param>
    /// <returns>True when the value matches.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "TemplateMatches returns false when schema validation over an arbitrary template value cannot run, so a malformed schema reads as no match instead of crashing template validation.")]
    private static bool TemplateMatches(JsonNode? value, JsonNode? schema, JsonNode root)
    {
        var errors = new List<string>();
        try
        {
            JsonSchema.Validate(value, schema, root, "template", errors);
        }
        catch (Exception)
        {
            return false;
        }

        return errors.Count == 0;
    }
}
