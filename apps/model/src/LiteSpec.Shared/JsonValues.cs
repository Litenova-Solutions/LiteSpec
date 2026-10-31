using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec;

/// <summary>
///     How a JSON value reads outside its document: the text a message prints, whether
///     the value counts as present, and whether two values are strictly equal. Every
///     reader of parsed JSON shares these three readings instead of copying them.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
public static class JsonValues
{
    /// <summary>Gets the compact JSON options: no indentation and no escaping of quotes, apostrophes or angle brackets.</summary>
    static JsonSerializerOptions Compact { get; } = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    ///     The string a JSON value prints as in a message: a string is itself, a number
    ///     prints in invariant form, <c>true</c> and <c>false</c> print as themselves,
    ///     null prints as <c>null</c>, and objects and arrays print as compact JSON.
    /// </summary>
    /// <param name="value">The JSON value, or null for a JSON null or a missing value.</param>
    /// <returns>The value as message text.</returns>
    public static string Text(JsonNode? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (value is JsonValue scalar)
        {
            return scalar.GetValueKind() switch
            {
                JsonValueKind.String => scalar.GetValue<string>(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "null",
                JsonValueKind.Undefined => "null",
                JsonValueKind.Number => NumberText(scalar),
                _ => scalar.ToJsonString(Compact),
            };
        }

        return value.ToJsonString(Compact);
    }

    /// <summary>
    ///     Whether a JSON value counts as present, with JavaScript truthiness: null,
    ///     <c>false</c>, zero, NaN and the empty string are absent, and every object
    ///     and array is present.
    /// </summary>
    /// <param name="value">The JSON value, or null for a JSON null or a missing value.</param>
    /// <returns>Whether the value counts as present.</returns>
    public static bool IsTruthy(JsonNode? value)
    {
        return value switch
        {
            null => false,
            JsonValue scalar => scalar.GetValueKind() switch
            {
                JsonValueKind.String => scalar.GetValue<string>().Length > 0,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => false,
                JsonValueKind.Undefined => false,
                JsonValueKind.Number => NumberIsPresent(scalar),
                _ => true,
            },
            JsonArray => true,
            JsonObject => true,
            _ => false,
        };
    }

    /// <summary>
    ///     Whether two JSON values are strictly equal, as JavaScript <c>===</c> reads
    ///     them: primitives compare by type and value, with every number one type, and
    ///     objects and arrays compare by reference.
    /// </summary>
    /// <param name="left">The first JSON value, or null for a JSON null or a missing value.</param>
    /// <param name="right">The second JSON value, or null for a JSON null or a missing value.</param>
    /// <returns>Whether the two values are strictly equal.</returns>
    public static bool StrictEqual(JsonNode? left, JsonNode? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        if (left is JsonValue leftValue && right is JsonValue rightValue)
        {
            var leftKind = leftValue.GetValueKind();
            var rightKind = rightValue.GetValueKind();

            if (leftKind == JsonValueKind.Number && rightKind == JsonValueKind.Number)
            {
                return leftValue.TryGetValue<double>(out var leftNumber)
                    && rightValue.TryGetValue<double>(out var rightNumber)
                    && leftNumber == rightNumber;
            }

            if (leftKind != rightKind)
            {
                return false;
            }

            return leftKind switch
            {
                JsonValueKind.String => string.Equals(
                    leftValue.GetValue<string>(), rightValue.GetValue<string>(), StringComparison.Ordinal),
                JsonValueKind.True => true,
                JsonValueKind.False => true,
                JsonValueKind.Null => true,
                JsonValueKind.Undefined => true,
                _ => JsonNode.DeepEquals(left, right),
            };
        }

        if (left is JsonValue || right is JsonValue)
        {
            return false;
        }

        return ReferenceEquals(left, right);
    }

    /// <summary>Prints one JSON number in invariant form: whole numbers without a fraction, the rest as invariant decimals.</summary>
    /// <param name="scalar">The JSON number.</param>
    /// <returns>The number in invariant form.</returns>
    static string NumberText(JsonValue scalar)
    {
        if (scalar.TryGetValue<long>(out var integer))
        {
            return integer.ToString(CultureInfo.InvariantCulture);
        }

        if (scalar.TryGetValue<double>(out var number))
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        return scalar.ToJsonString(Compact);
    }

    /// <summary>Whether one JSON number counts as present: every number but zero and NaN.</summary>
    /// <param name="scalar">The JSON number.</param>
    /// <returns>Whether the number counts as present.</returns>
    static bool NumberIsPresent(JsonValue scalar) =>
        !scalar.TryGetValue<double>(out var number) || (number != 0 && !double.IsNaN(number));
}
