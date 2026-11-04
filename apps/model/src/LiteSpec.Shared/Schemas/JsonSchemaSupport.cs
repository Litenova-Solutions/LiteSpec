using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Schemas;

/// <summary>
///     The JSON value helpers the schema check shares.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class JsonSchemaSupport
{
    // Compact JSON with relaxed escaping, so structural comparison and diagnostic text stay stable.
    /// <summary>Gets the compact JSON options used for structural comparison and diagnostic text.</summary>
    internal static readonly JsonSerializerOptions CompactJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Reports the JavaScript truthiness of a JSON value.</summary>
    /// <param name="node">The JSON value.</param>
    /// <returns>True when the value is truthy.</returns>
    internal static bool IsTruthy(JsonNode? node) => node switch
    {
        null => false,
        JsonObject => true,
        JsonArray => true,
        JsonValue value when value.TryGetValue<string>(out var text) => !string.IsNullOrEmpty(text),
        JsonValue value when value.TryGetValue<bool>(out var flag) => flag,
        JsonValue value when value.TryGetValue<double>(out var number) => number != 0 && !double.IsNaN(number),
        _ => true,
    };

    /// <summary>Tries to read a schema number, whatever numeric form the parser gave it.</summary>
    /// <param name="node">The JSON value.</param>
    /// <param name="value">The number, when the value holds one.</param>
    /// <returns>True when the value holds a number.</returns>
    internal static bool TrySchemaNumber(JsonNode? node, out double value)
    {
        // A schema number arrives int-backed, long-backed, or double-backed
        // depending on the parser, and each form reads differently, so the
        // check reads the JSON text every form prints.
        var text = node?.ToJsonString();

        if (text is not null && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>Prints a JSON value in compact form, or the text null for a missing value.</summary>
    /// <param name="node">The JSON value.</param>
    /// <returns>The compact JSON text.</returns>
    internal static string Stringify(JsonNode? node) => node is null ? "null" : node.ToJsonString(CompactJson);

    /// <summary>Reads a required-property name from a JSON value.</summary>
    /// <param name="node">The JSON value.</param>
    /// <returns>The property name.</returns>
    internal static string RequiredName(JsonNode? node) =>
        node is JsonValue candidate && candidate.TryGetValue<string>(out var name) && name is not null ? name : Stringify(node);

    /// <summary>Removes the first occurrence of a location prefix from a message.</summary>
    /// <param name="message">The message to strip.</param>
    /// <param name="location">The location whose prefix is removed.</param>
    /// <returns>The message without the prefix.</returns>
    internal static string StripLocationPrefix(string message, string location)
    {
        // Removes the first occurrence of the location prefix.
        var prefix = $"{location}: ";
        var index = message.IndexOf(prefix, StringComparison.Ordinal);
        return index >= 0 ? message.Remove(index, prefix.Length) : message;
    }

    /// <summary>Prints a number in JavaScript form.</summary>
    /// <param name="value">The number to print.</param>
    /// <returns>The JavaScript number text.</returns>
    internal static string JsNumber(double value)
    {
        if (double.IsPositiveInfinity(value))
        {
            return "Infinity";
        }

        if (double.IsNegativeInfinity(value))
        {
            return "-Infinity";
        }

        if (double.IsNaN(value))
        {
            return "NaN";
        }

        if (value == 0)
        {
            return "0";
        }

        if (double.IsFinite(value) && value == Math.Truncate(value) && Math.Abs(value) < 1e21)
        {
            return value.ToString("F0", CultureInfo.InvariantCulture);
        }

        var text = value.ToString("G", CultureInfo.InvariantCulture);
        var exponentAt = text.IndexOf('E');

        if (exponentAt < 0)
        {
            return text;
        }

        var exponent = int.Parse(text[(exponentAt + 1)..], CultureInfo.InvariantCulture);
        return text[..exponentAt] + "e" + (exponent < 0 ? "-" : "+") + Math.Abs(exponent).ToString(CultureInfo.InvariantCulture);
    }
}
