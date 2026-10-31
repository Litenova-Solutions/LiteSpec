namespace LiteSpec.Model.Readers.Operations;

/// <summary>
///     Reads the follows marks of the deployment, proxy, host and workflow files
///     the project lists in paths.operations, from their hash comment lines,
///     through the shared mark parser.
/// </summary>
/// <follows>litespec/quality/operations#point-each-operations-file-at-its-decision</follows>
static class OperationsMarkReader
{
    /// <summary>Every operations file: a named file as named, every file below a named folder except its Markdown documentation.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <returns>The operations file paths in sorted order, without Markdown documentation.</returns>
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
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <returns>Every follows mark in the operations files.</returns>
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
