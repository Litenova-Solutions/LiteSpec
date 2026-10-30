using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     Shared HTML for the book generator: escaping, the page shell and navigation,
///     plus the value semantics (truthiness, string coercion, locale comparison) the pages need.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookRender
{
    /// <summary>
    ///     The English collation the generator sorts with, backed by the "en" culture.
    /// </summary>
    public static readonly CompareInfo Locale = CultureInfo.GetCultureInfo("en").CompareInfo;

    static readonly JsonSerializerOptions CompactJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Compares two strings with English collation.</summary>
    public static int LocaleCompare(string left, string right) => Locale.Compare(left, right, CompareOptions.None);

    /// <summary>An <see cref="IComparer{T}"/> that compares strings with <see cref="LocaleCompare"/>.</summary>
    public static IComparer<string> LocaleComparer { get; } = Comparer<string>.Create(LocaleCompare);

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
