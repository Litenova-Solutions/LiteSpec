using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks;

/// <summary>The format check: every record file stands in its canonical form.</summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
internal static class FormatChecks
{
    /// <summary>Reports format-drift.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var subjects = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            if (RecordFiles.IsRecordFile(record.File))
            {
                subjects.TryAdd(record.File, SubjectOf(record));
            }
        }

        if (RecordFiles.IsRecordFile(model.Config.GlossaryPath))
        {
            subjects.TryAdd(model.Config.GlossaryPath, null);
        }

        // Each file is judged in parallel; the findings follow in path order.
        var files = subjects.Keys.Order(StringComparer.Ordinal).ToList();
        var drifted = new bool[files.Count];

        Parallel.For(0, files.Count, index => drifted[index] = Drifted(files[index]));

        for (var index = 0; index < files.Count; index++)
        {
            if (drifted[index])
            {
                findings.Add(new Finding("format-drift", "Run 'litespec model format'.", files[index], 1, subjects[files[index]]));
            }
        }
    }

    /// <summary>Whether the record file reads but is not in canonical form.</summary>
    /// <param name="file">The record file to judge.</param>
    /// <returns>True when the file has a canonical form it does not match; false when it matches or cannot be read.</returns>
    private static bool Drifted(string file)
    {
        string text;

        try
        {
            text = SourceTree.Read(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }

        // A file outside the subset is record-invalid, which the reader reports.
        return !RecordFile.IsCanonical(text, out var canonical) && canonical is not null;
    }
}
