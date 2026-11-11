using System.Text.Json.Nodes;
using LiteSpec.Cli.Model;

namespace LiteSpec.Cli.Tests.Support;

/// <summary>
///     Writes record files and explanation files into a test consumer. A case states a
///     record's fields as JSON, the shape it reads best in C#, and the file on disk is the
///     canonical YAML record file the CLI reads.
/// </summary>
static class TestRecords
{
    /// <summary>Writes the record to its record file under the root, in canonical form.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="relative">The record file's path under the root, ending in <c>.yaml</c>.</param>
    /// <param name="json">The record's fields as one JSON object.</param>
    /// <returns>The record file's full path.</returns>
    public static string Write(string root, string relative, string json)
    {
        var path = Path.Join(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, RecordFile.Write(JsonNode.Parse(json)!));
        return path;
    }

    /// <summary>Writes the explanation file beside a record file.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="relative">The explanation file's path under the root, ending in <c>.md</c>.</param>
    /// <param name="markdown">The explanation's Markdown.</param>
    /// <returns>The explanation file's full path.</returns>
    public static string Explain(string root, string relative, string markdown)
    {
        var path = Path.Join(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, markdown);
        return path;
    }
}
