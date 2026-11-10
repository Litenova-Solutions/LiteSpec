using System.Buffers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Model;

/// <summary>
///     The two files a record lives in: the record file, <c>&lt;name&gt;.yaml</c>, holding
///     every field as YAML, and the optional explanation file beside
///     it, <c>&lt;name&gt;.md</c>, holding Markdown that explains the record. Every reader
///     and writer of record files goes through this type, so the file rules sit in one place.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class RecordFile
{
    /// <summary>The record file extension.</summary>
    public const string Extension = ".yaml";

    /// <summary>The explanation file extension.</summary>
    public const string ExplanationExtension = ".md";

    /// <summary>
    ///     The record types whose explanation is an article: required beside the
    ///     record file, because the article carries the record's reasoning.
    /// </summary>
    public static readonly IReadOnlySet<string> ArticleTypes =
        new HashSet<string>(StringComparer.Ordinal) { RecordTypes.Research, RecordTypes.Decision };

    /// <summary>Whether the path names a record file.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>True for a <c>.yaml</c> file.</returns>
    public static bool IsRecordFile(string path) => path.EndsWith(Extension, StringComparison.Ordinal);

    /// <summary>The explanation file beside a record file.</summary>
    /// <param name="recordPath">The record file path.</param>
    /// <returns>The explanation file path.</returns>
    public static string ExplanationPathOf(string recordPath) => Path.ChangeExtension(recordPath, ExplanationExtension);

    /// <summary>The record file beside an explanation file.</summary>
    /// <param name="explanationPath">The explanation file path.</param>
    /// <returns>The record file path.</returns>
    public static string RecordPathOf(string explanationPath) => Path.ChangeExtension(explanationPath, Extension);

    /// <summary>
    ///     Parses a record file's text.
    /// </summary>
    /// <param name="text">The file text.</param>
    /// <param name="root">The parsed value as JSON.</param>
    /// <param name="tree">The parsed value with the line of every node.</param>
    /// <param name="error">Why parsing failed, naming the line.</param>
    /// <returns>Whether the text parsed as YAML.</returns>
    public static bool TryParse(string text, out JsonElement root, out JsonText tree, out string error)
    {
        if (!TryParseTree(text, out tree, out error))
        {
            root = default;
            return false;
        }

        root = ElementOf(tree);
        return true;
    }

    /// <summary>
    ///     Parses a record file's text into its tree, keeping the line of every value.
    /// </summary>
    /// <param name="text">The file text.</param>
    /// <param name="tree">The parsed value with the line of every node.</param>
    /// <param name="error">Why parsing failed, naming the line.</param>
    /// <returns>Whether the text parsed as YAML.</returns>
    public static bool TryParseTree(string text, out JsonText tree, out string error)
    {
        try
        {
            tree = YamlText.Parse(text);
            error = "";
            return true;
        }
        catch (JsonTextException exception)
        {
            tree = null!;
            error = exception.Message;
            return false;
        }
    }

    /// <summary>
    ///     The JSON node of a tree, for the callers that edit a record as a JSON object. The tree is
    ///     converted node by node, never printed to text and parsed back.
    /// </summary>
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

    /// <summary>
    ///     The JSON element of a tree, for the readers that navigate records as JSON. The tree is
    ///     written once through a writer, never as text.
    /// </summary>
    /// <param name="value">The tree to convert.</param>
    /// <returns>The element, cloned so it outlives its document.</returns>
    public static JsonElement ElementOf(JsonText value)
    {
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            WriteValue(writer, value);
        }

        using var document = JsonDocument.Parse(buffer.WrittenMemory);

        return document.RootElement.Clone();
    }

    /// <summary>Writes one tree value through the writer.</summary>
    /// <param name="writer">The writer that receives the value.</param>
    /// <param name="value">The tree value to write.</param>
    private static void WriteValue(Utf8JsonWriter writer, JsonText value)
    {
        switch (value.Kind)
        {
            case JsonKind.Object:
                writer.WriteStartObject();

                foreach (var property in value.Properties)
                {
                    writer.WritePropertyName(property.Key);
                    WriteValue(writer, property.Value);
                }

                writer.WriteEndObject();
                break;
            case JsonKind.Array:
                writer.WriteStartArray();

                foreach (var item in value.Items)
                {
                    WriteValue(writer, item);
                }

                writer.WriteEndArray();
                break;
            case JsonKind.String:
                writer.WriteStringValue(value.GetString());
                break;
            case JsonKind.Number:
                writer.WriteRawValue(value.GetNumber()!, skipInputValidation: true);
                break;
            case JsonKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonKind.False:
                writer.WriteBooleanValue(false);
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }

    /// <summary>Writes a record in its canonical form.</summary>
    /// <param name="root">The record value.</param>
    /// <returns>The record file text.</returns>
    public static string Write(JsonNode root) => YamlWriter.Write(TreeOf(root), RecordKeyOrder.For);

    /// <summary>Converts a JSON node into the tree a record writes from, keeping the property order.</summary>
    /// <param name="node">The node to convert; null is the JSON null.</param>
    /// <returns>The tree value.</returns>
    private static JsonText TreeOf(JsonNode? node) => node switch
    {
        JsonObject obj => JsonText.ObjectOf(obj.Select(property => new KeyValuePair<string, JsonText>(property.Key, TreeOf(property.Value)))),
        JsonArray array => JsonText.ArrayOf(array.Select(TreeOf)),
        JsonValue value when value.TryGetValue(out string? text) => JsonText.Text(text),
        JsonValue value when value.TryGetValue(out bool flag) => JsonText.Boolean(flag),
        JsonValue value => JsonText.Number(value.ToJsonString()),
        _ => JsonText.Null(),
    };

    /// <summary>
    ///     The canonical text of a record file, or null when the text is not YAML.
    /// </summary>
    /// <param name="text">The file text.</param>
    /// <returns>The canonical text.</returns>
    public static string? Canonical(string text)
    {
        try
        {
            return YamlWriter.Write(YamlText.Parse(text), RecordKeyOrder.For);
        }
        catch (JsonTextException)
        {
            return null;
        }
    }

    /// <summary>
    ///     Whether the text already stands in canonical form. Line ends do not count: a
    ///     checkout may turn LF into CRLF, and the record reads the same either way.
    /// </summary>
    /// <param name="text">The file text.</param>
    /// <param name="canonical">The canonical text, or null when the text is not YAML.</param>
    /// <returns>True when the text differs from its canonical form in line ends at most.</returns>
    public static bool IsCanonical(string text, out string? canonical)
    {
        canonical = Canonical(text);
        return canonical is not null && canonical == text.Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
