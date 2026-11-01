using System.Text.Json.Nodes;
using static LiteSpec.Schemas.JsonSchemaSupport;

namespace LiteSpec.Schemas;

/// <summary>
///     The object keywords: <c>minProperties</c>, <c>maxProperties</c>, <c>required</c>,
///     <c>dependentRequired</c>, <c>propertyNames</c>, <c>properties</c>,
///     <c>patternProperties</c>, and <c>additionalProperties</c>.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class SchemaObjects
{
    /// <summary>Validates a value through the object keywords of a schema.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="schema">The schema object.</param>
    /// <param name="root">The schema root.</param>
    /// <param name="location">The value location.</param>
    /// <param name="errors">The errors to append to.</param>
    internal static void ValidateObjects(JsonNode? value, JsonObject schema, JsonNode root, string location, List<string> errors)
    {
        if (value is JsonObject fields)
        {
            if (schema.TryGetPropertyValue("minProperties", out var minProperties) && TryNumber(minProperties, out var fewest) && fields.Count < fewest)
            {
                errors.Add($"{location}: object has fewer than {JsNumber(fewest)} properties");
            }

            if (schema.TryGetPropertyValue("maxProperties", out var maxProperties) && TryNumber(maxProperties, out var most) && fields.Count > most)
            {
                errors.Add($"{location}: object has more than {JsNumber(most)} properties");
            }

            if (schema.TryGetPropertyValue("required", out var required) && required is JsonArray names)
            {
                foreach (var name in names)
                {
                    var property = RequiredName(name);

                    if (!fields.ContainsKey(property))
                    {
                        errors.Add($"{location}: missing required property '{property}'");
                    }
                }
            }

            if (schema.TryGetPropertyValue("dependentRequired", out var dependentRequired) && dependentRequired is JsonObject triggers)
            {
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
                            errors.Add($"{location}: property '{trigger}' requires '{property}'");
                        }
                    }
                }
            }

            if (schema.TryGetPropertyValue("propertyNames", out var propertyNames) && IsTruthy(propertyNames))
            {
                foreach (var key in fields.Select(pair => pair.Key).ToList())
                {
                    JsonSchema.Validate(JsonValue.Create(key), propertyNames, root, $"{location}/{key}", errors);
                }
            }

            var declared = schema.TryGetPropertyValue("properties", out var properties) && properties is JsonObject declaredFields
                ? declaredFields
                : NoProperties;
            var patternFields = schema.TryGetPropertyValue("patternProperties", out var patternProperties) && patternProperties is JsonObject patterns
                ? patterns
                : null;
            schema.TryGetPropertyValue("additionalProperties", out var additional);

            foreach (var (key, child) in fields)
            {
                var evaluated = false;

                if (declared.ContainsKey(key))
                {
                    JsonSchema.Validate(child, declared[key], root, $"{location}/{key}", errors);
                    evaluated = true;
                }

                if (patternFields is not null)
                {
                    foreach (var (expression, subschema) in patternFields)
                    {
                        if (!CachedPattern(expression).IsMatch(key))
                        {
                            continue;
                        }

                        JsonSchema.Validate(child, subschema, root, $"{location}/{key}", errors);
                        evaluated = true;
                    }
                }

                if (evaluated)
                {
                    continue;
                }

                if (additional is JsonValue flag && flag.TryGetValue<bool>(out var allow) && !allow)
                {
                    errors.Add($"{location}: unknown property '{key}'");
                }
                else if (additional is JsonObject extra)
                {
                    JsonSchema.Validate(child, extra, root, $"{location}/{key}", errors);
                }
            }
        }
    }
}
