using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The citation checks: every cited identifier resolves to a record or a shared provision, in record fields, page prose, and code and test comments.</summary>
/// <follows>litespec/backend/identifiers#resolve-every-identifier-citation</follows>
internal static class CitationChecks
{
    /// <summary>Reports a citation resolving to no record.</summary>
    /// <param name="model">The loaded project model that holds every record, page body and scanned comment.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records)
        {
            if (record.Raw.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var cited = new HashSet<string>(StringComparer.Ordinal);

            foreach (var property in record.Raw.EnumerateObject())
            {
                if (property.Name is RecordFields.Id or RecordFields.Type)
                {
                    continue;
                }

                foreach (var text in StringsOf(property.Value))
                {
                    foreach (Match match in Identifiers.Citation.Matches(text))
                    {
                        cited.Add(Identifiers.TrimCitation(match.Value));
                    }

                    foreach (Match match in Identifiers.SharedCitation().Matches(text))
                    {
                        cited.Add(match.Value);
                    }
                }
            }

            foreach (var id in cited.Order(StringComparer.Ordinal))
            {
                if (!Resolves(model, id))
                {
                    findings.Add(new Finding("reference-unknown", $"Record '{record.Id}' cites '{id}', which no record holds.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }

        var texts = new List<(string File, string Text, int StartLine, string? Subject)>(model.Bodies.Count);

        // A batch review report records what its reader saw when the review ran, as an approval
        // manifest does, so a record renamed or removed since then is history, not a broken link.
        var docs = model.Config.RecordRoots.Count > 0 ? model.Config.RecordRoots[0] : Path.Combine(model.Config.Root, "docs");
        var reviews = Path.GetFullPath(Path.Combine(docs, "proof", "reviews")) + Path.DirectorySeparatorChar;

        foreach (var body in model.Bodies)
        {
            if (Path.GetFullPath(body.File).StartsWith(reviews, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            texts.Add((body.File, body.Body, body.BodyStartLine, body.PageId));
        }

        foreach (var comment in model.Code.Comments)
        {
            texts.Add((comment.File, comment.Text, comment.Line, null));
        }

        foreach (var comment in model.ScreenComments)
        {
            texts.Add((comment.File, comment.Text, comment.Line, null));
        }

        foreach (var comment in model.TestComments)
        {
            texts.Add((comment.File, comment.Text, comment.Line, null));
        }

        var found = new List<Finding>();

        System.Threading.Tasks.Parallel.ForEach(
            texts,
            () => new List<Finding>(),
            (text, _, local) =>
            {
                CheckTextCitations(model, text.File, text.Text, text.StartLine, text.Subject, local);

                return local;
            },
            local =>
            {
                lock (found)
                {
                    found.AddRange(local);
                }
            });

        findings.AddRange(found);
    }

    /// <summary>Reports each citation in one text that resolves to no record or shared provision.</summary>
    /// <param name="model">The loaded project model that resolves each cited identifier.</param>
    /// <param name="file">The file that holds the text, shown on each finding.</param>
    /// <param name="text">The page body or comment text whose citations are checked.</param>
    /// <param name="startLine">The one-based line of the first line of the text in its file.</param>
    /// <param name="subject">The record the text belongs to, or null for code and test comments.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckTextCitations(
        ProjectModel model,
        string file,
        string text,
        int startLine,
        string? subject,
        List<Finding> findings)
    {
        var cited = new Dictionary<string, int>(StringComparer.Ordinal);
        var starts = LineStarts(text);

        void Collect(MatchCollection matches, Func<string, string> clean)
        {
            foreach (Match match in matches)
            {
                cited.TryAdd(clean(match.Value), startLine + LineOf(starts, match.Index) - 1);
            }
        }

        // A link target is a file path, so a path that starts with a record type's name, such as
        // product/brief.md, is not a citation. Blanking the targets keeps every offset in place.
        var prose = LinkTarget.Replace(text, match => "](" + new string(' ', match.Length - 3) + ")");

        Collect(Identifiers.Citation.Matches(prose), Identifiers.TrimCitation);
        Collect(Identifiers.SharedCitation().Matches(prose), value => value);

        foreach (var entry in cited.OrderBy(left => left.Value).ThenBy(left => left.Key, StringComparer.Ordinal))
        {
            if (!Resolves(model, entry.Key))
            {
                findings.Add(new Finding("reference-unknown", $"Citation '{entry.Key}' resolves to no record.", file, entry.Value, subject));
            }
        }
    }

    /// <summary>Decides whether a cited identifier names a record or an admitted shared provision.</summary>
    /// <param name="model">The loaded project model that holds every record and the admitted provision identifiers.</param>
    /// <param name="id">The cited identifier under test.</param>
    /// <returns>True when the identifier resolves to a record or an admitted provision; otherwise false.</returns>
    private static bool Resolves(ProjectModel model, string id)
    {
        if (model.ById.ContainsKey(id))
        {
            return true;
        }

        if (id.StartsWith("litespec/", StringComparison.Ordinal))
        {
            return model.Provisions.Count == 0 || model.Provisions.Contains(id);
        }

        return false;
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

    /// <summary>Matches one Markdown link target, so file paths are blanked before citations are read.</summary>
    private static readonly Regex LinkTarget = new(@"\]\([^)\s]*\)", RegexOptions.Compiled);
}
