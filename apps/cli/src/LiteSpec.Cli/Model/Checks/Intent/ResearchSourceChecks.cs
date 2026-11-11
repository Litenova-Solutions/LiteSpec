using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Checks.RecordShape;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.ProseText;

namespace LiteSpec.Cli.Model.Checks.Intent;

/// <summary>The research source checks: a research explanation cites every source by key and links no URL directly.</summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
internal static class ResearchSourceChecks
{
    /// <summary>Reports source-link-bare, source-key-unknown and source-unused.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record => record.Type == RecordTypes.Research && !record.IsEntry))
        {
            if (record.ExplanationFile is not { } explanationFile || record.Explanation is not { } explanation)
            {
                continue;
            }

            var sources = SourceKeysOf(record);
            var declared = new HashSet<string>(sources, StringComparer.OrdinalIgnoreCase);
            var cited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var prose = LinkChecks.WithoutCode(explanation);
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

            foreach (var key in sources.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!cited.Contains(key))
                {
                    findings.Add(new Finding("source-unused", $"Research '{record.Id}' names source key '{key}', which its explanation cites nowhere.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }
    }

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

    /// <summary>Finds each URL outside code in a research explanation.</summary>
    private static readonly Regex BareUrl = new(@"https?://[^\s<>""')\]]+", RegexOptions.Compiled);

    /// <summary>Finds each reference link outside code, so its key resolves against the record's sources.</summary>
    private static readonly Regex ReferenceLink = new(@"(?<![!])\[(?<text>[^\]\r\n]+)\]\[(?<key>[^\]\r\n]*)\]", RegexOptions.Compiled);
}
