using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Standards;

/// <summary>
///     Reads the LiteSpec files people write. A <c>.yaml</c> or <c>.yml</c> file is read through
///     <see cref="YamlText"/>, any other file as JSON, and both arrive as the same node tree, so a
///     reader and the schema evaluator never see which syntax the file used.
/// </summary>
/// <follows>litespec/core/scope#write-every-hand-written-litespec-file-in-yaml</follows>
public static class RecordText
{
    /// <summary>Reads a data file.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>The file's value, or null for a JSON null.</returns>
    /// <exception cref="JsonTextException">The text is not valid YAML or JSON; the message names the line.</exception>
    public static JsonNode? Read(string path) => Parse(FileText.TrimBom(FileText.ReadText(path)), IsYaml(path));

    /// <summary>Parses data text.</summary>
    /// <param name="text">The text.</param>
    /// <param name="yaml">Whether the text is YAML rather than JSON.</param>
    /// <returns>The value, or null for a JSON null.</returns>
    /// <exception cref="JsonTextException">The text is not valid YAML or JSON; the message names the line.</exception>
    public static JsonNode? Parse(string text, bool yaml) => NodeOf(yaml ? YamlText.Parse(text) : JsonText.Parse(text));

    /// <summary>Whether a path names a YAML file.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>True for a <c>.yaml</c> or <c>.yml</c> file.</returns>
    public static bool IsYaml(string path) =>
        path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase);

    /// <summary>Converts a node tree to the parsed tree the schema evaluator and the writers read, keeping property order.</summary>
    /// <param name="node">The node to convert.</param>
    /// <returns>The equivalent parsed tree.</returns>
    public static JsonText TextOf(JsonNode? node) => node switch
    {
        JsonObject container => JsonText.ObjectOf(container.Select(property => KeyValuePair.Create(property.Key, TextOf(property.Value)))),
        JsonArray array => JsonText.ArrayOf(array.Select(TextOf)),
        JsonValue value when value.TryGetValue<string>(out var text) => JsonText.Text(text),
        JsonValue value when value.TryGetValue<bool>(out var flag) => JsonText.Boolean(flag),
        JsonValue value => JsonText.Number(value.ToJsonString()),
        _ => JsonText.Null(),
    };

    /// <summary>Converts a parsed tree to a node, keeping property order.</summary>
    /// <param name="value">The tree to convert.</param>
    /// <returns>The node, or null for a JSON null.</returns>
    public static JsonNode? NodeOf(JsonText value) => value.Kind switch
    {
        JsonKind.Object => new JsonObject(value.Properties.Select(property => KeyValuePair.Create(property.Key, NodeOf(property.Value)))),
        JsonKind.Array => new JsonArray(value.Items.Select(NodeOf).ToArray()),
        JsonKind.String => JsonValue.Create(value.GetString()),
        JsonKind.Number => JsonNode.Parse(value.GetNumber()!),
        JsonKind.True => JsonValue.Create(true),
        JsonKind.False => JsonValue.Create(false),
        _ => null,
    };
}
