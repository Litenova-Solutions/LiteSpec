using System.Text.Json;
using LiteSpec.Cli.Files;
using Markdig.Syntax;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Rules;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.RecordShape;

/// <summary>The explanation checks: every article type and every record page on a required layer carries its explanation file, each explanation holds its fixed sections in order and opens with a paragraph, and every explanation file stands beside a record file.</summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
internal static class ExplanationChecks
{
    /// <summary>Reports explanation-orphaned, explanation-missing, explanation-opening-missing and project-invalid.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckOrphaned(model, findings);
        CheckMissing(model, findings);
        CheckSections(model, findings);
        CheckOpening(model, findings);
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
                // an orphaned explanation, except a project convention, which rules reads.
                if (HasFrontmatterFence(text) || File.Exists(RecordFile.RecordPathOf(file)) || ConventionFiles.InConventionsFolder(file))
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

    /// <summary>Reports each record that needs an explanation file and has none: an article type, or a record on a required layer.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckMissing(ProjectModel model, List<Finding> findings)
    {
        var required = new HashSet<string>(model.Config.ExplanationLayers, StringComparer.Ordinal);

        foreach (var record in model.Records.Where(record => !record.IsEntry && NeedsExplanation(record.Type, required)))
        {
            if (record.ExplanationFile is not null && record.Explanation is not null)
            {
                continue;
            }

            var expected = RecordFile.ExplanationPathOf(record.File);

            findings.Add(new Finding("explanation-missing", $"{LabelOf(record.Type)} '{record.Id}' holds no explanation file '{PathText.Relative(model.Config.Root, expected)}'.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>
    ///     Whether a record type needs an explanation file: an article type, or a type on a layer the project requires.
    ///     A requirement's statement is its lead, so its explanation stays optional even on a required layer.
    /// </summary>
    /// <param name="type">The record type.</param>
    /// <param name="required">The record layers whose every record page carries an explanation file.</param>
    /// <returns>True when the type needs an explanation file.</returns>
    private static bool NeedsExplanation(string type, HashSet<string> required) =>
        type != RecordTypes.Requirement
        && (RecordFile.ArticleTypes.Contains(type)
            || (RecordTypes.LayerByType.TryGetValue(type, out var layer) && required.Contains(layer)));

    /// <summary>
    ///     Reports each explanation that lacks a required section of its type's table, or holds its sections out of
    ///     order. Covers the explanation of every record and of every container page whose type the table lists.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and page body.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSections(ProjectModel model, List<Finding> findings)
    {
        foreach (var explanation in Explanations(model))
        {
            if (!ExplanationSections.ByType.TryGetValue(explanation.Type, out var sections))
            {
                continue;
            }

            var label = LabelOf(explanation.Type);
            var headings = HeadingsOf(explanation.Text);

            foreach (var section in sections.Where(section => !section.Optional))
            {
                if (!headings.Any(heading => heading.Text == "## " + section.Name))
                {
                    findings.Add(new Finding("explanation-missing", $"{label} '{explanation.Name}' holds no {section.Name} section in its explanation.", explanation.File, 1, explanation.Subject));
                }
            }

            if (OutOfOrder(headings, sections) is { } pair)
            {
                findings.Add(new Finding("explanation-missing", $"{label} '{explanation.Name}' holds its sections out of order: {pair.Later} must follow {pair.Earlier}.", explanation.File, pair.Line, explanation.Subject));
            }

            // A free section may follow the fixed sections, so only a section ahead of the last fixed one is reported.
            var lastFixed = headings.FindLastIndex(heading => sections.Any(section => heading.Text == "## " + section.Name));

            for (var at = 0; at < lastFixed; at++)
            {
                var heading = headings[at];

                if (!sections.Any(section => heading.Text == "## " + section.Name))
                {
                    findings.Add(new Finding("explanation-section-unknown", $"{label} '{explanation.Name}' holds the section '{heading.Text[3..]}' ahead of its fixed sections, and its type's section table does not state it. Free sections follow the fixed ones.", explanation.File, heading.Line, explanation.Subject));
                }
            }
        }
    }

    /// <summary>
    ///     The first pair of fixed sections that a file holds in the wrong order: a section the table puts first
    ///     that the file holds after a section the table puts later.
    /// </summary>
    /// <param name="headings">The H2 headings of the explanation file, in file order.</param>
    /// <param name="sections">The fixed sections of the record type, in table order.</param>
    /// <returns>The earlier and later section names and the line of the later heading, or null when the fixed sections keep their order.</returns>
    private static (string Earlier, string Later, int Line)? OutOfOrder(IReadOnlyList<Heading> headings, IReadOnlyList<ExplanationSections.Section> sections)
    {
        var position = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var index = 0; index < sections.Count; index++)
        {
            position["## " + sections[index].Name] = index;
        }

        int? previous = null;

        foreach (var heading in headings)
        {
            if (!position.TryGetValue(heading.Text, out var index))
            {
                continue;
            }

            if (previous is { } before && index < before)
            {
                return (sections[index].Name, sections[before].Name, heading.Line);
            }

            previous = index;
        }

        return null;
    }

    /// <summary>
    ///     Reports each explanation that opens with no paragraph between its H1 and its first H2.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and page body.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckOpening(ProjectModel model, List<Finding> findings)
    {
        foreach (var explanation in Explanations(model))
        {
            if (HasOpeningParagraph(explanation.Text))
            {
                continue;
            }

            findings.Add(new Finding("explanation-opening-missing", $"{LabelOf(explanation.Type)} '{explanation.Name}' opens with no paragraph between its H1 and its first H2.", explanation.File, explanation.Line, explanation.Subject));
        }
    }

    /// <summary>
    ///     Every explanation file the checks read: the explanation of each record that carries one, and the explanation of
    ///     each container page, whose record type the record file names.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and page body.</param>
    /// <returns>Each explanation with its record type, name, text, file, line and subject.</returns>
    private static IEnumerable<Explained> Explanations(ProjectModel model)
    {
        foreach (var record in model.Records.Where(record => !record.IsEntry && record.Explanation is not null))
        {
            yield return new Explained(record.Type, record.Id, record.Explanation!, record.ExplanationFile ?? RecordFile.ExplanationPathOf(record.File), record.Line, SubjectOf(record), record.File);
        }

        foreach (var body in model.Bodies.Where(body => body.PageId is null))
        {
            var recordPath = RecordFile.RecordPathOf(body.File);

            if (!File.Exists(recordPath) || TypeOfContainer(recordPath) is not { } type)
            {
                continue;
            }

            yield return new Explained(type, PathText.Relative(model.Config.Root, recordPath), body.Body, body.File, body.BodyStartLine, null, recordPath);
        }
    }

    /// <summary>The record type a container page names in its top level, or null when the file names none.</summary>
    /// <param name="recordPath">The container record file path.</param>
    /// <returns>The container page style, or null when the file cannot be read or names no type.</returns>
    private static string? TypeOfContainer(string recordPath)
    {
        try
        {
            if (RecordFile.TryParse(SourceTree.Read(recordPath), out var root, out _, out _)
                && root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty(RecordFields.Type, out var type)
                && type.ValueKind == JsonValueKind.String)
            {
                return type.GetString();
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }

        return null;
    }

    /// <summary>
    ///     Whether an explanation holds a paragraph block before its first H2. The block structure decides it: an HTML
    ///     comment, a fenced code block, a list or a table before the first H2 does not count as the opening paragraph.
    /// </summary>
    /// <param name="text">The explanation file text.</param>
    /// <returns>True when a paragraph block sits before the first H2 heading block.</returns>
    private static bool HasOpeningParagraph(string text)
    {
        foreach (var block in PageReader.Parse(text).Document)
        {
            switch (block)
            {
                case HeadingBlock { Level: 1 }:
                    continue;
                case HeadingBlock:
                    return false;
                case ParagraphBlock:
                    return true;
            }
        }

        return false;
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

    /// <summary>One explanation file the checks read, with the record type it explains.</summary>
    /// <param name="Type">The record type, or the container page style.</param>
    /// <param name="Name">The record identifier, or the record file path for a container page.</param>
    /// <param name="Text">The explanation file text.</param>
    /// <param name="File">The explanation file path.</param>
    /// <param name="Line">The line a finding points at.</param>
    /// <param name="Subject">The record identifier a finding names, or null for a container page.</param>
    /// <param name="RecordPath">The record file that holds the explained record, which the section findings point at.</param>
    private sealed record Explained(string Type, string Name, string Text, string File, int Line, string? Subject, string RecordPath);

    /// <summary>The prose label a finding shows for a record type: the type capitalized with hyphens as spaces.</summary>
    /// <param name="type">The record type identifier.</param>
    /// <returns>The capitalized prose label.</returns>
    private static string LabelOf(string type) =>
        type.Length == 0 ? "Record" : char.ToUpperInvariant(type[0]) + type.Substring(1).Replace('-', ' ');

    /// <summary>
    ///     Every H2 heading block of an explanation file, in file order. A heading written inside a fenced code block
    ///     or an HTML comment is not a heading block, so it is not counted.
    /// </summary>
    /// <param name="explanation">The explanation file text.</param>
    /// <returns>Each H2 heading with its text as <c>## Name</c> and its 1-based line.</returns>
    private static List<Heading> HeadingsOf(string explanation) =>
        PageReader.Parse(explanation).Headings
            .Where(heading => heading.Level == 2)
            .Select(heading => new Heading("## " + heading.Text.Trim(), heading.Line))
            .ToList();

    /// <summary>One H2 heading of an explanation file: its text as <c>## Name</c> and its 1-based line.</summary>
    /// <param name="Text">The heading with its level marker, such as <c>## Evidence</c>.</param>
    /// <param name="Line">The 1-based line the heading starts on.</param>
    private sealed record Heading(string Text, int Line);
}
