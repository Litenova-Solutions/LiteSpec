namespace LiteSpec.Model;

/// <summary>
///     Rewrites every record file into canonical form, leaving all other files untouched.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class RecordFormatter
{
    /// <summary>
    ///     Formats every record file under the record roots and the glossary file: each
    ///     file that differs from its canonical form is rewritten, unless the run only
    ///     checks. Returns the files changed or that would change, and the files that
    ///     do not parse.
    /// </summary>
    /// <param name="config">The consumer configuration holding the record roots and the glossary.</param>
    /// <param name="check">Whether to list the files that would change without writing.</param>
    /// <returns>The changed files and the files that do not parse, relative to the root, in read order.</returns>
    public static (IReadOnlyList<string> Changed, IReadOnlyList<string> Invalid) Format(ConsumerConfig config, bool check)
    {
        var changed = new List<string>();
        var invalid = new List<string>();

        foreach (var file in FilesOf(config))
        {
            var text = SourceTree.Read(file);

            if (RecordFile.IsCanonical(text, out var canonical))
            {
                continue;
            }

            if (canonical is null)
            {
                invalid.Add(PathText.Relative(config.Root, file));
                continue;
            }

            if (!check)
            {
                File.WriteAllText(file, canonical);
            }

            changed.Add(PathText.Relative(config.Root, file));
        }

        return (changed, invalid);
    }

    /// <summary>Every file format covers: each record file under the record roots, then the glossary file.</summary>
    /// <param name="config">The consumer configuration holding the record roots and the glossary.</param>
    /// <returns>The absolute paths in read order, without duplicates.</returns>
    static List<string> FilesOf(ConsumerConfig config)
    {
        var files = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in config.RecordRoots)
        {
            foreach (var file in SourceTree.Files(root, RecordFile.Extension))
            {
                if (seen.Add(file))
                {
                    files.Add(file);
                }
            }
        }

        if (config.GlossaryPath.EndsWith(RecordFile.Extension, StringComparison.OrdinalIgnoreCase)
            && File.Exists(config.GlossaryPath)
            && seen.Add(config.GlossaryPath))
        {
            files.Add(config.GlossaryPath);
        }

        // The project file and the UI pattern catalog are YAML under the record root, but hold no record.
        files.RemoveAll(file => file.Equals(ConsumerRoot.ProjectFile(config.Root), StringComparison.OrdinalIgnoreCase)
            || file.Equals(config.UiPatternCatalog, StringComparison.OrdinalIgnoreCase));

        return files;
    }
}
