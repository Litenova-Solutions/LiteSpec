using System.Text.Json.Nodes;
using static LiteSpec.Schemas.JsonSchemaSupport;

namespace LiteSpec.Schemas;

/// <summary>
///     The number keywords: <c>minimum</c>, <c>maximum</c>, <c>exclusiveMinimum</c>,
///     <c>exclusiveMaximum</c>, and <c>multipleOf</c>.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class SchemaNumbers
{
    /// <summary>Validates a value through the number keywords of a schema.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="schema">The schema object.</param>
    /// <param name="location">The value location.</param>
    /// <param name="errors">The errors to append to.</param>
    internal static void ValidateNumbers(JsonNode? value, JsonObject schema, string location, List<string> errors)
    {
        if (value is JsonValue numeric && numeric.TryGetValue<double>(out var number))
        {
            if (schema.TryGetPropertyValue("minimum", out var minimum) && TryNumber(minimum, out var floor) && number < floor)
            {
                errors.Add($"{location}: number is below {JsNumber(floor)}");
            }

            if (schema.TryGetPropertyValue("maximum", out var maximum) && TryNumber(maximum, out var ceiling) && number > ceiling)
            {
                errors.Add($"{location}: number is above {JsNumber(ceiling)}");
            }

            if (schema.TryGetPropertyValue("exclusiveMinimum", out var exclusiveFloor) && TryNumber(exclusiveFloor, out var above) && number <= above)
            {
                errors.Add($"{location}: number is not above {JsNumber(above)}");
            }

            if (schema.TryGetPropertyValue("exclusiveMaximum", out var exclusiveCeiling) && TryNumber(exclusiveCeiling, out var below) && number >= below)
            {
                errors.Add($"{location}: number is not below {JsNumber(below)}");
            }

            // Floating-point remainders drift, so the test rounds to the nearest
            // multiple and compares against a tolerance derived from the divisor.
            if (schema.TryGetPropertyValue("multipleOf", out var divisorNode) && TryNumber(divisorNode, out var divisor))
            {
                var quotient = number / divisor;

                if (Math.Abs(quotient - Math.Round(quotient)) > 1e-9)
                {
                    errors.Add($"{location}: number is not a multiple of {JsNumber(divisor)}");
                }
            }
        }
    }
}
