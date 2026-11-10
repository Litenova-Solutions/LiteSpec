using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Model;
using LiteSpec.Schemas;
using static LiteSpec.Authoring.ManifestValidator;
using static LiteSpec.Authoring.ProvisionValidator;
using static LiteSpec.Authoring.ValidatorSupport;

namespace LiteSpec.Authoring;

/// <summary>
///     The record pass: record files, identifiers, sources, references, and decision citations for LiteSpec's own research and decisions.
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
    // Each record is a .yaml record file with its explanation beside it. The record
    // pass reads the file through the strict YAML reader and joins the explanation
    // page the main walk parsed, so a consumer record file and a LiteSpec record
    // file fail the same way.
    // (litespec/core/maintaining#record-the-reasoning-behind-each-release)
    /// <summary>Checks the records among the parsed pages.</summary>
    /// <param name="parsedPages">The parsed pages.</param>
    internal void CheckRecords(List<ParsedPage> parsedPages)
    {
        var records = CollectRecords(parsedPages);
        CheckRecordText(records);
        CheckRecordSchema(records);
        CheckRecordExplanations(records, parsedPages);
        CheckRecordIds(records);
        CheckRecordSources(records);
        CheckRecordReferences(records, parsedPages);
        CheckRecordDecisionCitations(records, parsedPages);
    }

    /// <summary>Parses a record file's strict YAML text.</summary>
    /// <param name="text">The record file text.</param>
    /// <param name="node">The parsed record, or null when it does not parse.</param>
    /// <param name="error">The parse failure message.</param>
    /// <returns>True when the record parses.</returns>
    internal static bool TryParseRecordFile(string text, out JsonNode? node, out string error)
    {
        node = null;
        try
        {
            node = DataFile.NodeOf(YamlText.Parse(text));
            error = "";
            return true;
        }
        catch (JsonTextException exception)
        {
            error = exception.Message;
            return false;
        }
        catch (System.Text.Json.JsonException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    /// <summary>Collects every LiteSpec record file with its explanation page.</summary>
    /// <param name="parsedPages">The parsed pages.</param>
    /// <returns>The collected records.</returns>
    private List<ParsedRecord> CollectRecords(List<ParsedPage> parsedPages)
    {
        var explanations = new Dictionary<string, ParsedPage>(StringComparer.Ordinal);
        foreach (var page in parsedPages)
        {
            if (page.Parsed.Kind is "research" or "decision")
            {
                explanations[Path.ChangeExtension(page.Relative, ".yaml")] = page;
            }
        }

        // A record file is a .yaml file whose path names a research or decision
        // record. Any other .yaml file holds no LiteSpec record.
        var records = new List<ParsedRecord>();
        foreach (var file in Walk(Path.Combine(Context.Root, "docs"), candidate => candidate.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)))
        {
            var relative = PathText.Relative(Context.Root, file);
            var kind = PageValidator.PageClass(Path.ChangeExtension(relative, ".md"));
            if (kind is "research" or "decision")
            {
                var raw = FileText.ReadText(file);
                _ = TryParseRecordFile(raw, out var node, out var error);
                records.Add(new ParsedRecord(relative, kind, raw, node, error, explanations.GetValueOrDefault(relative)));
            }
        }

        return records;
    }

    /// <summary>Collects the record identifiers of LiteSpec's own record files.</summary>
    /// <param name="root">The repository root.</param>
    /// <returns>The own record identifiers.</returns>
    internal static HashSet<string> OwnRecordIds(string root)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in Walk(Path.Combine(root, "docs"), candidate => candidate.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)))
        {
            var relative = PathText.Relative(root, file);
            if (PageValidator.PageClass(Path.ChangeExtension(relative, ".md")) is not ("research" or "decision"))
            {
                continue;
            }

            if (TryParseRecordFile(FileText.ReadText(file), out var node, out _)
                && JsonString((node as JsonObject)?["id"]) is { } id)
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    // The record file holds authored prose inside its fields, so the ASCII rule
    // reads it as it reads every page.
    /// <summary>Checks that every record file holds ASCII text only.</summary>
    /// <param name="records">The collected records.</param>
    private void CheckRecordText(List<ParsedRecord> records)
    {
        var add = Context.Add;
        foreach (var record in records)
        {
            var offset = NonAsciiOffset(record.Raw);
            if (offset >= 0)
            {
                add(record.Relative, LineNumber(record.Raw, offset), Diagnostics.Codes.ProseNonAscii, NonAsciiMessage(record.Raw[offset], "non-ASCII character "));
            }
        }
    }

    // A record file is validated against the researchPage or decisionPage shape the
    // record schema ships, through the shared schema module. A missing schema file
    // skips the check: the record still faces the identifier, source, and reference
    // checks below.
    /// <summary>Checks record files against the shipped record schema shapes.</summary>
    /// <param name="records">The collected records.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "CheckRecordSchema reports a validation failure over arbitrary record files as collected errors instead of crashing the validation run on malformed repository input.")]
    private void CheckRecordSchema(List<ParsedRecord> records)
    {
        var root = Context.Root;
        var add = Context.Add;
        if (records.Count == 0)
        {
            return;
        }

        var schemaFile = JoinRoot(root, "schemas/record.schema.yaml");
        if (!File.Exists(schemaFile))
        {
            return;
        }

        JsonNode? schema;
        try
        {
            schema = DataFile.Read(schemaFile);
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

        foreach (var record in records)
        {
            if (record.Node is null)
            {
                add(record.Relative, 1, Diagnostics.Codes.RecordSchema, $"record file is invalid: {record.Error}");
                continue;
            }

            var defName = record.Kind == "research" ? "researchPage" : "decisionPage";
            var def = defs[defName];
            if (def is null)
            {
                continue;
            }

            var errors = new List<string>();
            try
            {
                JsonSchema.Validate(record.Node, def, schema!, record.Relative, errors);
            }
            catch (Exception exception)
            {
                errors.Add($"{record.Relative}: {exception.Message}");
            }

            foreach (var message in errors)
            {
                var text = message.StartsWith(record.Relative + ": ", StringComparison.Ordinal)
                    ? message[(record.Relative.Length + 2)..]
                    : message;
                add(record.Relative, 1, Diagnostics.Codes.RecordSchema, text);
            }
        }
    }

    // Research and decision records are articles: a record file without its
    // explanation is half a page, and an explanation without its record explains
    // nothing.
    /// <summary>Checks that every record file sits beside its explanation file.</summary>
    /// <param name="records">The collected records.</param>
    /// <param name="parsedPages">The parsed pages.</param>
    private void CheckRecordExplanations(List<ParsedRecord> records, List<ParsedPage> parsedPages)
    {
        var add = Context.Add;
        foreach (var record in records)
        {
            if (record.Explanation is null)
            {
                add(record.Relative, 1, Diagnostics.Codes.RecordExplanationMissing, $"record has no explanation file '{Path.ChangeExtension(record.Relative, ".md")}' beside it; research and decision records require one");
            }
        }

        var recordFiles = new HashSet<string>(records.Select(record => record.Relative), StringComparer.Ordinal);
        foreach (var page in parsedPages)
        {
            if (page.Parsed.Kind is "research" or "decision"
                && !recordFiles.Contains(Path.ChangeExtension(page.Relative, ".yaml")))
            {
                add(page.Relative, 1, Diagnostics.Codes.RecordExplanationOrphaned, $"explanation file has no record file '{Path.ChangeExtension(page.Relative, ".yaml")}' beside it");
            }
        }
    }

    // The identifier names the file, so a reader finds the record from its citation
    // without an index. A missing identifier belongs to the schema check above.
    /// <summary>Checks that each record identifier matches its file name.</summary>
    /// <param name="records">The collected records.</param>
    private void CheckRecordIds(List<ParsedRecord> records)
    {
        var add = Context.Add;
        foreach (var record in records)
        {
            var id = JsonString((record.Node as JsonObject)?["id"]);
            if (id is null || id.Length == 0)
            {
                continue;
            }

            var fileName = record.Relative.Split('/')[^1];
            var stem = fileName.EndsWith(".yaml", StringComparison.Ordinal) ? fileName[..^".yaml".Length] : fileName;
            var expected = $"{record.Kind}/{stem}";
            if (id != expected)
            {
                add(record.Relative, 1, Diagnostics.Codes.RecordIdMismatch, $"record id '{id}' does not match file name; expected '{expected}'");
            }
        }
    }

    // Every source carries the date it was read, and a path source names a file the
    // repository holds. A web source needs no existence check beyond its read date.
    /// <summary>Checks that each record source carries a read date and an existing path.</summary>
    /// <param name="records">The collected records.</param>
    private void CheckRecordSources(List<ParsedRecord> records)
    {
        var root = Context.Root;
        var add = Context.Add;
        foreach (var record in records)
        {
            if ((record.Node as JsonObject)?["sources"] is not JsonArray sources)
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
                    add(record.Relative, 1, Diagnostics.Codes.RecordSource, $"source {index + 1} has no read date");
                }

                var path = JsonString(entry["path"]);
                if (path is not null && path.Length > 0 && !File.Exists(JoinRoot(root, path)))
                {
                    add(record.Relative, 1, Diagnostics.Codes.RecordSource, $"source {index + 1} path '{path}' does not exist");
                }
            }
        }
    }

    // Every written link resolves to a LiteSpec record, and every backticked record
    // identifier in prose or in a record field resolves to one of LiteSpec's records
    // or to a record of the Claimwick example the pages cite. An unknown target
    // names a record that no file holds.
    /// <summary>Checks that every record reference names a known record.</summary>
    /// <param name="records">The collected records.</param>
    /// <param name="parsedPages">The parsed pages.</param>
    private void CheckRecordReferences(List<ParsedRecord> records, List<ParsedPage> parsedPages)
    {
        var root = Context.Root;
        var add = Context.Add;
        var known = new HashSet<string>(StringComparer.Ordinal);
        foreach (var record in records)
        {
            if (JsonString((record.Node as JsonObject)?["id"]) is { } id)
            {
                known.Add(id);
            }
        }

        foreach (var record in records)
        {
            if (record.Node is not JsonObject obj)
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

                    add(record.Relative, 1, Diagnostics.Codes.RecordUnknownReference, $"'{field}' names unknown record '{target}'");
                }
            }
        }

        var cited = new HashSet<string>(known, StringComparer.Ordinal);
        cited.UnionWith(ExampleRecordIds(root));

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

                add(page.Relative, LineNumber(prose, match.Index), Diagnostics.Codes.RecordUnknownReference, $"active content references unknown record '{id}'");
            }
        }

        foreach (var record in records)
        {
            foreach (Match match in RecordReferencePattern().Matches(record.Raw))
            {
                var id = match.Groups[1].Value.Trim();
                if (id.Length == 0 || cited.Contains(id))
                {
                    continue;
                }

                add(record.Relative, LineNumber(record.Raw, match.Index), Diagnostics.Codes.RecordUnknownReference, $"active content references unknown record '{id}'");
            }
        }
    }

    // The research and decision identifiers the Claimwick example records hold.
    /// <summary>Lists the record identifiers held by the Claimwick example.</summary>
    /// <param name="root">The repository root.</param>
    /// <returns>The example record identifiers.</returns>
    internal static HashSet<string> ExampleRecordIds(string root)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in Walk(Path.Combine(root, "examples", "claimwick", ".litespec"), candidate => candidate.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)))
        {
            if (TryParseRecordFile(FileText.ReadText(file), out var node, out _)
                && JsonString((node as JsonObject)?["id"]) is { } id
                && (id.StartsWith("research/", StringComparison.Ordinal) || id.StartsWith("decision/", StringComparison.Ordinal)))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    // A decision settles at least one provision, so its Rationale cites it.
    // Research may stand uncited as a basis pool. A record present in git is
    // decided; there is no draft state.
    /// <summary>Checks that each decision is cited by a provision Rationale.</summary>
    /// <param name="records">The collected records.</param>
    /// <param name="parsedPages">The parsed pages.</param>
    private void CheckRecordDecisionCitations(List<ParsedRecord> records, List<ParsedPage> parsedPages)
    {
        var add = Context.Add;
        var decided = new List<(string Relative, string Id)>();
        foreach (var record in records.Where(record => record.Kind == "decision"))
        {
            var obj = record.Node as JsonObject;
            var id = JsonString(obj?["id"]);
            if (id is not null)
            {
                decided.Add((record.Relative, id));
            }
        }

        if (decided.Count == 0)
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

        foreach (var (relative, id) in decided)
        {
            if (!rationales.Any(text => text.Contains(id, StringComparison.Ordinal)))
            {
                add(relative, 1, Diagnostics.Codes.RecordDecisionUncited, $"decision '{id}' is cited by no provision Rationale");
            }
        }
    }

    /// <summary>Gets the pattern that matches a backticked record reference.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"`((?:research/[a-z0-9][a-z0-9-]*|decision/[0-9]+-[a-z0-9][a-z0-9-]*))`")]
    private static partial Regex RecordReferencePattern();
}
