using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Model.Ui;

/// <summary>
///     Reads JSON properties for the UI checks, keeping each read's presence in
///     <see cref="JsValue"/>, and prints reads the way a message prints them.
///     The text, truthiness and strict equality of the values come from
///     <see cref="JsonValues"/>; this type keeps only what that contract does
///     not carry: a missing key is not a present null, and a missing key prints
///     as <c>undefined</c>.
/// </summary>
/// <follows>litespec/frontend/ui#select-one-visual-authority</follows>
/// <follows>litespec/frontend/ui#pin-the-baseline-in-the-shared-package</follows>
/// <follows>litespec/frontend/web/ui#track-source-changes</follows>
static class JsonRead
{
    /// <summary>The compact JSON options a baseline value prints with.</summary>
    static readonly JsonSerializerOptions CompactRelaxed = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Reads one property; a non-object or missing key is not found.</summary>
    /// <param name="node">The JSON value holding the property.</param>
    /// <param name="key">The property name.</param>
    /// <returns>The read, with its presence.</returns>
    public static JsValue Child(JsonNode? node, string key) =>
        node is JsonObject found && found.TryGetPropertyValue(key, out var value)
            ? new JsValue(true, value)
            : new JsValue(false, null);

    /// <summary>Reads one optional link of a <c>?.</c> chain.</summary>
    /// <param name="value">The read so far.</param>
    /// <param name="key">The property name.</param>
    /// <returns>The read, with its presence.</returns>
    public static JsValue Dot(JsValue value, string key) =>
        value.Node is JsonObject found ? Child(found, key) : new JsValue(false, null);

    /// <summary>Walks a <c>?.</c> chain from a value.</summary>
    /// <param name="node">The JSON value the chain starts from.</param>
    /// <param name="keys">The property names, in order.</param>
    /// <returns>The read, with its presence.</returns>
    public static JsValue Chain(JsonNode? node, params string[] keys)
    {
        var current = new JsValue(true, node);
        foreach (var key in keys)
        {
            current = Dot(current, key);
        }

        return current;
    }

    /// <summary>How a template string prints a read: missing is undefined, null is null.</summary>
    /// <param name="value">The read to print.</param>
    /// <returns>The text the read prints as in a message.</returns>
    public static string Show(JsValue value) =>
        value.Found ? JsonValues.Text(value.Node) : "undefined";

    /// <summary>A read with a fallback for a missing or null value.</summary>
    /// <param name="value">The read to print.</param>
    /// <param name="fallback">The text for a missing or null value.</param>
    /// <returns>The text the read prints as in a message.</returns>
    public static string ShowOr(JsValue value, string fallback) =>
        value.Node is null ? fallback : JsonValues.Text(value.Node);

    /// <summary>Whether a read counts as present.</summary>
    /// <param name="value">The read to test.</param>
    /// <returns>True when the read counts as present.</returns>
    public static bool Truthy(JsValue value) => value is { Found: true, Node: not null } && JsonValues.IsTruthy(value.Node);

    /// <summary>Whether two reads are strictly equal: primitives compare by type and value, objects and arrays by reference.</summary>
    /// <param name="left">The first read.</param>
    /// <param name="right">The second read.</param>
    /// <returns>True when the reads are strictly equal.</returns>
    public static bool StrictEqual(JsValue left, JsValue right)
    {
        if (!left.Found || !right.Found)
        {
            return left.Found == right.Found;
        }

        return JsonValues.StrictEqual(left.Node, right.Node);
    }

    /// <summary>How a message prints a baseline value: compact JSON, or undefined when missing.</summary>
    /// <param name="value">The read to print.</param>
    /// <returns>The text the read prints as in a message.</returns>
    public static string StringifyOrUndefined(JsValue value) =>
        !value.Found ? "undefined" : value.Node is null ? "null" : value.Node.ToJsonString(CompactRelaxed);

    /// <summary>The keys of the manifest's pinned npm packages.</summary>
    /// <param name="manifest">The parsed manifest.</param>
    /// <returns>The pinned npm package keys.</returns>
    public static HashSet<string> NpmKeys(JsonNode? manifest)
    {
        var npm = Chain(manifest, "packages", "npm").Node;
        return npm switch
        {
            JsonObject found => found.Select(property => property.Key).ToHashSet(StringComparer.Ordinal),
            JsonArray array => Enumerable.Range(0, array.Count).Select(index => index.ToString(CultureInfo.InvariantCulture)).ToHashSet(StringComparer.Ordinal),
            JsonValue value when value.TryGetValue<string>(out var text) => Enumerable.Range(0, text.Length).Select(index => index.ToString(CultureInfo.InvariantCulture)).ToHashSet(StringComparer.Ordinal),
            _ => new HashSet<string>(StringComparer.Ordinal),
        };
    }
}
