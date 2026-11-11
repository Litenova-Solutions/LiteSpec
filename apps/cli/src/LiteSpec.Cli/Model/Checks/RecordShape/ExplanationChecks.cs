using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.RecordShape;

/// <summary>The explanation checks: every article type carries its explanation file with its required sections, every record page on a required layer carries its explanation file, and every explanation file stands beside a record file.</summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
internal static class ExplanationChecks
{
    /// <summary>Reports explanation-orphaned, explanation-missing and project-invalid.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckOrphaned(model, findings);
        CheckMissing(model, findings);
        CheckRequiredLayers(model, findings);
        CheckUnclaimedLayers(model, findings);
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

                findings.Add(new Finding("explanation-orphaned", $"Explanation file '{PathText.Relative(model.Config.Root, file)}' stands beside no record file.", file, 1, null));
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
            if (!ExplanationSections.TryGetValue(record.Type, out var sections))
            {
                continue;
            }

            var label = LabelOf(record.Type);

            if (record.ExplanationFile is null || record.Explanation is null)
            {
                var expected = RecordFile.ExplanationPathOf(record.File);

                findings.Add(new Finding("explanation-missing", $"{label} '{record.Id}' holds no explanation file '{PathText.Relative(model.Config.Root, expected)}'.", record.File, record.Line, SubjectOf(record)));
                continue;
            }

            var headings = HeadingsOf(record.Explanation);

            foreach (var section in sections.Where(section => !section.Optional))
            {
                if (!headings.Contains("## " + section.Name))
                {
                    findings.Add(new Finding("explanation-missing", $"{label} '{record.Id}' holds no {section.Name} section in its explanation.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }
    }

    /// <summary>Reports each record page on a required layer with no explanation file, and each explanation lacking a required section or any H2 section.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRequiredLayers(ProjectModel model, List<Finding> findings)
    {
        if (model.Config.ExplanationLayers.Count == 0)
        {
            return;
        }

        var required = new HashSet<string>(model.Config.ExplanationLayers, StringComparer.Ordinal);

        foreach (var record in model.Records.Where(record => !record.IsEntry && !RecordFile.ArticleTypes.Contains(record.Type)))
        {
            if (!RecordTypes.LayerByType.TryGetValue(record.Type, out var layer) || !required.Contains(layer))
            {
                continue;
            }

            var label = LabelOf(record.Type);

            if (record.ExplanationFile is null || record.Explanation is null)
            {
                var expected = RecordFile.ExplanationPathOf(record.File);

                findings.Add(new Finding("explanation-missing", $"{label} '{record.Id}' holds no explanation file '{PathText.Relative(model.Config.Root, expected)}'.", record.File, record.Line, SubjectOf(record)));
                continue;
            }

            if (ExplanationSections.TryGetValue(record.Type, out var sections))
            {
                var headings = HeadingsOf(record.Explanation);

                foreach (var section in sections.Where(section => !section.Optional))
                {
                    if (!headings.Contains("## " + section.Name))
                    {
                        findings.Add(new Finding("explanation-missing", $"{label} '{record.Id}' holds no {section.Name} section in its explanation.", record.File, record.Line, SubjectOf(record)));
                    }
                }
            }
            else if (HeadingsOf(record.Explanation).Count == 0)
            {
                findings.Add(new Finding("explanation-missing", $"{label} '{record.Id}' holds no H2 section in its explanation.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Reports each required explanation layer the project does not claim in layers.</summary>
    /// <param name="model">The loaded project model that holds the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckUnclaimedLayers(ProjectModel model, List<Finding> findings)
    {
        if (model.Config.ExplanationLayers.Count == 0)
        {
            return;
        }

        var projectFile = ConsumerRoot.ProjectFile(model.Config.Root);

        foreach (var layer in model.Config.ExplanationLayers)
        {
            var claimed = layer switch
            {
                "intent" => model.Config.Layers.Intent,
                "behavior" => model.Config.Layers.Behavior,
                _ => true,
            };

            if (!claimed)
            {
                findings.Add(new Finding("project-invalid", $"explanations.layers names '{layer}', which the project does not claim in layers [litespec/core/model#require-explanation-files-on-the-layers-a-project-names]", projectFile, 1, null));
            }
        }
    }

    /// <summary>The H2 sections each record type with fixed sections requires in its explanation file, in order, with the sections that stay optional.</summary>
    private static readonly Dictionary<string, (string Name, bool Optional)[]> ExplanationSections = new(StringComparer.Ordinal)
    {
        [RecordTypes.Problem] = [("Who Has It", false), ("What They Do Today", false), ("Evidence", false), ("What Would Retire It", false)],
        [RecordTypes.Journey] = [("Who Walks and Why", false), ("Before It Starts", true), ("Example", false), ("What It Connects To", false)],
        [RecordTypes.Feature] = [("Who It Serves and Why", false), ("What It Offers", false), ("What Good Looks Like", false)],
        [RecordTypes.Goal] = [("Why It Matters", false), ("What Counts as Done", false), ("What It Connects To", false)],
        [RecordTypes.Actor] = [("Who They Are", false), ("What They Do", false), ("What They Can Reach", false)],
        [RecordTypes.Research] = [("Answer", false)],
        [RecordTypes.Decision] = [("Context", false)],
    };

    /// <summary>The prose label a finding shows for a record type: the type capitalized with hyphens as spaces.</summary>
    /// <param name="type">The record type identifier.</param>
    /// <returns>The capitalized prose label.</returns>
    private static string LabelOf(string type) =>
        type.Length == 0 ? "Record" : char.ToUpperInvariant(type[0]) + type.Substring(1).Replace('-', ' ');

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
