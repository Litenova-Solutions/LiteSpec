using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Checks.RecordShape;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.ProseText;

namespace LiteSpec.Cli.Model.Checks.Intent;

/// <summary>The source checks: a research or constraint explanation cites every source by key and links no URL directly.</summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
internal static class ResearchSourceChecks
{
    /// <summary>Reports source-link-bare, source-key-unknown and source-unused on research and constraint explanations.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record => (record.Type is RecordTypes.Research or RecordTypes.Constraint) && !record.IsEntry))
        {
            if (record.ExplanationFile is not { } explanationFile || record.Explanation is not { } explanation)
            {
                continue;
            }

            var sources = SourceKeysOf(record);
            var declared = new HashSet<string>(sources, StringComparer.OrdinalIgnoreCase);
            var cited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var prose = LinkChecks.WithoutCode(SourceLinks.Strip(explanation));
            var starts = LineStarts(explanation);

            // A reference link cites its source by key even when its label shows the URL,
            // so labels are blanked, keeping every offset, before bare URLs are read.
            var unlabelled = ReferenceLink.Replace(prose, link =>
                "[" + new string(' ', link.Groups["text"].Length) + link.Value[(1 + link.Groups["text"].Length)..]);

            foreach (Match match in BareUrl.Matches(unlabelled))
            {
                findings.Add(new Finding("source-link-bare", $"Research explanation links '{Identifiers.TrimCitation(match.Value)}' directly; cite a source key instead.", explanationFile, LineOf(starts, match.Index), SubjectOf(record)));
            }

            foreach (Match match in ReferenceLink.Matches(prose))
            {
                var key = match.Groups["key"].Value.Trim();

                if (key.Length == 0)
                {
                    key = match.Groups["text"].Value.Trim();
                }

                cited.Add(key);

                if (!declared.Contains(key))
                {
                    findings.Add(new Finding("source-key-unknown", $"Research explanation cites source key '{key}', which the record's sources do not name.", explanationFile, LineOf(starts, match.Index), SubjectOf(record)));
                }
            }

            if (record.Type == RecordTypes.Research && SectionOf(prose, FindingsSection) is { } findingsBody)
            {
                CheckFindingSources(record, explanationFile, prose, findingsBody, starts, findings);
            }

            foreach (var key in sources.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!cited.Contains(key))
                {
                    findings.Add(new Finding("source-unused", $"Record '{record.Id}' names source key '{key}', which its explanation cites nowhere.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }
    }

    /// <summary>Reports finding-source-missing for each Findings paragraph that cites no source key with a reference link.</summary>
    /// <param name="record">The research record whose Findings are read.</param>
    /// <param name="explanationFile">The explanation file the Findings sit in.</param>
    /// <param name="prose">The explanation prose with code and the generated region blanked, offset for offset.</param>
    /// <param name="findingsBody">The text of the Findings section.</param>
    /// <param name="starts">The line starts of the explanation, for line numbers.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFindingSources(Record record, string explanationFile, string prose, string findingsBody, int[] starts, List<Finding> findings)
    {
        foreach (var paragraph in BlankLine.Split(findingsBody.Trim()))
        {
            var text = paragraph.Trim();

            if (text.Length == 0 || text.StartsWith('#') || text.StartsWith("<!--", StringComparison.Ordinal) || ReferenceLink.IsMatch(text))
            {
                continue;
            }

            var line = LineOf(starts, prose.IndexOf(text, StringComparison.Ordinal));

            findings.Add(new Finding(
                "finding-source-missing",
                $"Research '{record.Id}' states a finding with no source link: '{Excerpt(text)}'.",
                explanationFile,
                line,
                SubjectOf(record)));
        }
    }

    /// <summary>The first words of a paragraph, for a finding message.</summary>
    /// <param name="text">The paragraph text.</param>
    /// <returns>The first 60 characters, with an ellipsis when the paragraph is longer.</returns>
    private static string Excerpt(string text) =>
        text.Length <= 60 ? text : text[..60] + "...";

    /// <summary>The source keys a research record declares.</summary>
    /// <param name="record">The research record whose sources are read.</param>
    /// <returns>Each declared source key in written order.</returns>
    private static List<string> SourceKeysOf(Record record)
    {
        var keys = new List<string>();

        if (record.Raw.ValueKind == JsonValueKind.Object
            && record.Raw.TryGetProperty(RecordFields.Sources, out var sources)
            && sources.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in sources.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.Object
                    && entry.TryGetProperty(RecordFields.Key, out var key)
                    && key.ValueKind == JsonValueKind.String
                    && key.GetString() is { Length: > 0 } name)
                {
                    keys.Add(name);
                }
            }
        }

        return keys;
    }

    /// <summary>Splits a text at each blank line, the boundary of one paragraph.</summary>
    private static readonly Regex BlankLine = new(@"\n[ \t]*\n", RegexOptions.Compiled);

    /// <summary>The H2 heading of the section that holds a research record's findings.</summary>
    private const string FindingsSection = "Findings";

    /// <summary>Finds each URL outside code in a research explanation.</summary>
    private static readonly Regex BareUrl = new(@"https?://[^\s<>""')\]]+", RegexOptions.Compiled);

    /// <summary>Finds each reference link outside code, so its key resolves against the record's sources.</summary>
    private static readonly Regex ReferenceLink = new(@"(?<![!])\[(?<text>[^\]\r\n]+)\]\[(?<key>[^\]\r\n]*)\]", RegexOptions.Compiled);
}
