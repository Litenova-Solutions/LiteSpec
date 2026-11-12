using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static LiteSpec.Standards.JsonSchemaSupport;

namespace LiteSpec.Standards;

/// <summary>
///     The JSON Schema evaluator every validator uses: draft 2020-12 for the keywords
///     <see cref="JsonSchema.Keywords"/> names, with local <c>#/</c> references. It reports each
///     failure in the house message shape as it evaluates: a schema's own assertions first, then
///     what its subschemas report. A condition (<c>if</c>) and a negation (<c>not</c>) judge a
///     branch, so nothing inside them reports; <c>oneOf</c> and <c>anyOf</c> report one message
///     that names each rejected shape's first failure, and a composite that passes reports nothing
///     from its rejected shapes; and a <c>false</c> schema names the unknown property under
///     <c>additionalProperties</c> and reports the value as not allowed anywhere else.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class SchemaEvaluator
{
    /// <summary>How deep references and subschemas may nest before the evaluation stops, so a reference cycle that consumes no instance ends.</summary>
    private const int MaxDepth = 256;

    /// <summary>Gets the effective tree of a subschema validated against a root, built once per pair.</summary>
    private static readonly ConditionalWeakTable<JsonNode, EffectiveEntry> EffectiveCache = new();

    /// <summary>Gets each pattern compiled once, with a timeout so a pathological pattern cannot hang a check.</summary>
    private static readonly ConcurrentDictionary<string, Regex> Patterns = new(StringComparer.Ordinal);

    /// <summary>Validates a value against a schema whose <c>#/</c> references resolve against the root, adding one message per failure.</summary>
    /// <param name="value">The JSON value to validate.</param>
    /// <param name="schema">The schema to validate against, the root itself or a subschema of it.</param>
    /// <param name="root">The schema root that holds the <c>$defs</c> references resolve to.</param>
    /// <param name="location">The location that opens every message, such as a file path.</param>
    /// <param name="errors">The list each message is added to.</param>
    internal static void Validate(JsonNode? value, JsonNode? schema, JsonNode root, string location, List<string> errors)
    {
        if (schema is not JsonObject)
        {
            return;
        }

        var effective = EffectiveTree(schema, root);
        var context = new Context(effective, location);

        Evaluate(effective, value, "", null, context, errors, 0);
    }

    /// <summary>
    ///     The tree a schema evaluates in: the root itself, or a copy of the subschema that carries
    ///     the root's <c>$defs</c>, so a <c>#/$defs/</c> reference resolves while every other
    ///     <c>#/</c> pointer reads the subschema.
    /// </summary>
    /// <param name="schema">The schema to validate against.</param>
    /// <param name="root">The schema root.</param>
    /// <returns>The tree to evaluate.</returns>
    private static JsonNode EffectiveTree(JsonNode schema, JsonNode root)
    {
        if (ReferenceEquals(schema, root))
        {
            return root;
        }

        var entry = EffectiveCache.GetValue(schema, _ => new EffectiveEntry());

        lock (entry)
        {
            if (entry.Tree is null || !ReferenceEquals(entry.Root, root))
            {
                var clone = (JsonObject)schema.DeepClone();

                if (!clone.ContainsKey("$defs") && root is JsonObject rootObject &&
                    rootObject.TryGetPropertyValue("$defs", out var defs) && defs is not null)
                {
                    clone["$defs"] = defs.DeepClone();
                }

                entry.Tree = clone;
                entry.Root = root;
            }

            return entry.Tree;
        }
    }

    /// <summary>
    ///     Evaluates one schema against one value, adding its messages when a list is given, and returns whether
    ///     the value passes. Without a list it only judges, so it stops at the first failure.
    /// </summary>
    /// <param name="schema">The schema: an object, or a boolean schema.</param>
    /// <param name="value">The value at this point of the instance.</param>
    /// <param name="pointer">The JSON Pointer of the value inside the instance.</param>
    /// <param name="position">The keyword whose subschema this is, such as <c>additionalProperties</c>, or null.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="errors">The list messages are added to, or null to judge the value without reporting.</param>
    /// <param name="depth">How deep the evaluation is nested.</param>
    /// <returns>True when the value passes the schema.</returns>
    private static bool Evaluate(JsonNode? schema, JsonNode? value, string pointer, string? position, Context context, List<string>? errors, int depth)
    {
        if (depth > MaxDepth)
        {
            throw new InvalidOperationException("schema references nest too deeply to evaluate");
        }

        if (schema is JsonValue flag && flag.TryGetValue<bool>(out var allowed))
        {
            if (!allowed && errors is not null)
            {
                if (position == "additionalProperties")
                {
                    var slash = pointer.LastIndexOf('/');
                    var property = pointer[(slash + 1)..].Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);
                    errors.Add($"{context.Location}{pointer[..slash]}: unknown property '{property}'");
                }
                else
                {
                    errors.Add($"{context.Location}{pointer}: value is not allowed");
                }
            }

            return allowed;
        }

        if (schema is not JsonObject keywords)
        {
            return true;
        }

        var prefix = context.Location + pointer;
        var own = errors is null ? null : new List<string>();
        var nested = errors is null ? null : new List<string>();
        var valid = true;

        valid &= Assertions(keywords, value, prefix, context, own, depth);

        if (!valid && errors is null)
        {
            return false;
        }

        valid &= Applicators(keywords, value, pointer, context, nested, depth);

        if (errors is not null && !valid)
        {
            errors.AddRange(own!);
            errors.AddRange(nested!);
        }

        return valid;
    }

    /// <summary>Evaluates the keywords that judge the value at this point, adding their messages.</summary>
    /// <param name="keywords">The schema object.</param>
    /// <param name="value">The value at this point.</param>
    /// <param name="prefix">The message prefix: the location and the value's pointer.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="errors">The list messages are added to, or null.</param>
    /// <param name="depth">How deep the evaluation is nested.</param>
    /// <returns>True when every assertion passes.</returns>
    private static bool Assertions(JsonObject keywords, JsonNode? value, string prefix, Context context, List<string>? errors, int depth)
    {
        var valid = true;
        var kind = KindOf(value);

        foreach (var (keyword, argument) in keywords)
        {
            var passed = keyword switch
            {
                "type" => TypeMatches(argument, value, kind) || Fail(errors, prefix, TypeMessage(argument)),
                "const" => JsonEquals(argument, value) || Fail(errors, prefix, $"expected constant {Stringify(argument)}"),
                "enum" => argument is not JsonArray choices || choices.Any(choice => JsonEquals(choice, value)) || Fail(errors, prefix, "value is outside the allowed enum"),
                "pattern" => kind != "string" || argument is not JsonValue patternValue || !patternValue.TryGetValue<string>(out var pattern) || PatternFor(pattern).IsMatch(value!.GetValue<string>()) || Fail(errors, prefix, $"string does not match {pattern}"),
                "minLength" => kind != "string" || !TrySchemaNumber(argument, out var least) || CodePoints(value!.GetValue<string>()) >= least || Fail(errors, prefix, $"string is shorter than {JsNumber(least)}"),
                "maxLength" => kind != "string" || !TrySchemaNumber(argument, out var most) || CodePoints(value!.GetValue<string>()) <= most || Fail(errors, prefix, $"string is longer than {JsNumber(most)}"),
                "minimum" => Bound(argument, value, kind, prefix, errors, order => order >= 0, "number is below"),
                "maximum" => Bound(argument, value, kind, prefix, errors, order => order <= 0, "number is above"),
                "exclusiveMinimum" => Bound(argument, value, kind, prefix, errors, order => order > 0, "number is not above"),
                "exclusiveMaximum" => Bound(argument, value, kind, prefix, errors, order => order < 0, "number is not below"),
                "multipleOf" => MultipleOf(argument, value, kind, prefix, errors),
                "minItems" => value is not JsonArray shortList || !TrySchemaNumber(argument, out var fewest) || shortList.Count >= fewest || Fail(errors, prefix, $"array has fewer than {JsNumber(fewest)} items"),
                "maxItems" => value is not JsonArray longList || !TrySchemaNumber(argument, out var largest) || longList.Count <= largest || Fail(errors, prefix, $"array has more than {JsNumber(largest)} items"),
                "uniqueItems" => value is not JsonArray uniqueList || !IsTrue(argument) || Unique(uniqueList) || Fail(errors, prefix, "array items are not unique"),
                "contains" => value is not JsonArray containing || Contains(keywords, argument, containing, prefix, context, errors, depth),
                "minProperties" => value is not JsonObject smallObject || !TrySchemaNumber(argument, out var fewestFields) || smallObject.Count >= fewestFields || Fail(errors, prefix, $"object has fewer than {JsNumber(fewestFields)} properties"),
                "maxProperties" => value is not JsonObject largeObject || !TrySchemaNumber(argument, out var mostFields) || largeObject.Count <= mostFields || Fail(errors, prefix, $"object has more than {JsNumber(mostFields)} properties"),
                "required" => value is not JsonObject fields || Required(argument, fields, prefix, errors),
                "dependentRequired" => value is not JsonObject dependents || DependentRequired(argument, dependents, prefix, errors),
                "not" => !Evaluate(argument, value, "", null, context, null, depth + 1) || Fail(errors, prefix, "value matches prohibited schema"),
                "oneOf" => OneOf(argument, value, prefix, context, errors, depth),
                "anyOf" => AnyOf(argument, value, prefix, context, errors, depth),
                _ => true,
            };

            valid &= passed;

            if (!valid && errors is null)
            {
                return false;
            }
        }

        return valid;
    }

    /// <summary>Evaluates the keywords that apply subschemas to the value or its parts, adding what those report.</summary>
    /// <param name="keywords">The schema object.</param>
    /// <param name="value">The value at this point.</param>
    /// <param name="pointer">The value's pointer inside the instance.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="errors">The list messages are added to, or null.</param>
    /// <param name="depth">How deep the evaluation is nested.</param>
    /// <returns>True when every applied subschema passes.</returns>
    private static bool Applicators(JsonObject keywords, JsonNode? value, string pointer, Context context, List<string>? errors, int depth)
    {
        var valid = true;

        foreach (var (keyword, argument) in InEvaluationOrder(keywords))
        {
            switch (keyword)
            {
                case "$ref":
                    valid &= Evaluate(Reference(argument, context), value, pointer, null, context, errors, depth + 1);

                    if (!valid && errors is null)
                    {
                        return false;
                    }

                    break;

                case "allOf" when argument is JsonArray all:
                    foreach (var branch in all)
                    {
                        valid &= Evaluate(branch, value, pointer, null, context, errors, depth + 1);

                        if (!valid && errors is null)
                        {
                            return false;
                        }
                    }

                    break;

                case "if":
                    var conditionHolds = Evaluate(argument, value, pointer, null, context, null, depth + 1);
                    var branchKey = conditionHolds ? "then" : "else";

                    if (keywords.TryGetPropertyValue(branchKey, out var consequence))
                    {
                        valid &= Evaluate(consequence, value, pointer, null, context, errors, depth + 1);

                        if (!valid && errors is null)
                        {
                            return false;
                        }
                    }

                    break;

                case "properties" when argument is JsonObject properties && value is JsonObject fields:
                    foreach (var (name, subschema) in properties)
                    {
                        if (fields.TryGetPropertyValue(name, out var field))
                        {
                            valid &= Evaluate(subschema, field, $"{pointer}/{Escape(name)}", "properties", context, errors, depth + 1);

                            if (!valid && errors is null)
                            {
                                return false;
                            }
                        }
                    }

                    break;

                case "patternProperties" when argument is JsonObject patterns && value is JsonObject fields:
                    foreach (var (name, field) in fields)
                    {
                        foreach (var (pattern, subschema) in patterns)
                        {
                            if (PatternFor(pattern).IsMatch(name))
                            {
                                valid &= Evaluate(subschema, field, $"{pointer}/{Escape(name)}", "patternProperties", context, errors, depth + 1);

                                if (!valid && errors is null)
                                {
                                    return false;
                                }
                            }
                        }
                    }

                    break;

                case "additionalProperties" when value is JsonObject fields:
                    foreach (var (name, field) in fields)
                    {
                        if (!Declared(keywords, name))
                        {
                            valid &= Evaluate(argument, field, $"{pointer}/{Escape(name)}", "additionalProperties", context, errors, depth + 1);

                            if (!valid && errors is null)
                            {
                                return false;
                            }
                        }
                    }

                    break;

                case "propertyNames" when value is JsonObject fields:
                    foreach (var (name, _) in fields)
                    {
                        valid &= Evaluate(argument, JsonValue.Create(name), $"{pointer}/{Escape(name)}", "propertyNames", context, errors, depth + 1);

                        if (!valid && errors is null)
                        {
                            return false;
                        }
                    }

                    break;

                case "dependentSchemas" when argument is JsonObject dependentSchemas && value is JsonObject fields:
                    foreach (var (name, subschema) in dependentSchemas)
                    {
                        if (fields.ContainsKey(name))
                        {
                            valid &= Evaluate(subschema, value, pointer, "dependentSchemas", context, errors, depth + 1);

                            if (!valid && errors is null)
                            {
                                return false;
                            }
                        }
                    }

                    break;

                case "prefixItems" when argument is JsonArray prefix && value is JsonArray items:
                    for (var index = 0; index < Math.Min(prefix.Count, items.Count); index++)
                    {
                        valid &= Evaluate(prefix[index], items[index], $"{pointer}/{index}", "prefixItems", context, errors, depth + 1);

                        if (!valid && errors is null)
                        {
                            return false;
                        }
                    }

                    break;

                case "items" when value is JsonArray items:
                    var first = keywords["prefixItems"] is JsonArray leading ? leading.Count : 0;

                    for (var index = first; index < items.Count; index++)
                    {
                        valid &= Evaluate(argument, items[index], $"{pointer}/{index}", "items", context, errors, depth + 1);

                        if (!valid && errors is null)
                        {
                            return false;
                        }
                    }

                    break;
            }
        }

        return valid;
    }

    /// <summary>
    ///     The keywords of a schema in the order they evaluate: as written, except that the keywords
    ///     that read what others found, additionalProperties after properties and patternProperties
    ///     and items after prefixItems, evaluate last, so the messages keep one order whatever order
    ///     the schema lists the keywords in.
    /// </summary>
    /// <param name="keywords">The schema object.</param>
    /// <returns>The keywords in evaluation order.</returns>
    private static IEnumerable<KeyValuePair<string, JsonNode?>> InEvaluationOrder(JsonObject keywords)
    {
        var positions = new Dictionary<string, int>(StringComparer.Ordinal);
        var index = 0;

        foreach (var (keyword, _) in keywords)
        {
            positions[keyword] = index++;
        }

        int Rank(string keyword) => keyword switch
        {
            "additionalProperties" or "items" => positions.Count + positions[keyword],
            _ => positions[keyword],
        };

        return keywords.OrderBy(pair => Rank(pair.Key));
    }

    /// <summary>Whether <c>properties</c> or <c>patternProperties</c> covers a property, so <c>additionalProperties</c> skips it.</summary>
    /// <param name="keywords">The schema object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>True when another keyword covers the property.</returns>
    private static bool Declared(JsonObject keywords, string name)
    {
        if (keywords["properties"] is JsonObject properties && properties.ContainsKey(name))
        {
            return true;
        }

        if (keywords["patternProperties"] is JsonObject patterns)
        {
            foreach (var (pattern, _) in patterns)
            {
                if (PatternFor(pattern).IsMatch(name))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>The schema a <c>$ref</c> names, resolved against the evaluation tree.</summary>
    /// <param name="argument">The <c>$ref</c> value.</param>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The referenced schema.</returns>
    private static JsonNode? Reference(JsonNode? argument, Context context)
    {
        if (argument is not JsonValue reference || !reference.TryGetValue<string>(out var target))
        {
            throw new InvalidOperationException("a schema reference is not a string");
        }

        if (target == "#")
        {
            return context.Tree;
        }

        return SchemaReferences.ResolveRef(context.Tree, target)
            ?? throw new InvalidOperationException($"schema reference '{target}' resolves to nothing");
    }

    /// <summary>Evaluates <c>contains</c> with <c>minContains</c> and <c>maxContains</c>, adding one message when the count is outside them.</summary>
    /// <param name="keywords">The schema object holding the counts.</param>
    /// <param name="argument">The <c>contains</c> subschema.</param>
    /// <param name="items">The array.</param>
    /// <param name="prefix">The message prefix.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="errors">The list messages are added to, or null.</param>
    /// <param name="depth">How deep the evaluation is nested.</param>
    /// <returns>True when the count of matching items lies within the bounds.</returns>
    private static bool Contains(JsonObject keywords, JsonNode? argument, JsonArray items, string prefix, Context context, List<string>? errors, int depth)
    {
        var minimum = 1.0;
        double? maximum = null;

        if (keywords["minContains"] is JsonNode minNode && TrySchemaNumber(minNode, out var least))
        {
            minimum = Math.Truncate(least);
        }

        if (keywords["maxContains"] is JsonNode maxNode && TrySchemaNumber(maxNode, out var most))
        {
            maximum = Math.Truncate(most);
        }

        var found = items.Count(item => Evaluate(argument, item, "", null, context, null, depth + 1));

        if (found < minimum)
        {
            return Fail(errors, prefix, minimum == 1 && maximum is null
                ? "array contains no item matching the required shape"
                : $"array contains fewer than {JsNumber(minimum)} matching items");
        }

        if (maximum.HasValue && found > maximum.Value)
        {
            return Fail(errors, prefix, $"array contains more than {JsNumber(maximum.Value)} matching items");
        }

        return true;
    }

    /// <summary>
    ///     Evaluates <c>oneOf</c>, adding one message that counts the matching shapes and, when none match, names each
    ///     shape's first failure. The shapes are judged without messages first, so a value that passes builds none.
    /// </summary>
    /// <param name="argument">The list of shapes.</param>
    /// <param name="value">The value.</param>
    /// <param name="prefix">The message prefix.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="errors">The list messages are added to, or null.</param>
    /// <param name="depth">How deep the evaluation is nested.</param>
    /// <returns>True when exactly one shape matches.</returns>
    private static bool OneOf(JsonNode? argument, JsonNode? value, string prefix, Context context, List<string>? errors, int depth)
    {
        if (argument is not JsonArray shapes)
        {
            return true;
        }

        var matched = Matches(shapes, value, prefix, context, depth, stopAt: 2);

        if (matched == 1)
        {
            return true;
        }

        if (errors is null)
        {
            return false;
        }

        if (matched > 1)
        {
            matched = Matches(shapes, value, prefix, context, depth, stopAt: int.MaxValue);
        }

        var message = $"value matches {matched} of {shapes.Count} allowed shapes, expected exactly 1";

        if (matched == 0)
        {
            var reasons = Reasons(Shapes(shapes, value, prefix, context, depth), context);

            if (reasons.Count > 0)
            {
                message += $" ({string.Join("; ", reasons)})";
            }
        }

        return Fail(errors, prefix, message);
    }

    /// <summary>
    ///     Evaluates <c>anyOf</c>, adding one message that names each shape's first failure when none matches. The
    ///     shapes are judged without messages first, so a value that passes builds none.
    /// </summary>
    /// <param name="argument">The list of shapes.</param>
    /// <param name="value">The value.</param>
    /// <param name="prefix">The message prefix.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="errors">The list messages are added to, or null.</param>
    /// <param name="depth">How deep the evaluation is nested.</param>
    /// <returns>True when at least one shape matches.</returns>
    private static bool AnyOf(JsonNode? argument, JsonNode? value, string prefix, Context context, List<string>? errors, int depth)
    {
        if (argument is not JsonArray shapes)
        {
            return true;
        }

        if (Matches(shapes, value, prefix, context, depth, stopAt: 1) > 0)
        {
            return true;
        }

        if (errors is null)
        {
            return false;
        }

        return Fail(errors, prefix, $"value matches none of the {shapes.Count} allowed shapes ({string.Join("; ", Reasons(Shapes(shapes, value, prefix, context, depth), context))})");
    }

    /// <summary>Counts the shapes the value matches, evaluated without messages, stopping once the count reaches a bound.</summary>
    /// <param name="shapes">The shapes.</param>
    /// <param name="value">The value.</param>
    /// <param name="prefix">The message prefix.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="depth">How deep the evaluation is nested.</param>
    /// <param name="stopAt">The count at which counting stops.</param>
    /// <returns>The number of matching shapes, at most the bound.</returns>
    private static int Matches(JsonArray shapes, JsonNode? value, string prefix, Context context, int depth, int stopAt)
    {
        var pointer = prefix[context.Location.Length..];
        var matched = 0;

        foreach (var shape in shapes)
        {
            if (Evaluate(shape, value, pointer, null, context, null, depth + 1) && ++matched >= stopAt)
            {
                break;
            }
        }

        return matched;
    }

    /// <summary>Evaluates each shape of a composite against the value, keeping each shape's own messages.</summary>
    /// <param name="shapes">The shapes.</param>
    /// <param name="value">The value.</param>
    /// <param name="prefix">The message prefix of the value, whose pointer each shape reports under.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="depth">How deep the evaluation is nested.</param>
    /// <returns>Each shape's outcome and messages, in order.</returns>
    private static List<(bool Valid, List<string> Messages)> Shapes(JsonArray shapes, JsonNode? value, string prefix, Context context, int depth)
    {
        var pointer = prefix[context.Location.Length..];
        var outcomes = new List<(bool Valid, List<string> Messages)>();

        foreach (var shape in shapes)
        {
            var messages = new List<string>();
            var valid = Evaluate(shape, value, pointer, null, context, messages, depth + 1);
            outcomes.Add((valid, messages));
        }

        return outcomes;
    }

    /// <summary>The first failure of each rejected shape, numbered by its place in the composite.</summary>
    /// <param name="outcomes">The shapes' outcomes.</param>
    /// <param name="context">The evaluation context.</param>
    /// <returns>One reason per rejected shape that reported a failure.</returns>
    private static List<string> Reasons(List<(bool Valid, List<string> Messages)> outcomes, Context context)
    {
        var reasons = new List<string>();

        for (var index = 0; index < outcomes.Count; index++)
        {
            if (!outcomes[index].Valid && outcomes[index].Messages.Count > 0)
            {
                reasons.Add($"shape {index + 1}: {StripLocationPrefix(outcomes[index].Messages[0], context.Location)}");
            }
        }

        return reasons;
    }

    /// <summary>Evaluates <c>required</c>, adding one message per missing property.</summary>
    /// <param name="argument">The required names.</param>
    /// <param name="fields">The object.</param>
    /// <param name="prefix">The message prefix.</param>
    /// <param name="errors">The list messages are added to, or null.</param>
    /// <returns>True when every required property is present.</returns>
    private static bool Required(JsonNode? argument, JsonObject fields, string prefix, List<string>? errors)
    {
        if (argument is not JsonArray names)
        {
            return true;
        }

        var valid = true;

        foreach (var name in names)
        {
            var property = RequiredName(name);

            if (!fields.ContainsKey(property))
            {
                valid = Fail(errors, prefix, $"missing required property '{property}'");
            }
        }

        return valid;
    }

    /// <summary>Evaluates <c>dependentRequired</c>, adding one message per missing dependent property.</summary>
    /// <param name="argument">The trigger properties and the properties each requires.</param>
    /// <param name="fields">The object.</param>
    /// <param name="prefix">The message prefix.</param>
    /// <param name="errors">The list messages are added to, or null.</param>
    /// <returns>True when every present trigger has its dependents.</returns>
    private static bool DependentRequired(JsonNode? argument, JsonObject fields, string prefix, List<string>? errors)
    {
        if (argument is not JsonObject triggers)
        {
            return true;
        }

        var valid = true;

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
                    valid = Fail(errors, prefix, $"property '{trigger}' requires '{property}'");
                }
            }
        }

        return valid;
    }

    /// <summary>Evaluates a numeric bound, adding its message when the number falls outside it.</summary>
    /// <param name="argument">The bound.</param>
    /// <param name="value">The value.</param>
    /// <param name="kind">The value's JSON type.</param>
    /// <param name="prefix">The message prefix.</param>
    /// <param name="errors">The list messages are added to, or null.</param>
    /// <param name="holds">Whether the order of the number against the bound satisfies it: negative below, zero equal, positive above.</param>
    /// <param name="phrase">The message before the bound, such as <c>number is below</c>.</param>
    /// <returns>True when the value is no number or satisfies the bound.</returns>
    private static bool Bound(JsonNode? argument, JsonNode? value, string? kind, string prefix, List<string>? errors, Func<int, bool> holds, string phrase)
    {
        if (kind != "number" || !TrySchemaNumber(argument, out var bound))
        {
            return true;
        }

        return holds(Order(value, argument)) || Fail(errors, prefix, $"{phrase} {JsNumber(bound)}");
    }

    /// <summary>How one number orders against another: exactly as decimals when both fit, else as doubles.</summary>
    /// <param name="left">The first number.</param>
    /// <param name="right">The second number.</param>
    /// <returns>Negative, zero or positive as the first is below, equal to or above the second.</returns>
    private static int Order(JsonNode? left, JsonNode? right)
    {
        if (TryDecimal(left, out var exactLeft) && TryDecimal(right, out var exactRight))
        {
            return exactLeft.CompareTo(exactRight);
        }

        TrySchemaNumber(left, out var nearLeft);
        TrySchemaNumber(right, out var nearRight);
        return nearLeft.CompareTo(nearRight);
    }

    /// <summary>Evaluates <c>multipleOf</c>, adding its message when the number is no multiple.</summary>
    /// <param name="argument">The divisor.</param>
    /// <param name="value">The value.</param>
    /// <param name="kind">The value's JSON type.</param>
    /// <param name="prefix">The message prefix.</param>
    /// <param name="errors">The list messages are added to, or null.</param>
    /// <returns>True when the value is no number or a multiple of the divisor.</returns>
    private static bool MultipleOf(JsonNode? argument, JsonNode? value, string? kind, string prefix, List<string>? errors)
    {
        if (kind != "number" || !TrySchemaNumber(argument, out var divisor) || divisor <= 0)
        {
            return true;
        }

        var multiple = TryDecimal(value, out var number) && TryDecimal(argument, out var exact)
            ? number % exact == 0
            : TrySchemaNumber(value, out var approximate) && Math.Abs(Math.IEEERemainder(approximate, divisor)) < 1e-9;

        return multiple || Fail(errors, prefix, $"number is not a multiple of {JsNumber(divisor)}");
    }

    /// <summary>Whether the value has one of the types the <c>type</c> keyword names.</summary>
    /// <param name="argument">The <c>type</c> value: one name or a list.</param>
    /// <param name="value">The value.</param>
    /// <param name="kind">The value's JSON type.</param>
    /// <returns>True when the value has a named type.</returns>
    private static bool TypeMatches(JsonNode? argument, JsonNode? value, string? kind)
    {
        bool Matches(string name) => name == kind || (name == "integer" && kind == "number" && IsInteger(value));

        return argument switch
        {
            JsonValue single when single.TryGetValue<string>(out var name) => Matches(name),
            JsonArray union => union.Any(entry => entry is JsonValue named && named.TryGetValue<string>(out var name) && Matches(name)),
            _ => true,
        };
    }

    /// <summary>The <c>type</c> message: <c>expected object</c>, or <c>expected string or null</c> for a list.</summary>
    /// <param name="argument">The <c>type</c> value.</param>
    /// <returns>The message.</returns>
    private static string TypeMessage(JsonNode? argument) => argument switch
    {
        JsonValue single when single.TryGetValue<string>(out var name) => $"expected {name}",
        JsonArray union => $"expected {string.Join(" or ", union.Select(entry => entry is JsonValue named && named.TryGetValue<string>(out var name) ? name : Stringify(entry)))}",
        _ => $"expected {Stringify(argument)}",
    };

    /// <summary>The JSON type of a value: object, array, string, number, boolean or null.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The type name.</returns>
    private static string? KindOf(JsonNode? value) => value switch
    {
        null => "null",
        JsonObject => "object",
        JsonArray => "array",
        JsonValue scalar => scalar.GetValueKind() switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => "number",
            JsonValueKind.True or JsonValueKind.False => "boolean",
            JsonValueKind.Null => "null",
            _ => null,
        },
        _ => null,
    };

    /// <summary>Whether a number has no fractional part, as JSON Schema counts <c>1.0</c> an integer.</summary>
    /// <param name="value">The number.</param>
    /// <returns>True when the number is whole.</returns>
    private static bool IsInteger(JsonNode? value) =>
        TryDecimal(value, out var exact)
            ? exact == decimal.Truncate(exact)
            : TrySchemaNumber(value, out var approximate) && double.IsFinite(approximate) && approximate == Math.Truncate(approximate);

    /// <summary>Reads a number exactly, when it fits a decimal.</summary>
    /// <param name="node">The number.</param>
    /// <param name="value">The number as a decimal.</param>
    /// <returns>True when the number fits a decimal.</returns>
    private static bool TryDecimal(JsonNode? node, out decimal value)
    {
        value = 0;
        var text = node?.ToJsonString();
        return text is not null && decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Whether two JSON values are equal as JSON Schema compares them: numbers by value, objects by their properties in any order.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>True when the values are equal.</returns>
    internal static bool JsonEquals(JsonNode? left, JsonNode? right)
    {
        var kind = KindOf(left);

        if (kind != KindOf(right))
        {
            return false;
        }

        switch (kind)
        {
            case "null":
                return true;
            case "boolean":
                return left!.GetValue<bool>() == right!.GetValue<bool>();
            case "string":
                return string.Equals(left!.GetValue<string>(), right!.GetValue<string>(), StringComparison.Ordinal);
            case "number":
                return TryDecimal(left, out var first) && TryDecimal(right, out var second)
                    ? first == second
                    : TrySchemaNumber(left, out var near) && TrySchemaNumber(right, out var far) && near.Equals(far);
            case "array":
                var leftItems = (JsonArray)left!;
                var rightItems = (JsonArray)right!;
                return leftItems.Count == rightItems.Count && leftItems.Zip(rightItems).All(pair => JsonEquals(pair.First, pair.Second));
            case "object":
                var leftFields = (JsonObject)left!;
                var rightFields = (JsonObject)right!;
                return leftFields.Count == rightFields.Count &&
                    leftFields.All(field => rightFields.TryGetPropertyValue(field.Key, out var other) && JsonEquals(field.Value, other));
            default:
                return false;
        }
    }

    /// <summary>Whether every item of an array differs from every other.</summary>
    /// <param name="items">The array.</param>
    /// <returns>True when no two items are equal.</returns>
    private static bool Unique(JsonArray items)
    {
        for (var index = 0; index < items.Count; index++)
        {
            for (var other = index + 1; other < items.Count; other++)
            {
                if (JsonEquals(items[index], items[other]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>The length of a string in Unicode code points, as JSON Schema counts it.</summary>
    /// <param name="text">The string.</param>
    /// <returns>The number of code points.</returns>
    private static int CodePoints(string text)
    {
        var count = 0;

        for (var index = 0; index < text.Length; index++)
        {
            if (!char.IsLowSurrogate(text[index]) || index == 0 || !char.IsHighSurrogate(text[index - 1]))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Whether a schema value is the JSON value <c>true</c>.</summary>
    /// <param name="node">The schema value.</param>
    /// <returns>True for <c>true</c>.</returns>
    private static bool IsTrue(JsonNode? node) => node is JsonValue flag && flag.TryGetValue<bool>(out var value) && value;

    /// <summary>The compiled form of a pattern, built once.</summary>
    /// <param name="pattern">The regular expression.</param>
    /// <returns>The compiled expression.</returns>
    private static Regex PatternFor(string pattern) =>
        Patterns.GetOrAdd(pattern, key => new Regex(key, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)));

    /// <summary>A property name as a JSON Pointer segment.</summary>
    /// <param name="name">The property name.</param>
    /// <returns>The escaped segment.</returns>
    private static string Escape(string name) => name.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);

    /// <summary>Adds a message when a list is given, and returns false, so a failing assertion reads <c>holds || Fail(...)</c>.</summary>
    /// <param name="errors">The list, or null.</param>
    /// <param name="prefix">The message prefix.</param>
    /// <param name="message">The message.</param>
    /// <returns>False.</returns>
    private static bool Fail(List<string>? errors, string prefix, string message)
    {
        errors?.Add($"{prefix}: {message}");
        return false;
    }

    /// <summary>The tree references resolve against and the location every message opens with.</summary>
    /// <param name="Tree">The evaluation tree.</param>
    /// <param name="Location">The location prefix.</param>
    private sealed record Context(JsonNode Tree, string Location);

    /// <summary>One cached effective tree and the root it was built against.</summary>
    private sealed class EffectiveEntry
    {
        /// <summary>Gets or sets the root the tree was built against.</summary>
        public JsonNode? Root { get; set; }

        /// <summary>Gets or sets the effective tree.</summary>
        public JsonNode? Tree { get; set; }
    }
}
