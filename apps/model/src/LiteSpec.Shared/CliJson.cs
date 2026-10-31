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

    /// <summary>Serializes a value with <see cref="Options"/>.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The JSON text.</returns>
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    /// <summary>Writes a value to standard output with <see cref="Options"/>.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to write.</param>
    public static void Write<T>(T value) => Console.WriteLine(Serialize(value));

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
