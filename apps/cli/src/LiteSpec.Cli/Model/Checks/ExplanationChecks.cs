using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The explanation checks: every article type carries its explanation file with its required sections, and every explanation file stands beside a record file.</summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
internal static class ExplanationChecks
{
    /// <summary>Reports explanation-orphaned and explanation-missing.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckOrphaned(model, findings);
        CheckMissing(model, findings);
    }

    /// <summary>Reports each Markdown file under a record root that is neither a document nor an explanation file.</summary>
    /// <param name="model">The loaded project model that holds the record roots.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckOrphaned(ProjectModel model, List<Finding> findings)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var root in model.Config.RecordRoots)
        {
            foreach (var file in SourceTree.Files(root, ".md"))
            {
                if (!seen.Add(file))
                {
                    continue;
                }

                if (Path.GetFileName(file).Equals("README.md", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string text;

                try
                {
                    text = SourceTree.Read(file);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    continue;
                }

                // A file opening with a frontmatter fence is a document, and a file
                // with a record file beside it is an explanation. Anything else is
                // an orphaned explanation.
                if (HasFrontmatterFence(text) || File.Exists(RecordFile.RecordPathOf(file)))
                {
                    continue;
                }

                findings.Add(new Finding("explanation-orphaned", $"Explanation file '{model.Config.Relative(file)}' stands beside no record file.", file, 1, null));
            }
        }
    }

    /// <summary>Whether the text opens with a frontmatter fence line, so the file is a document.</summary>
    /// <param name="text">The file text to test.</param>
    /// <returns>True when the first line of the text is the frontmatter fence.</returns>
    private static bool HasFrontmatterFence(string text)
    {
        var firstLine = text.Split('\n')[0].TrimEnd('\r');

        return string.Equals(firstLine, "---", StringComparison.Ordinal);
    }

    /// <summary>Reports each article page with no explanation file, and each explanation lacking a required section.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckMissing(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record => !record.IsEntry && RecordFile.ArticleTypes.Contains(record.Type)))
        {
            if (!RequiredSections.TryGetValue(record.Type, out var sections))
            {
                continue;
            }

            var label = record.Type switch
            {
                RecordTypes.Research => "Research",
                _ => "Decision",
            };

            if (record.ExplanationFile is null || record.Explanation is null)
            {
                var expected = RecordFile.ExplanationPathOf(record.File);

                findings.Add(new Finding("explanation-missing", $"{label} '{record.Id}' holds no explanation file '{model.Config.Relative(expected)}'.", record.File, record.Line, SubjectOf(record)));
                continue;
            }

            var headings = HeadingsOf(record.Explanation);

            foreach (var section in sections)
            {
                if (!headings.Contains("## " + section))
                {
                    findings.Add(new Finding("explanation-missing", $"{label} '{record.Id}' holds no {section} section in its explanation.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }
    }

    /// <summary>The H2 sections each article type requires in its explanation file.</summary>
    private static readonly Dictionary<string, string[]> RequiredSections = new(StringComparer.Ordinal)
    {
        [RecordTypes.Research] = ["Answer"],
        [RecordTypes.Decision] = ["Context"],
    };

    /// <summary>Every H2 heading line of an explanation file, trimmed, as the page contract reads them.</summary>
    /// <param name="explanation">The explanation file text.</param>
    /// <returns>The trimmed H2 heading lines.</returns>
    private static HashSet<string> HeadingsOf(string explanation)
    {
        var headings = new HashSet<string>(StringComparer.Ordinal);

        foreach (var line in explanation.Split('\n'))
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("## ", StringComparison.Ordinal))
            {
                headings.Add(trimmed);
            }
        }

        return headings;
    }
}
