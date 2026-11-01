using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     Shared values for the book generator: HTML escaping, JSON string coercion,
///     truthiness and ordinal comparison, plus the record reads the writers need.
///     The escaping and the JSON readings themselves live in <see cref="HtmlText"/>
///     and <see cref="JsonValues"/>; this type keeps the book's own display
///     coercion and its link check.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookRender
{
    /// <summary>Gets the compact JSON options, with no escaping of quotes, apostrophes or angle brackets.</summary>
    static JsonSerializerOptions CompactJson { get; } = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Compares two strings by code point, so the order is the same on every machine.</summary>
    /// <param name="left">The first string.</param>
    /// <param name="right">The second string.</param>
    /// <returns>The ordinal comparison of the two strings.</returns>
    public static int OrdinalCompare(string left, string right) => string.CompareOrdinal(left, right);

    /// <summary>Gets an <see cref="IComparer{T}"/> that compares strings with <see cref="OrdinalCompare"/>.</summary>
    public static IComparer<string> OrdinalComparer { get; } = Comparer<string>.Create(OrdinalCompare);

    /// <summary>
    ///     Whether a value may render as a link target. Allowed targets are
    ///     <c>http:</c>, <c>https:</c> and <c>mailto:</c> links, in-page
    ///     fragments, relative paths with no scheme and no network host, and
    ///     links in the configured editor scheme. Every other value renders
    ///     as plain text.
    /// </summary>
    /// <param name="value">The candidate link target, from record data.</param>
    /// <param name="editorScheme">The scheme of the configured editor template, without the colon, or null.</param>
    /// <returns>Whether the value may render as a link target.</returns>
    public static bool IsSafeHref(string? value, string? editorScheme = null)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var target = value.Trim();

        if (target.Length == 0 || target.Any(character => character < ' ' || character == '\u007f'))
        {
            return false;
        }

        if (target.StartsWith('#'))
        {
            return true;
        }

        var colon = target.IndexOf(':');
        var schemeEnd = colon;

        if (schemeEnd > 0)
        {
            for (var index = 0; index < schemeEnd; index++)
            {
                var character = target[index];

                if (!(character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '+' or '-' or '.')
                    || (index == 0 && character is >= '0' and <= '9'))
                {
                    schemeEnd = -1;
                    break;
                }
            }
        }

        if (schemeEnd > 0)
        {
            var scheme = target[..schemeEnd];

            return string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase)
                || string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase)
                || string.Equals(scheme, "mailto", StringComparison.OrdinalIgnoreCase)
                || (editorScheme is { Length: > 0 }
                    && string.Equals(scheme, editorScheme, StringComparison.OrdinalIgnoreCase));
        }

        return !target.StartsWith("//", StringComparison.Ordinal)
            && !target.StartsWith("\\\\", StringComparison.Ordinal);
    }

    /// <summary>
    ///     Coerces a value to its display string: missing values become the empty string,
    ///     arrays join with commas, objects become <c>[object Object]</c>, and a scalar
    ///     prints as <see cref="JsonValues.Text"/> prints it.
    /// </summary>
    /// <param name="value">The JSON value, or null for a missing value.</param>
    /// <returns>The display string.</returns>
    public static string Str(JsonNode? value)
    {
        return value switch
        {
            null => "",
            JsonArray items => string.Join(",", items.Select(Str)),
            JsonObject => "[object Object]",
            _ => JsonValues.Text(value),
        };
    }

    /// <summary>
    ///     A property read for display: a missing property is
    ///     <c>undefined</c>, a JSON null is <c>null</c>, anything else coerces.
    /// </summary>
    /// <param name="obj">The object holding the property, or null.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The property as display text.</returns>
    public static string PropText(JsonObject? obj, string name)
    {
        if (obj is null || !obj.TryGetPropertyValue(name, out var value))
        {
            return "undefined";
        }

        return value is null ? "null" : Str(value);
    }

    /// <summary>Tests presence: missing, empty-string, zero, false and NaN values are falsy, everything else is truthy.</summary>
    /// <param name="value">The JSON value, or null for a missing value.</param>
    /// <returns>Whether the value counts as present.</returns>
    public static bool IsTruthy(JsonNode? value) => JsonValues.IsTruthy(value);

    /// <summary>Whether the value holds the boolean <c>true</c>.</summary>
    /// <param name="value">The JSON value, or null for a missing value.</param>
    /// <returns>Whether the value is the boolean <c>true</c>.</returns>
    public static bool IsTrue(JsonNode? value) =>
        value is JsonValue scalar && scalar.TryGetValue<bool>(out var flag) && flag;

    /// <summary>Coerces a value to a string, or returns the fallback when it is falsy.</summary>
    /// <param name="value">The JSON value, or null for a missing value.</param>
    /// <param name="fallback">The text for a falsy value.</param>
    /// <returns>The coerced value or the fallback.</returns>
    public static string OrStr(JsonNode? value, string fallback) => IsTruthy(value) ? Str(value) : fallback;

    /// <summary>Escapes the five HTML metacharacters.</summary>
    /// <param name="value">The text to escape, or null.</param>
    /// <returns>The escaped text.</returns>
    public static string Esc(string? value) => HtmlText.Escape(value);

    /// <summary>Coerces a JSON value with <see cref="Str"/> and escapes it.</summary>
    /// <param name="value">The JSON value, or null for a missing value.</param>
    /// <returns>The coerced, escaped text.</returns>
    public static string Esc(JsonNode? value) => Esc(Str(value));

    /// <summary>Coerces a value with a fallback and escapes it for HTML.</summary>
    /// <param name="value">The JSON value, or null for a missing value.</param>
    /// <param name="fallback">The text for a falsy value.</param>
    /// <returns>The coerced, escaped text.</returns>
    public static string EscOr(JsonNode? value, string fallback) => Esc(OrStr(value, fallback));

    /// <summary>Serializes a value as compact JSON.</summary>
    /// <param name="value">The JSON value, or null.</param>
    /// <returns>The compact JSON.</returns>
    public static string JsonCompact(JsonNode? value) => JsonSerializer.Serialize(value, CompactJson);

    /// <summary>Serializes a value as indented JSON.</summary>
    /// <param name="value">The JSON value, or null.</param>
    /// <returns>The indented JSON.</returns>
    public static string JsonIndented(JsonNode? value) => JsonSerializer.Serialize(value, CliJson.Options);

    /// <summary>Reads a string property, or null when it is missing or not a string.</summary>
    /// <param name="obj">The object holding the property, or null.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The string property, or null.</returns>
    public static string? StrProp(JsonObject? obj, string name) =>
        obj?[name] is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? text : null;

    /// <summary>One nullable string as a JSON node, null staying null.</summary>
    /// <param name="value">The string, or null.</param>
    /// <returns>The JSON node, or null.</returns>
    public static JsonNode? StrNode(string? value) => value is null ? null : JsonValue.Create(value);

    /// <summary>Reads a record's <c>fields</c> object, or null when it is missing or not an object.</summary>
    /// <param name="record">The record, or null.</param>
    /// <returns>The fields object, or null.</returns>
    public static JsonObject? Fields(JsonObject? record) => record?["fields"] as JsonObject;

    /// <summary>Reads a record's <c>links</c> object, or null when it is missing or not an object.</summary>
    /// <param name="record">The record, or null.</param>
    /// <returns>The links object, or null.</returns>
    public static JsonObject? Links(JsonObject? record) => record?["links"] as JsonObject;
}
