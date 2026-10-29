using System.Text.RegularExpressions;

namespace LiteSpec.Model.Readers.Operations;

/// <summary>One follows mark in an operations file.</summary>
/// <param name="File">The absolute file path.</param>
/// <param name="Line">The 1-based line.</param>
/// <param name="Id">The decision, deviation or provision the mark names.</param>
sealed record OperationsMark(string File, int Line, string Id);

/// <summary>
///     Reads the follows marks of the deployment, proxy, host and workflow files
///     the project lists in paths.operations, from their hash comment lines.
/// </summary>
/// <follows>litespec/quality/operations#point-each-operations-file-at-its-decision</follows>
static partial class OperationsMarkReader
{
    /// <summary>Every operations file: a named file as named, every file below a named folder except its Markdown documentation.</summary>
    public static IReadOnlyList<string> Files(ConsumerConfig config)
    {
        var files = new List<string>();

        foreach (var entry in config.Operations)
        {
            if (File.Exists(entry))
            {
                files.Add(entry);
            }
            else if (Directory.Exists(entry))
            {
                files.AddRange(SourceTree.Files(entry).Where(file => !file.EndsWith(".md", StringComparison.OrdinalIgnoreCase)));
            }
        }

        files.Sort(StringComparer.Ordinal);

        return files;
    }

    /// <summary>Reads every follows mark in the operations files.</summary>
    public static List<OperationsMark> Read(ConsumerConfig config)
    {
        var marks = new List<OperationsMark>();

        foreach (var file in Files(config))
        {
            string[] lines;

            try
            {
                lines = File.ReadAllLines(file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            for (var index = 0; index < lines.Length; index++)
            {
                if (!lines[index].TrimStart().StartsWith('#'))
                {
                    continue;
                }

                foreach (Match match in Follows().Matches(lines[index]))
                {
                    var id = match.Groups["id"].Value.Trim();

                    if (id.Length > 0)
                    {
                        marks.Add(new OperationsMark(file, index + 1, id));
                    }
                }
            }
        }

        return marks;
    }

    [GeneratedRegex(@"<follows>(?<id>[^<>]+)</follows>")]
    private static partial Regex Follows();
}
