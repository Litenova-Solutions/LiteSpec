using System.Text.Json.Nodes;
using static LiteSpec.Schemas.JsonSchemaSupport;

namespace LiteSpec.Schemas;

/// <summary>
///     The string keywords: <c>minLength</c>, <c>maxLength</c>, and <c>pattern</c>.
///     The <c>format</c> keyword asserts nothing and is an annotation.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class SchemaStrings
{
    /// <summary>Validates a value through the string keywords of a schema.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="schema">The schema object.</param>
    /// <param name="location">The value location.</param>
    /// <param name="errors">The errors to append to.</param>
    internal static void ValidateStrings(JsonNode? value, JsonObject schema, string location, List<string> errors)
    {
        if (value is JsonValue stringValue && stringValue.TryGetValue<string>(out var text) && text is not null)
        {
            if (schema.TryGetPropertyValue("minLength", out var minLength) && TryNumber(minLength, out var min) && text.Length < min)
            {
                errors.Add($"{location}: string is shorter than {JsNumber(min)}");
            }

            if (schema.TryGetPropertyValue("maxLength", out var maxLength) && TryNumber(maxLength, out var max) && text.Length > max)
            {
                errors.Add($"{location}: string is longer than {JsNumber(max)}");
            }

            if (schema.TryGetPropertyValue("pattern", out var patternNode) && patternNode is JsonValue patternValue
                && patternValue.TryGetValue<string>(out var pattern) && !string.IsNullOrEmpty(pattern)
                && !CachedPattern(pattern).IsMatch(text))
            {
                errors.Add($"{location}: string does not match {pattern}");
            }
        }
    }
}
