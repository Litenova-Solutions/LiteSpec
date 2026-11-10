using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec;

/// <summary>
///     The JSON every command writes and reads: two-space indentation and no
///     escaping of quotes, apostrophes or angle brackets, so messages read the
///     same in JSON as in the human output.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
public static class CliJson
{
    /// <summary>The serializer options every command uses.</summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        // A fixed line end, so output is byte-identical on every operating system.
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    ///     Serializes a JSON node with <see cref="Options"/>. Every command builds its output as a
    ///     node tree, because the native build has no reflection to read an object's properties.
    /// </summary>
    /// <param name="value">The JSON node to serialize, or null for a JSON null.</param>
    /// <returns>The JSON text.</returns>
    public static string Serialize(JsonNode? value) => value?.ToJsonString(Options) ?? "null";

    /// <summary>Writes a JSON node to standard output with <see cref="Options"/>.</summary>
    /// <param name="value">The JSON node to write, or null for a JSON null.</param>
    public static void Write(JsonNode? value) => Console.WriteLine(Serialize(value));

    /// <summary>A JSON array of strings, in sequence order.</summary>
    /// <param name="values">The strings to hold.</param>
    /// <returns>The JSON array.</returns>
    public static JsonArray Strings(IEnumerable<string> values) => new([.. values.Select(value => (JsonNode?)value)]);

    /// <summary>A JSON object that maps each key to an array of strings, in sequence order.</summary>
    /// <param name="lists">The keys and their strings.</param>
    /// <returns>The JSON object.</returns>
    public static JsonObject StringLists(IEnumerable<KeyValuePair<string, List<string>>> lists)
    {
        var json = new JsonObject();

        foreach (var (key, values) in lists)
        {
            json[key] = Strings(values);
        }

        return json;
    }

    /// <summary>Reads a JSON file, or returns null when it is missing or not JSON.</summary>
    /// <param name="file">The file to read.</param>
    /// <returns>The JSON value, or null when the file is missing or not JSON.</returns>
    public static JsonNode? TryRead(string file)
    {
        try
        {
            return File.Exists(file) ? JsonNode.Parse(File.ReadAllText(file), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Disallow }) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
