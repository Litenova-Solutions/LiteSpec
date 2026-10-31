using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Schemas;
using static LiteSpec.Authoring.ManifestValidator;
using static LiteSpec.Authoring.ProvisionValidator;
using static LiteSpec.Authoring.ValidatorSupport;

namespace LiteSpec.Authoring;

/// <summary>
///     The record pass: frontmatter, identifiers, sources, references, and decision citations for LiteSpec's own research and decisions.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal sealed partial class RecordsValidator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RecordsValidator"/> class.
    /// </summary>
    /// <param name="context">The shared validation context.</param>
    public RecordsValidator(ValidationContext context)
    {
        Context = context;
    }

    /// <summary>Gets the shared validation context.</summary>
    private ValidationContext Context { get; }

    // LiteSpec keeps its own reasoning as consumer-shaped records under docs/intent/.
    // The record pass reuses the shipped record schema instead of inventing a second
    // shape, so a consumer page and a LiteSpec record page fail the same way.
    // (litespec/core/maintaining#record-the-reasoning-behind-each-release)
    /// <summary>Checks the records among the parsed pages.</summary>
    /// <param name="parsedPages">The parsed pages.</param>
    internal void CheckRecords(List<ParsedPage> parsedPages)
    {
        CheckRecordSchema(parsedPages);
        CheckRecordIds(parsedPages);
        CheckRecordSources(parsedPages);
        CheckRecordReferences(parsedPages);
        CheckRecordDecisionCitations(parsedPages);
    }

    /// <summary>Parses the JSON frontmatter block of a record page.</summary>
    /// <param name="raw">The raw page text.</param>
    /// <param name="node">The parsed frontmatter, or null when it does not parse.</param>
    /// <param name="error">The parse failure message.</param>
    /// <returns>True when the frontmatter parses.</returns>
    internal static bool TryParseRecordFrontmatter(string raw, out JsonNode? node, out string error)
    {
        node = null;
        error = "";

        var lines = raw.Split('\n');
        if (lines.Length < 3 || lines[0].TrimEnd('\r') != "---")
        {
            error = "no frontmatter block; every record page opens with a '---' JSON block carrying its record";
            return false;
        }

        var closing = -1;
        for (var index = 1; index < lines.Length; index++)
        {
            if (lines[index].TrimEnd('\r') == "---")
            {
                closing = index;
                break;
            }
        }

        if (closing < 0)
        {
            error = "unterminated frontmatter block";
            return false;
        }

        var json = string.Join('\n', lines[1..closing]);
        try
        {
            node = JsonNode.Parse(json);
            return true;
        }
        catch (System.Text.Json.JsonException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    // Frontmatter is validated against the researchPage or decisionPage shape the
    // record schema ships, through the shared schema module. A missing schema file
    // skips the check: the page still faces the identifier, source, and reference
    // checks below.
    /// <summary>Checks record frontmatter against the shipped record schema shapes.</summary>
    /// <param name="parsedPages">The parsed pages.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "CheckRecordSchema reports a validation failure over arbitrary record frontmatter as collected errors instead of crashing the validation run on malformed repository input.")]
    private void CheckRecordSchema(List<ParsedPage> parsedPages)
    {
        var root = Context.Root;
        var add = Context.Add;
        var records = parsedPages.Where(page => page.Parsed.Kind is "research" or "decision").ToList();
        if (records.Count == 0)
        {
            return;
        }

        var schemaFile = JoinRoot(root, "schemas/record.schema.json");
        if (!File.Exists(schemaFile))
        {
            return;
        }

        JsonNode? schema;
        try
        {
            schema = JsonNode.Parse(TrimBom(ReadText(schemaFile)));
        }
        catch (IOException)
        {
            return;
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }
        catch (System.Text.Json.JsonException)
        {
            return;
        }

        if (schema is not JsonObject)
        {
            return;
        }

        var defs = (schema as JsonObject)?["$defs"] as JsonObject;
        if (defs is null)
        {
            return;
        }

        foreach (var page in records)
        {
            if (!TryParseRecordFrontmatter(page.Reader.Raw, out var frontmatter, out var error))
            {
                add(page.Relative, 1, "RECORD_SCHEMA", $"record frontmatter is invalid: {error}");
                continue;
            }

            var defName = page.Parsed.Kind == "research" ? "researchPage" : "decisionPage";
            var def = defs[defName];
            if (def is null)
            {
                continue;
            }

            var errors = new List<string>();
            try
            {
                JsonSchema.Validate(frontmatter, def, schema!, page.Relative, errors);
            }
            catch (Exception exception)
            {
                errors.Add($"{page.Relative}: {exception.Message}");
            }

            foreach (var message in errors)
            {
                var text = message.StartsWith(page.Relative + ": ", StringComparison.Ordinal)
                    ? message[(page.Relative.Length + 2)..]
                    : message;
                add(page.Relative, 1, "RECORD_SCHEMA", text);
            }
        }
    }

    // The identifier names the file, so a reader finds the record from its citation
    // without an index. A missing identifier belongs to the schema check above.
    /// <summary>Checks that each record identifier matches its file name.</summary>
    /// <param name="parsedPages">The parsed pages.</param>
    private void CheckRecordIds(List<ParsedPage> parsedPages)
    {
        var add = Context.Add;
        foreach (var page in parsedPages.Where(page => page.Parsed.Kind is "research" or "decision"))
        {
            if (!TryParseRecordFrontmatter(page.Reader.Raw, out var frontmatter, out _))
            {
                continue;
            }

            var id = JsonString((frontmatter as JsonObject)?["id"]);
            if (id is null || id.Length == 0)
            {
                continue;
            }

            var fileName = page.Relative.Split('/')[^1];
            var stem = fileName.EndsWith(".md", StringComparison.Ordinal) ? fileName[..^".md".Length] : fileName;
            var expected = $"{page.Parsed.Kind}/{stem}";
            if (id != expected)
            {
                add(page.Relative, 1, "RECORD_ID_MISMATCH", $"record id '{id}' does not match file name; expected '{expected}'");
            }
        }
    }

    // Every source carries the date it was read, and a path source names a file the
    // repository holds. A web source needs no existence check beyond its read date.
    /// <summary>Checks that each record source carries a read date and an existing path.</summary>
    /// <param name="parsedPages">The parsed pages.</param>
    private void CheckRecordSources(List<ParsedPage> parsedPages)
    {
        var root = Context.Root;
        var add = Context.Add;
        foreach (var page in parsedPages.Where(page => page.Parsed.Kind is "research" or "decision"))
        {
            if (!TryParseRecordFrontmatter(page.Reader.Raw, out var frontmatter, out _))
            {
                continue;
            }

            if ((frontmatter as JsonObject)?["sources"] is not JsonArray sources)
            {
                continue;
            }

            for (var index = 0; index < sources.Count; index++)
            {
                if (sources[index] is not JsonObject entry)
                {
                    continue;
                }

                var read = JsonString(entry["readOn"]);
                if (read is null || read.Length == 0)
                {
                    add(page.Relative, 1, "RECORD_SOURCE", $"source {index + 1} has no read date");
                }

                var path = JsonString(entry["path"]);
                if (path is not null && path.Length > 0 && !File.Exists(JoinRoot(root, path)))
                {
                    add(page.Relative, 1, "RECORD_SOURCE", $"source {index + 1} path '{path}' does not exist");
                }
            }
        }
    }

    // Every written link resolves to a LiteSpec record, and every backticked record
    // identifier under docs/ resolves to one of LiteSpec's records or to a record of
    // the Claimwick example the pages cite. An unknown target names a record that no
    // file holds.
    /// <summary>Checks that every record reference names a known record.</summary>
    /// <param name="parsedPages">The parsed pages.</param>
    private void CheckRecordReferences(List<ParsedPage> parsedPages)
    {
        var root = Context.Root;
        var add = Context.Add;
        var known = new HashSet<string>(StringComparer.Ordinal);
        var records = parsedPages.Where(page => page.Parsed.Kind is "research" or "decision").ToList();
        foreach (var page in records)
        {
            if (!TryParseRecordFrontmatter(page.Reader.Raw, out var frontmatter, out _))
            {
                continue;
            }

            var id = JsonString((frontmatter as JsonObject)?["id"]);
            if (id is not null)
            {
                known.Add(id);
            }
        }

        foreach (var page in records)
        {
            if (!TryParseRecordFrontmatter(page.Reader.Raw, out var frontmatter, out _))
            {
                continue;
            }

            if (frontmatter is not JsonObject obj)
            {
                continue;
            }

            foreach (var field in new[] { "basedOn", "decides" })
            {
                if (obj[field] is not JsonArray list)
                {
                    continue;
                }

                foreach (var item in list)
                {
                    var target = JsonString(item);
                    if (target is null || known.Contains(target))
                    {
                        continue;
                    }

                    add(page.Relative, 1, "RECORD_UNKNOWN_REFERENCE", $"'{field}' names unknown record '{target}'");
                }
            }

            var replaces = JsonString(obj["replaces"]);
            if (replaces is not null && !known.Contains(replaces))
            {
                add(page.Relative, 1, "RECORD_UNKNOWN_REFERENCE", $"'replaces' names unknown record '{replaces}'");
            }
        }

        var cited = new HashSet<string>(known, StringComparer.Ordinal);
        cited.UnionWith(ExampleRecordIds());

        foreach (var page in parsedPages)
        {
            var prose = string.Join('\n', page.Reader.Lines.Select((line, index) =>
                page.Reader.FencedLines.Contains(index + 1) || page.Reader.GeneratedLines.Contains(index + 1) ? "" : line));
            foreach (Match match in RecordReferencePattern().Matches(prose))
            {
                var id = match.Groups[1].Value.Trim();
                if (id.Length == 0 || cited.Contains(id))
                {
                    continue;
                }

                add(page.Relative, LineNumber(prose, match.Index), "RECORD_UNKNOWN_REFERENCE", $"active content references unknown record '{id}'");
            }
        }
    }

    // The research and decision identifiers the Claimwick example records hold.
    /// <summary>Lists the record identifiers held by the Claimwick example.</summary>
    /// <returns>The example record identifiers.</returns>
    private IEnumerable<string> ExampleRecordIds()
    {
        var root = Context.Root;
        var intent = Path.Combine(root, "examples", "claimwick", "docs");
        if (!Directory.Exists(intent))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(intent, "*.md", SearchOption.AllDirectories))
        {
            if (TryParseRecordFrontmatter(File.ReadAllText(file), out var frontmatter, out _)
                && JsonString((frontmatter as JsonObject)?["id"]) is { } id
                && (id.StartsWith("research/", StringComparison.Ordinal) || id.StartsWith("decision/", StringComparison.Ordinal)))
            {
                yield return id;
            }
        }
    }

    // An approved decision settles at least one provision, so its Rationale cites
    // it. Research may stand uncited as a basis pool, and a draft decision settles
    // nothing yet.
    /// <summary>Checks that each approved decision is cited by a provision Rationale.</summary>
    /// <param name="parsedPages">The parsed pages.</param>
    private void CheckRecordDecisionCitations(List<ParsedPage> parsedPages)
    {
        var add = Context.Add;
        var approved = new List<(string Relative, string Id)>();
        foreach (var page in parsedPages.Where(page => page.Parsed.Kind == "decision"))
        {
            if (!TryParseRecordFrontmatter(page.Reader.Raw, out var frontmatter, out _))
            {
                continue;
            }

            var obj = frontmatter as JsonObject;
            var id = JsonString(obj?["id"]);
            var status = JsonString(obj?["status"]);
            if (id is not null && status == "approved")
            {
                approved.Add((page.Relative, id));
            }
        }

        if (approved.Count == 0)
        {
            return;
        }

        var rationales = new List<string>();
        foreach (var page in parsedPages)
        {
            if (!Provisions.IsProvisionPage(page.Relative))
            {
                continue;
            }

            foreach (var provision in page.Parsed.Provisions)
            {
                var active = "";
                var builder = new List<string>();
                foreach (var item in provision.Body)
                {
                    if (page.Reader.LabelAtLine.TryGetValue(item.Number, out var label) && ProvisionLabels.Contains(label))
                    {
                        active = label;
                    }

                    if (active == "Rationale")
                    {
                        builder.Add(item.Line);
                    }
                }

                if (builder.Count > 0)
                {
                    rationales.Add(string.Join('\n', builder));
                }
            }
        }

        foreach (var (relative, id) in approved)
        {
            if (!rationales.Any(text => text.Contains(id, StringComparison.Ordinal)))
            {
                add(relative, 1, "RECORD_DECISION_UNCITED", $"approved decision '{id}' is cited by no provision Rationale");
            }
        }
    }

    /// <summary>Gets the pattern that matches a backticked record reference.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"`((?:research/[a-z0-9][a-z0-9-]*|decision/[0-9]+-[a-z0-9][a-z0-9-]*))`")]
    private static partial Regex RecordReferencePattern();
}
