using System.Text.Json.Nodes;
using static LiteSpec.Schemas.JsonSchemaSupport;

namespace LiteSpec.Schemas;

/// <summary>
///     The array keywords: <c>minItems</c>, <c>maxItems</c>, <c>uniqueItems</c>,
///     <c>items</c>, <c>prefixItems</c>, and <c>contains</c>.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class SchemaArrays
{
    /// <summary>Validates a value through the array keywords of a schema.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="schema">The schema object.</param>
    /// <param name="root">The schema root.</param>
    /// <param name="location">The value location.</param>
    /// <param name="errors">The errors to append to.</param>
    internal static void ValidateArrays(JsonNode? value, JsonObject schema, JsonNode root, string location, List<string> errors)
    {
        if (value is JsonArray array)
        {
            if (schema.TryGetPropertyValue("minItems", out var minItems) && TryNumber(minItems, out var fewest) && array.Count < fewest)
            {
                errors.Add($"{location}: array has fewer than {JsNumber(fewest)} items");
            }

            if (schema.TryGetPropertyValue("maxItems", out var maxItems) && TryNumber(maxItems, out var most) && array.Count > most)
            {
                errors.Add($"{location}: array has more than {JsNumber(most)} items");
            }

            if (schema.TryGetPropertyValue("uniqueItems", out var unique) && IsTruthy(unique)
                && array.Select(Stringify).ToHashSet(StringComparer.Ordinal).Count != array.Count)
            {
                errors.Add($"{location}: array items are not unique");
            }

            JsonArray prefix = [];
            schema.TryGetPropertyValue("items", out var items);

            if (schema.TryGetPropertyValue("prefixItems", out var prefixNode) && prefixNode is JsonArray prefixArray)
            {
                prefix = prefixArray;
            }

            for (var index = 0; index < prefix.Count && index < array.Count; index++)
            {
                JsonSchema.Validate(array[index], prefix[index], root, $"{location}/{index}", errors);
            }

            for (var index = prefix.Count; index < array.Count; index++)
            {
                JsonSchema.Validate(array[index], items, root, $"{location}/{index}", errors);
            }

            if (schema.TryGetPropertyValue("contains", out var contains) && IsTruthy(contains))
            {
                var holds = array.Any(item =>
                {
                    var itemErrors = new List<string>();
                    JsonSchema.Validate(item, contains, root, location, itemErrors);
                    return itemErrors.Count == 0;
                });

                if (!holds)
                {
                    errors.Add($"{location}: array contains no item matching the required shape");
                }
            }
        }
    }
}
