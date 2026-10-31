using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace LiteSpec.Schemas;

/// <summary>
///     The JSON value helpers every schema keyword family shares.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class JsonSchemaSupport
{
    // Compact JSON with relaxed escaping, so structural comparison and diagnostic text stay stable.
    /// <summary>Gets the compact JSON options used for structural comparison and diagnostic text.</summary>
    internal static readonly JsonSerializerOptions CompactJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    // A schema repeats its patterns and references on every value it meets: a
    // record schema with hundreds of nested records resolves the same reference
    // thousands of times per page. Each distinct expression compiles once, each
    // reference resolves once per schema root, and the keyword gate walks each
    // schema once. The tables hold no diagnostic text, only the parsed shapes,
    // so every finding reads exactly as before.
    /// <summary>Gets the cache of compiled schema patterns, by expression.</summary>
    private static readonly ConcurrentDictionary<string, Regex> PatternCache = new(StringComparer.Ordinal);

    /// <summary>Gets the empty property map an object schema falls back to.</summary>
    internal static readonly JsonObject NoProperties = new();

    /// <summary>Returns the compiled pattern for an expression, compiling it once.</summary>
    /// <param name="pattern">The pattern expression.</param>
    /// <returns>The compiled pattern.</returns>
    internal static Regex CachedPattern(string pattern) =>
        PatternCache.GetOrAdd(pattern, expression => new Regex(expression, RegexOptions.Compiled));

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

    /// <summary>Tries to read a JSON value as a number.</summary>
    /// <param name="node">The JSON value.</param>
    /// <param name="value">The number, when the value holds one.</param>
    /// <returns>True when the value holds a number.</returns>
    internal static bool TryNumber(JsonNode? node, out double value)
    {
        if (node is JsonValue candidate && candidate.TryGetValue<double>(out var number))
        {
            value = number;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>Tries to read an array index from a pointer segment.</summary>
    /// <param name="key">The pointer segment.</param>
    /// <param name="array">The array to index.</param>
    /// <param name="element">The element, when the segment is a valid index.</param>
    /// <returns>True when the segment is a valid index.</returns>
    internal static bool TryArrayIndex(string key, JsonArray array, out JsonNode? element)
    {
        element = null;

        if (key.Length == 0 || (key.Length > 1 && key[0] == '0'))
        {
            return false;
        }

        foreach (var ch in key)
        {
            if (ch < '0' || ch > '9')
            {
                return false;
            }
        }

        if (!int.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index >= array.Count)
        {
            return false;
        }

        element = array[index];
        return true;
    }

    /// <summary>Prints a JSON value in compact form, or the text null for a missing value.</summary>
    /// <param name="node">The JSON value.</param>
    /// <returns>The compact JSON text.</returns>
    internal static string Stringify(JsonNode? node) => node is null ? "null" : node.ToJsonString(CompactJson);

    /// <summary>Reports whether two JSON values are deeply equal.</summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns>True when the values are deeply equal.</returns>
    internal static bool JsonEqual(JsonNode? left, JsonNode? right) => JsonNode.DeepEquals(left, right);

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
