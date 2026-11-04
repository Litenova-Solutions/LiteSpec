using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Model;

/// <summary>
///     The two files a record lives in: the record file, <c>&lt;name&gt;.yaml</c>, holding
///     every field in the strict YAML subset, and the optional explanation file beside
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
    /// <returns>Whether the text is in the YAML subset.</returns>
    public static bool TryParse(string text, out JsonElement root, out JsonText tree, out string error)
    {
        try
        {
            tree = YamlText.Parse(text);
            using var document = JsonDocument.Parse(tree.ToJson());
            root = document.RootElement.Clone();
            error = "";
            return true;
        }
        catch (JsonTextException exception)
        {
            root = default;
            tree = null!;
            error = exception.Message;
            return false;
        }
    }

    /// <summary>Writes a record in its canonical form.</summary>
    /// <param name="root">The record value.</param>
    /// <returns>The record file text.</returns>
    public static string Write(JsonElement root) => YamlWriter.Write(JsonText.Parse(root.GetRawText()), RecordKeyOrder.For);

    /// <summary>Writes a record in its canonical form.</summary>
    /// <param name="root">The record value.</param>
    /// <returns>The record file text.</returns>
    public static string Write(JsonNode root) => YamlWriter.Write(JsonText.Parse(root.ToJsonString()), RecordKeyOrder.For);

    /// <summary>
    ///     The canonical text of a record file, or null when the text is not in the subset.
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
    /// <param name="canonical">The canonical text, or null when the text is not in the subset.</param>
    /// <returns>True when the text differs from its canonical form in line ends at most.</returns>
    public static bool IsCanonical(string text, out string? canonical)
    {
        canonical = Canonical(text);
        return canonical is not null && canonical == text.Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
