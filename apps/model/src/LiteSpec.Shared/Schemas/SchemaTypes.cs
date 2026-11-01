using System.Text.Json.Nodes;
using static LiteSpec.Schemas.JsonSchemaSupport;

namespace LiteSpec.Schemas;

/// <summary>
///     The type, <c>const</c>, and <c>enum</c> keywords.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class SchemaTypes
{
    /// <summary>Validates a value through the <c>type</c>, <c>const</c>, and <c>enum</c> keywords of a schema.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="schema">The schema object.</param>
    /// <param name="location">The value location.</param>
    /// <param name="errors">The errors to append to.</param>
    /// <returns>False when a type mismatch ends validation of this schema.</returns>
    internal static bool ValidateTypes(JsonNode? value, JsonObject schema, string location, List<string> errors)
    {
        if (schema.TryGetPropertyValue("type", out var typeNode) && IsTruthy(typeNode)
            && typeNode is JsonValue typeValue && typeValue.TryGetValue<string>(out var typeName) && typeName is not null)
        {
            var matches = typeName switch
            {
                "object" => value is JsonObject,
                "array" => value is JsonArray,
                "integer" => value is JsonValue integer && integer.TryGetValue<double>(out var whole)
                    && double.IsFinite(whole) && whole == Math.Truncate(whole),
                "number" => value is JsonValue numberValue && numberValue.TryGetValue<double>(out var finite) && double.IsFinite(finite),
                "string" => value is JsonValue textual && textual.TryGetValue<string>(out _),
                "boolean" => value is JsonValue flag && flag.TryGetValue<bool>(out _),
                "null" => value is null,
                _ => true,
            };

            if (!matches)
            {
                errors.Add($"{location}: expected {typeName}");
                return false;
            }
        }

        if (schema.ContainsKey("const") && !JsonEqual(value, schema["const"]))
        {
            errors.Add($"{location}: expected constant {Stringify(schema["const"])}");
        }

        if (schema.TryGetPropertyValue("enum", out var enumNode) && IsTruthy(enumNode) && enumNode is JsonArray allowed
            && !allowed.Any(item => JsonEqual(item, value)))
        {
            errors.Add($"{location}: value is outside the allowed enum");
        }

        return true;
    }
}
