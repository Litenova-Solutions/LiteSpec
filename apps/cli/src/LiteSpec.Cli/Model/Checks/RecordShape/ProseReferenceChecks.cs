using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.ProseText;

namespace LiteSpec.Cli.Model.Checks.RecordShape;

/// <summary>The prose reference checks: every record identifier and provision identifier prose names in backticks resolves to a written record or a provision the carried release ships.</summary>
/// <follows>litespec/core/model#name-records-in-prose-by-their-identifiers</follows>
internal static class ProseReferenceChecks
{
    /// <summary>Reports reference-unknown for each backticked identifier in record fields, explanations, documents and the agent context that resolves to no record or shipped provision.</summary>
    /// <param name="model">The loaded project model that holds every record, explanation, document and carried provision.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records)
        {
            if (record.Raw.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var named = new HashSet<string>(StringComparer.Ordinal);

            foreach (var property in record.Raw.EnumerateObject())
            {
                if (property.Name is RecordFields.Id or RecordFields.Type)
                {
                    continue;
                }

                foreach (var text in StringsOf(property.Value))
                {
                    foreach (Match match in InlineCode.Matches(text))
                    {
                        if (ReferenceOf(match.Value) is { } reference)
                        {
                            named.Add(reference);
                        }
                    }
                }
            }

            foreach (var reference in named.Order(StringComparer.Ordinal))
            {
                if (Unknown(model, reference) is { } message)
                {
                    findings.Add(new Finding("reference-unknown", $"Record '{record.Id}' names '{reference}' in prose, {message}.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }

        // Explanation files are for people and no check reads them; only standalone documents are checked.
        foreach (var body in model.Bodies.Where(body => body.PageId is null))
        {
            CheckText(model, body.File, body.Body, body.BodyStartLine, body.PageId, findings);
        }

        var agents = Path.Combine(model.Config.Root, AgentProtocol.FileName);

        if (!File.Exists(agents))
        {
            return;
        }

        try
        {
            CheckText(model, agents, File.ReadAllText(agents), 1, null, findings);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }
    }

    /// <summary>Reports each backticked identifier in one text that resolves to no record or shipped provision.</summary>
    /// <param name="model">The loaded project model that resolves each named identifier.</param>
    /// <param name="file">The file that holds the text, shown on each finding.</param>
    /// <param name="text">The explanation, document or agent context text whose identifiers are checked.</param>
    /// <param name="startLine">The one-based line of the first line of the text in its file.</param>
    /// <param name="subject">The record the text belongs to, or null for documents and the agent context.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckText(
        ProjectModel model,
        string file,
        string text,
        int startLine,
        string? subject,
        List<Finding> findings)
    {
        var prose = WithoutFencesAndRegions(text);
        var starts = LineStarts(prose);
        var named = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (Match match in InlineCode.Matches(prose))
        {
            if (ReferenceOf(match.Value) is { } reference)
            {
                named.TryAdd(reference, startLine + LineOf(starts, match.Index) - 1);
            }
        }

        foreach (var entry in named.OrderBy(left => left.Value).ThenBy(left => left.Key, StringComparer.Ordinal))
        {
            if (Unknown(model, entry.Key) is { } message)
            {
                findings.Add(new Finding("reference-unknown", $"Prose names '{entry.Key}', {message}.", file, entry.Value, subject));
            }
        }
    }

    /// <summary>The named reference of one code span, or null when the span holds no record or provision identifier.</summary>
    /// <param name="span">The code span with its backticks.</param>
    /// <returns>The identifier the span names, or null.</returns>
    private static string? ReferenceOf(string span)
    {
        var inner = Identifiers.TrimCitation(span.Trim('`'));

        if (inner.Length == 0)
        {
            return null;
        }

        return RecordReference.IsMatch(inner) || ProvisionReference.IsMatch(inner) ? inner : null;
    }

    /// <summary>Why a named reference resolves to nothing, or null when it resolves to a record or a shipped provision.</summary>
    /// <param name="model">The loaded project model that holds every record and the carried provisions.</param>
    /// <param name="reference">The named identifier under test.</param>
    /// <returns>The reason the reference is unknown, or null.</returns>
    private static string? Unknown(ProjectModel model, string reference)
    {
        if (reference.StartsWith("litespec/", StringComparison.Ordinal))
        {
            return model.Provisions.Count == 0 || model.Provisions.Contains(reference)
                ? null
                : "which the carried release does not ship";
        }

        if (Identifiers.TrySplitStep(reference, out _, out _))
        {
            return "which names a step in the old flow/name#step form; cite the step id instead";
        }

        return model.ById.ContainsKey(Identifiers.BaseId(reference)) ? null : "which no record holds";
    }

    /// <summary>Yields each string held by a JSON value, descending into arrays and objects.</summary>
    /// <param name="value">The JSON value whose strings are collected.</param>
    /// <returns>Each string in document order; empty when the value holds no string.</returns>
    private static IEnumerable<string> StringsOf(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String && value.GetString() is { } text)
        {
            yield return text;
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in value.EnumerateArray())
            {
                foreach (var nested in StringsOf(entry))
                {
                    yield return nested;
                }
            }
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                foreach (var nested in StringsOf(property.Value))
                {
                    yield return nested;
                }
            }
        }
    }

    /// <summary>Blanks fenced code blocks and generated regions, keeping offsets and line breaks, so examples and tool output are not read.</summary>
    /// <param name="text">The Markdown text whose fences and regions are blanked.</param>
    /// <returns>The text with fenced blocks and generated regions replaced by spaces, keeping every offset and line break.</returns>
    internal static string WithoutFencesAndRegions(string text)
    {
        static string Blank(Match match) => Regex.Replace(match.Value, @"[^\r\n]", " ");

        return GeneratedRegion.Replace(FencedCode.Replace(text, Blank), Blank);
    }

    /// <summary>Matches one backticked record identifier with an optional step fragment, built from every record type.</summary>
    private static readonly Regex RecordReference = new(
        $"^(?:{string.Join("|", RecordTypes.All.OrderBy(kind => kind, StringComparer.Ordinal).Select(Regex.Escape))})/[A-Za-z0-9][A-Za-z0-9._-]*(?:#[A-Za-z0-9][A-Za-z0-9._-]*)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Matches one backticked shared provision identifier in its published form.</summary>
    private static readonly Regex ProvisionReference = new(
        @"^litespec/[a-z0-9]+(?:-[a-z0-9]+)*(?:/[a-z0-9]+(?:-[a-z0-9]+)*)*#[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Matches one inline code span delimited by single backticks.</summary>
    private static readonly Regex InlineCode = new(@"`[^`\r\n]*`", RegexOptions.Compiled);

    /// <summary>Matches one fenced code block from its opening fence to its closing fence.</summary>
    private static readonly Regex FencedCode = new(@"(?ms)^[ \t]*(```|~~~).*?^[ \t]*\1[^\r\n]*$", RegexOptions.Compiled);

    /// <summary>Matches one generated region from its opening fence to the closing fence of the same name.</summary>
    private static readonly Regex GeneratedRegion = new(
        @"<!-- generated:(?<name>[A-Za-z0-9-]+) -->(?<body>.*?)<!-- /generated:\k<name> -->",
        RegexOptions.Compiled | RegexOptions.Singleline);
}
