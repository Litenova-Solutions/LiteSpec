using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     Shared values for the book generator: HTML escaping, JSON string coercion,
///     truthiness and ordinal comparison, plus the record reads the writers need.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookRender
{
    static readonly JsonSerializerOptions CompactJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Compares two strings by code point, so the order is the same on every machine.</summary>
    public static int OrdinalCompare(string left, string right) => string.CompareOrdinal(left, right);

    /// <summary>An <see cref="IComparer{T}"/> that compares strings with <see cref="OrdinalCompare"/>.</summary>
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
    ///     arrays join with commas, objects become <c>[object Object]</c>.
    /// </summary>
    public static string Str(JsonNode? value)
    {
        return value switch
        {
            null => "",
            JsonValue scalar when scalar.TryGetValue<string>(out var text) => text ?? "",
            JsonValue scalar => scalar.ToJsonString(),
            JsonArray items => string.Join(",", items.Select(Str)),
            JsonObject => "[object Object]",
            _ => "",
        };
    }

    /// <summary>
    ///     A property read for display: a missing property is
    ///     <c>undefined</c>, a JSON null is <c>null</c>, anything else coerces.
    /// </summary>
    public static string PropText(JsonObject? obj, string name)
    {
        if (obj is null || !obj.TryGetPropertyValue(name, out var value))
        {
            return "undefined";
        }

        return value is null ? "null" : Str(value);
    }

    /// <summary>Tests presence: missing, empty-string, zero, false and NaN values are falsy, everything else is truthy.</summary>
    public static bool IsTruthy(JsonNode? value)
    {
        return value switch
        {
            null => false,
            JsonValue scalar when scalar.TryGetValue<bool>(out var flag) => flag,
            JsonValue scalar when scalar.TryGetValue<string>(out var text) => (text?.Length ?? 0) > 0,
            JsonValue scalar when scalar.TryGetValue<double>(out var number) => number != 0 && !double.IsNaN(number),
            JsonValue => true,
            JsonArray => true,
            JsonObject => true,
            _ => false,
        };
    }

    /// <summary>Whether the value holds the boolean <c>true</c>.</summary>
    public static bool IsTrue(JsonNode? value) =>
        value is JsonValue scalar && scalar.TryGetValue<bool>(out var flag) && flag;

    /// <summary>Coerces a value to a string, or returns the fallback when it is falsy.</summary>
    public static string OrStr(JsonNode? value, string fallback) => IsTruthy(value) ? Str(value) : fallback;

    /// <summary>Escapes the five HTML metacharacters.</summary>
    public static string Esc(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var built = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '&': built.Append("&amp;"); break;
                case '<': built.Append("&lt;"); break;
                case '>': built.Append("&gt;"); break;
                case '"': built.Append("&quot;"); break;
                case '\'': built.Append("&#39;"); break;
                default: built.Append(ch); break;
            }
        }

        return built.ToString();
    }

    /// <summary>Coerces a JSON value with <see cref="Str"/> and escapes it.</summary>
    public static string Esc(JsonNode? value) => Esc(Str(value));

    /// <summary>Coerces a value with a fallback and escapes it for HTML.</summary>
    public static string EscOr(JsonNode? value, string fallback) => Esc(OrStr(value, fallback));

    /// <summary>Serializes a value as compact JSON.</summary>
    public static string JsonCompact(JsonNode? value) => JsonSerializer.Serialize(value, CompactJson);

    /// <summary>Serializes a value as indented JSON.</summary>
    public static string JsonIndented(JsonNode? value) => JsonSerializer.Serialize(value, CliJson.Options);

    /// <summary>Reads a string property, or null when it is missing or not a string.</summary>
    public static string? StrProp(JsonObject? obj, string name) =>
        obj?[name] is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? text : null;

    /// <summary>Reads a record's <c>fields</c> object, or null when it is missing or not an object.</summary>
    public static JsonObject? Fields(JsonObject? record) => record?["fields"] as JsonObject;

    /// <summary>Reads a record's <c>links</c> object, or null when it is missing or not an object.</summary>
    public static JsonObject? Links(JsonObject? record) => record?["links"] as JsonObject;
}
