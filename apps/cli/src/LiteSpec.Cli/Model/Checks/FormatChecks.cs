using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

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
            if (RecordFile.IsRecordFile(record.File))
            {
                subjects.TryAdd(record.File, SubjectOf(record));
            }
        }

        if (RecordFile.IsRecordFile(model.Config.GlossaryPath))
        {
            subjects.TryAdd(model.Config.GlossaryPath, null);
        }

        foreach (var file in subjects.Keys.Order(StringComparer.Ordinal))
        {
            string text;

            try
            {
                text = SourceTree.Read(file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            // A file outside the subset is record-invalid, which the reader reports.
            if (!RecordFile.IsCanonical(text, out var canonical) && canonical is not null)
            {
                findings.Add(new Finding("format-drift", "Run 'litespec model format'.", file, 1, subjects[file]));
            }
        }
    }
}
