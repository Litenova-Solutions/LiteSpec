namespace LiteSpec.Model.Readers.Operations;

/// <summary>One follows mark in an operations file.</summary>
/// <param name="File">The absolute file path.</param>
/// <param name="Line">The 1-based line.</param>
/// <param name="Id">The decision, exception or provision the mark names.</param>
/// <param name="Style">The style the mark was written in: always the comment.</param>
sealed record OperationsMark(string File, int Line, string Id, MarkStyle Style = MarkStyle.Comment);

/// <summary>
///     Reads the follows marks of the deployment, proxy, host and workflow files
///     the project lists in paths.operations, from their hash comment lines,
///     through the shared mark parser.
/// </summary>
/// <follows>litespec/quality/operations#point-each-operations-file-at-its-decision</follows>
static class OperationsMarkReader
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

                foreach (var mark in MarkParser.ReadTags(lines[index]))
                {
                    if (mark.Verb == MarkVerb.Follows)
                    {
                        marks.Add(new OperationsMark(file, index + 1, mark.RecordId, MarkStyle.Comment));
                    }
                }
            }
        }

        return marks;
    }
}
