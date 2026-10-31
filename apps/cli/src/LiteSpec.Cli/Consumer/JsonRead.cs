using System.Text.Json.Nodes;

namespace LiteSpec.Consumer;

/// <summary>
///     Reads JSON values for the consumer checks: the string a value carries,
///     and how a value or a property reads inside a message. The truthiness,
///     equality and plain text of a value come from <see cref="JsonValues"/>;
///     this type keeps only what that contract does not carry: a property read
///     distinguishes a missing property, which reads as <c>undefined</c>, from
///     a present one.
/// </summary>
/// <follows>litespec/core/authoring#declare-structured-specification-metadata</follows>
/// <follows>litespec/core/authoring#record-the-reviewed-litespec-release</follows>
/// <follows>litespec/core/authoring#reject-a-recorded-synonym-inside-its-scope</follows>
static class JsonRead
{
    /// <summary>
    ///     The string value of a JSON value, or null for anything else.
    /// </summary>
    /// <param name="node">The JSON value to read.</param>
    /// <returns>The string the value carries, or null when it carries none.</returns>
    public static string? AsString(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    /// <summary>
    ///     The string a property carries, or null when it is missing or carries none.
    /// </summary>
    /// <param name="container">The object holding the property, or null.</param>
    /// <param name="key">The property name.</param>
    /// <returns>The string the property carries, or null.</returns>
    public static string? PropString(JsonObject? container, string key) =>
        container is not null && container.TryGetPropertyValue(key, out var value) ? AsString(value) : null;

    /// <summary>
    ///     How a property reads inside a message: a missing property reads as <c>undefined</c>.
    /// </summary>
    /// <param name="container">The object holding the property, or null.</param>
    /// <param name="key">The property name.</param>
    /// <returns>The text the property prints as in a message.</returns>
    public static string PropText(JsonObject? container, string key) =>
        container is not null && container.TryGetPropertyValue(key, out var value)
            ? value is null ? "null" : JsonValues.Text(value)
            : "undefined";
}
