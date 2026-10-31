using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;

namespace LiteSpec.Model;

/// <summary>
///     Finds every written record: page records, entries and terms.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class RecordReader
{
    /// <summary>Reads every record under the configured roots.</summary>
    /// <param name="config">The consumer configuration holding the record roots and glossary.</param>
    /// <returns>The records, bodies, provisions and problems the read found, each sorted by file and line.</returns>
    public static RecordRead Read(ConsumerConfig config)
    {
        var files = new List<string>();

        foreach (var root in config.RecordRoots)
        {
            files.AddRange(SourceTree.Files(root, ".md"));
        }

        var records = new List<Record>();
        var bodies = new List<PageBody>();
        var problems = new List<Finding>();
        var gate = new object();

        System.Threading.Tasks.Parallel.ForEach(
            files,
            () => (Records: new List<Record>(), Bodies: new List<PageBody>(), Problems: new List<Finding>()),
            (file, _, local) =>
            {
                ReadPage(config, file, local.Records, local.Bodies, local.Problems);

                return local;
            },
            local =>
            {
                lock (gate)
                {
                    records.AddRange(local.Records);
                    bodies.AddRange(local.Bodies);
                    problems.AddRange(local.Problems);
                }
            });

        records.Sort((left, right) =>
        {
            var file = string.Compare(left.File, right.File, StringComparison.Ordinal);

            return file != 0 ? file : left.Line.CompareTo(right.Line);
        });
        bodies.Sort((left, right) => string.Compare(left.File, right.File, StringComparison.Ordinal));
        problems.Sort((left, right) =>
        {
            var file = string.Compare(left.Path, right.Path, StringComparison.Ordinal);

            return file != 0 ? file : left.Line.CompareTo(right.Line);
        });

        ReadTerms(config, records, problems);
        DeriveResults(records, config.ForbiddenCode, config.InvalidInputCode);

        return new RecordRead(records, bodies, ReadProvisions(), problems);
    }

    /// <summary>Whether the page opens with a frontmatter fence line, so a parse failure drops a record.</summary>
    /// <param name="text">The page text to test.</param>
    /// <returns>True when the first line of the text is the frontmatter fence.</returns>
    private static bool HasFrontmatterFence(string text)
    {
        var firstLine = text.Split('\n')[0].TrimEnd('\r');

        return string.Equals(firstLine, "---", StringComparison.Ordinal);
    }

    /// <summary>Reads one page into records, a body and problems, all appended in place.</summary>
    /// <param name="config">The consumer configuration of the project.</param>
    /// <param name="file">The absolute path of the page to read.</param>
    /// <param name="records">The records read so far, appended in place.</param>
    /// <param name="bodies">The page bodies read so far, appended in place.</param>
    /// <param name="problems">The problems found so far, appended in place.</param>
    private static void ReadPage(
        ConsumerConfig config,
        string file,
        List<Record> records,
        List<PageBody> bodies,
        List<Finding> problems)
    {
        var text = SourceTree.Read(file);

        if (!Frontmatter.TryParse(text, out var parsed, out var parseError))
        {
            if (HasFrontmatterFence(text))
            {
                problems.Add(new Finding(
                    "record-invalid",
                    $"Record page '{file}' could not be read: {parseError}",
                    file,
                    1,
                    null));
            }

            bodies.Add(new PageBody(file, text, 1, null));
            return;
        }

        string? pageId = null;
        string? pageOwner = null;
        string? pageStatus = null;
        string? pageDecider = null;
        string? pageDecideBy = null;

        if (parsed.Root.ValueKind == JsonValueKind.Object
            && parsed.Root.TryGetProperty(RecordFields.Type, out var typeValue)
            && typeValue.ValueKind == JsonValueKind.String
            && typeValue.GetString() is { } type)
        {
            var tables = TablesOf(parsed.Body);

            if (RecordTypes.All.Contains(type))
            {
                var record = BuildRecord(
                    PatchedPage(parsed.Root, tables), file, LineOf(text, $"\"{RecordFields.Type}\""), false, null,
                    null, null, null, null);

                pageId = record.Id;
                pageOwner = record.Owner;
                pageStatus = record.Status;
                pageDecider = record.Decider;
                pageDecideBy = record.DecideBy;
                records.Add(record);
            }
            else if (!RecordTypes.PageStyles.Contains(type))
            {
                problems.Add(new Finding(
                    "field-invalid",
                    $"Page type '{type}' is neither a record type nor a page style.",
                    file,
                    LineOf(text, $"\"{RecordFields.Type}\""),
                    null));
            }
            else
            {
                pageOwner = StringField(parsed.Root, RecordFields.Owner);
                pageStatus = StringField(parsed.Root, RecordFields.Status);
                pageDecider = StringField(parsed.Root, RecordFields.Decider);
                pageDecideBy = StringField(parsed.Root, RecordFields.DecideBy);
            }

            if (parsed.Root.TryGetProperty(RecordFields.Records, out var entries)
                && entries.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in entries.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var entryType = StringField(entry, RecordFields.Type);
                    var line = StringField(entry, RecordFields.Id) is { } entryId
                        ? LineOf(text, entryId)
                        : LineOf(text, $"\"{RecordFields.Records}\"");

                    if (!RecordTypes.All.Contains(entryType ?? ""))
                    {
                        problems.Add(new Finding(
                            "field-invalid",
                            $"Entry type '{entryType ?? "missing"}' is not a record type.",
                            file,
                            line,
                            pageId));
                        continue;
                    }

                    records.Add(BuildRecord(
                        PatchedEntry(entry, entries, tables), file, line, true, pageId,
                        pageOwner, pageStatus, pageDecider, pageDecideBy));
                }
            }
        }

        bodies.Add(new PageBody(file, parsed.Body, parsed.BodyStartLine, pageId));
    }

    /// <summary>One handwritten Markdown table and the heading above it.</summary>
    internal sealed record MarkdownTable(
        IReadOnlyList<string> Headers,
        IReadOnlyList<IReadOnlyList<string>> Rows,
        string Heading);

    /// <summary>The pattern matching a Markdown table delimiter row.</summary>
    static readonly Regex TableDelimiter = new(
        @"^\s*\|?\s*:?-{1,}:?\s*(\|\s*:?-{1,}:?\s*)*\|?\s*$",
        RegexOptions.CultureInvariant);

    /// <summary>
    ///     The page element with table fields added: transitions and data on an
    ///     aggregate, examples on a scenario. A front-matter field wins over a table,
    ///     and a table that disagrees with one reports table-drift instead of
    ///     merging. Tables inside generated regions are tool output and never lift.
    /// </summary>
    /// <param name="element">The page element to patch.</param>
    /// <param name="tables">The handwritten tables on the page.</param>
    /// <returns>The element with any table fields added, or the element itself when none apply.</returns>
    private static JsonElement PatchedPage(JsonElement element, IReadOnlyList<MarkdownTable> tables)
    {
        if (StringField(element, RecordFields.Type) == RecordTypes.Aggregate)
        {
            var transitions = TransitionsOf(tables);

            if (transitions.Count > 0 && FieldMissing(element, RecordFields.Transitions))
            {
                element = WithField(element, RecordFields.Transitions, writer => WriteTransitions(writer, transitions));
            }

            var data = DataOf(tables);

            if (data.Count > 0 && FieldMissing(element, RecordFields.Data))
            {
                element = WithField(element, RecordFields.Data, writer => WriteData(writer, data));
            }
        }
        else if (StringField(element, RecordFields.Type) == RecordTypes.Scenario)
        {
            var examples = FirstExamples(tables);

            if (examples.Count > 0 && FieldMissing(element, RecordFields.Examples))
            {
                element = WithField(element, RecordFields.Examples, writer => WriteExamples(writer, examples));
            }
        }

        return element;
    }

    /// <summary>
    ///     A scenario entry with its examples table added. On a page with one
    ///     scenario the first plain table wins; with several, a table belongs to the
    ///     scenario its heading names. A front-matter field wins over a table.
    /// </summary>
    /// <param name="entry">The entry element to patch.</param>
    /// <param name="siblings">The entries array the entry sits in.</param>
    /// <param name="tables">The handwritten tables on the page.</param>
    /// <returns>The entry with its examples added, or the entry itself when no table belongs to it.</returns>
    private static JsonElement PatchedEntry(
        JsonElement entry,
        JsonElement siblings,
        IReadOnlyList<MarkdownTable> tables)
    {
        if (StringField(entry, RecordFields.Type) != RecordTypes.Scenario || !FieldMissing(entry, RecordFields.Examples))
        {
            return entry;
        }

        var examples = ExamplesFor(entry, siblings, tables);

        return examples.Count > 0
            ? WithField(entry, RecordFields.Examples, writer => WriteExamples(writer, examples))
            : entry;
    }

    /// <summary>Tests whether an element lacks a field or holds it as JSON null.</summary>
    /// <param name="element">The element to test.</param>
    /// <param name="field">The field name to test for.</param>
    /// <returns>True when the field is absent or null.</returns>
    private static bool FieldMissing(JsonElement element, string field) =>
        !element.TryGetProperty(field, out var value) || value.ValueKind == JsonValueKind.Null;

    /// <summary>The examples rows belonging to one scenario entry.</summary>
    /// <param name="entry">The scenario entry to find examples for.</param>
    /// <param name="siblings">The entries array the entry sits in.</param>
    /// <param name="tables">The handwritten tables on the page.</param>
    /// <returns>The example rows of the table belonging to the entry.</returns>
    private static List<IReadOnlyDictionary<string, string>> ExamplesFor(
        JsonElement entry,
        JsonElement siblings,
        IReadOnlyList<MarkdownTable> tables)
    {
        var scenarios = 0;

        foreach (var sibling in siblings.EnumerateArray())
        {
            if (sibling.ValueKind == JsonValueKind.Object && StringField(sibling, RecordFields.Type) == RecordTypes.Scenario)
            {
                scenarios++;
            }
        }

        if (scenarios <= 1)
        {
            return FirstExamples(tables);
        }

        var id = StringField(entry, RecordFields.Id) ?? "";
        var title = StringField(entry, RecordFields.Title) ?? "";
        var rows = new List<IReadOnlyDictionary<string, string>>();

        foreach (var table in tables)
        {
            if (!IsTransitions(table.Headers)
                && !IsData(table.Headers)
                && HeadingNames(table.Heading, id, title))
            {
                rows.AddRange(ExamplesOf(table));
            }
        }

        return rows;
    }

    /// <summary>The examples rows of the first plain table, skipping transitions and data tables.</summary>
    /// <param name="tables">The handwritten tables on the page.</param>
    /// <returns>The rows of the first plain table, or an empty list when the page holds none.</returns>
    private static List<IReadOnlyDictionary<string, string>> FirstExamples(
        IReadOnlyList<MarkdownTable> tables)
    {
        foreach (var table in tables)
        {
            if (!IsTransitions(table.Headers) && !IsData(table.Headers))
            {
                return ExamplesOf(table);
            }
        }

        return [];
    }

    /// <summary>Every From and To row of every handwritten transitions table, in order; By aliases Triggered By.</summary>
    /// <param name="tables">The handwritten tables to read transitions from.</param>
    /// <returns>The From, To and optional By of every transitions row, in table order.</returns>
    internal static IReadOnlyList<(string From, string To, string? By)> TransitionsOf(
        IReadOnlyList<MarkdownTable> tables)
    {
        var rows = new List<(string From, string To, string? By)>();

        foreach (var table in tables)
        {
            var from = ColumnOf(table.Headers, "from");
            var to = ColumnOf(table.Headers, "to");

            if (from < 0 || to < 0)
            {
                continue;
            }

            var by = ColumnOf(table.Headers, "triggeredby");

            if (by < 0)
            {
                by = ColumnOf(table.Headers, "by");
            }

            foreach (var cells in table.Rows)
            {
                var start = CleanCell(CellOf(cells, from), false);
                var end = CleanCell(CellOf(cells, to), false);

                if (IsEmptyCell(end))
                {
                    continue;
                }

                // An empty From is a creation: the row names the state an instance starts in.
                if (IsEmptyCell(start))
                {
                    start = Creation;
                }

                string? trigger = null;

                if (by >= 0)
                {
                    var cause = CleanCell(CellOf(cells, by), true);
                    trigger = IsEmptyCell(cause) ? null : cause;
                }

                rows.Add((start, end, trigger));
            }
        }

        return rows;
    }

    /// <summary>Every named row of every handwritten data table, in order.</summary>
    /// <param name="tables">The handwritten tables to read data rows from.</param>
    /// <returns>The Name, Meaning and Classification of every data row, in table order.</returns>
    internal static IReadOnlyList<(string Name, string Meaning, string Classification)> DataOf(
        IReadOnlyList<MarkdownTable> tables)
    {
        var rows = new List<(string Name, string Meaning, string Classification)>();

        foreach (var table in tables)
        {
            var name = ColumnOf(table.Headers, "name");
            var meaning = ColumnOf(table.Headers, "meaning");
            var classification = ColumnOf(table.Headers, "classification");

            if (name < 0 || meaning < 0 || classification < 0)
            {
                continue;
            }

            foreach (var cells in table.Rows)
            {
                var item = CleanCell(CellOf(cells, name), false);

                if (IsEmptyCell(item))
                {
                    continue;
                }

                var sense = CleanCell(CellOf(cells, meaning), false);
                var level = CleanCell(CellOf(cells, classification), false);

                rows.Add((item, IsEmptyCell(sense) ? "" : sense, IsEmptyCell(level) ? "" : level.ToLowerInvariant()));
            }
        }

        return rows;
    }

    /// <summary>
    ///     The table rows as examples: headers become the keys verbatim, a blank
    ///     header becomes column plus its 1-based position, and a repeated header
    ///     keeps its last value.
    /// </summary>
    /// <param name="table">The handwritten table to read as examples.</param>
    /// <returns>The example rows of the table, keyed by header.</returns>
    private static List<IReadOnlyDictionary<string, string>> ExamplesOf(MarkdownTable table)
    {
        var rows = new List<IReadOnlyDictionary<string, string>>();

        foreach (var cells in table.Rows)
        {
            var row = new Dictionary<string, string>(StringComparer.Ordinal);

            for (var index = 0; index < table.Headers.Count; index++)
            {
                var header = CleanCell(table.Headers[index], false);
                var key = header.Length == 0 ? $"column{index + 1}" : header;
                var value = CleanCell(CellOf(cells, index), false);
                row[key] = IsEmptyCell(value) ? "" : value;
            }

            if (row.Count > 0)
            {
                rows.Add(row);
            }
        }

        return rows;
    }

    /// <summary>Tests whether a table is a transitions table, by its From and To columns.</summary>
    /// <param name="headers">The table headers to test.</param>
    /// <returns>True when the headers hold both a From and a To column.</returns>
    private static bool IsTransitions(IReadOnlyList<string> headers) =>
        ColumnOf(headers, "from") >= 0 && ColumnOf(headers, "to") >= 0;

    /// <summary>Tests whether a table is a data table, by its Name, Meaning and Classification columns.</summary>
    /// <param name="headers">The table headers to test.</param>
    /// <returns>True when the headers hold Name, Meaning and Classification columns.</returns>
    private static bool IsData(IReadOnlyList<string> headers) =>
        ColumnOf(headers, "name") >= 0
        && ColumnOf(headers, "meaning") >= 0
        && ColumnOf(headers, "classification") >= 0;

    /// <summary>Finds one column by its normalized header name.</summary>
    /// <param name="headers">The table headers to search.</param>
    /// <param name="target">The normalized column name to find.</param>
    /// <returns>The 0-based column index, or -1 when no header matches.</returns>
    private static int ColumnOf(IReadOnlyList<string> headers, string target)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            if (Norm(headers[index]) == target)
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>Reads one cell, past the end of a short row.</summary>
    /// <param name="cells">The row cells.</param>
    /// <param name="index">The 0-based cell index.</param>
    /// <returns>The cell text, or empty when the row holds no cell at the index.</returns>
    private static string CellOf(IReadOnlyList<string> cells, int index) =>
        index < cells.Count ? cells[index] : "";

    /// <summary>Headers compare case-insensitively, ignoring spaces, dashes and underscores.</summary>
    /// <param name="value">The header text to normalize.</param>
    /// <returns>The header lowered, without spaces, dashes, underscores or tabs.</returns>
    private static string Norm(string value) =>
        value.Replace(" ", "").Replace("-", "").Replace("_", "").Replace("\t", "").ToLowerInvariant();

    /// <summary>Whether the heading names the scenario by id, title or kebab name.</summary>
    /// <param name="heading">The heading text above the table.</param>
    /// <param name="id">The scenario id the heading may name.</param>
    /// <param name="title">The scenario title the heading may name.</param>
    /// <returns>True when the heading names the scenario by id, title or kebab name.</returns>
    private static bool HeadingNames(string heading, string id, string title)
    {
        if (heading.Length == 0)
        {
            return false;
        }

        var text = heading.ToLowerInvariant();

        if (id.Length > 0 && text.Contains(id.ToLowerInvariant(), StringComparison.Ordinal))
        {
            return true;
        }

        if (title.Length > 0 && text.Contains(title.ToLowerInvariant(), StringComparison.Ordinal))
        {
            return true;
        }

        var name = id.Contains('.') ? id[(id.LastIndexOf('.') + 1)..] : id;
        name = name.Contains('/') ? name[(name.LastIndexOf('/') + 1)..] : name;
        var words = name.Replace('-', ' ').Replace('_', ' ').ToLowerInvariant();

        return words.Length > 0 && text.Contains(words, StringComparison.Ordinal);
    }

    /// <summary>Trimmed text: code ticks stripped, a lone link reduced to its label, or its target for an id.</summary>
    /// <param name="cell">The raw cell text.</param>
    /// <param name="preferTarget">Whether a lone link reduces to its target instead of its label.</param>
    /// <returns>The cleaned cell text.</returns>
    private static string CleanCell(string cell, bool preferTarget)
    {
        var text = cell.Trim();

        while (text.Length >= 2 && text.StartsWith('`') && text.EndsWith('`'))
        {
            text = text[1..^1].Trim();
        }

        if (LinkOf(text) is { } link)
        {
            if (preferTarget && link.Target.Contains('/'))
            {
                return link.Target.Trim();
            }

            return link.Label.Trim();
        }

        return text;
    }

    /// <summary>Splits a lone Markdown link into its label and its target.</summary>
    /// <param name="text">The cell text to split.</param>
    /// <returns>The label and target, or null when the text is no lone link ending the cell.</returns>
    private static (string Label, string Target)? LinkOf(string text)
    {
        if (!text.StartsWith('['))
        {
            return null;
        }

        var labelEnd = text.IndexOf("](", StringComparison.Ordinal);

        if (labelEnd < 0)
        {
            return null;
        }

        var targetEnd = text.IndexOf(')', labelEnd + 2);

        if (targetEnd != text.Length - 1)
        {
            return null;
        }

        var target = text[(labelEnd + 2)..targetEnd];
        var space = target.IndexOfAny([' ', '\t', '"', '\'']);

        return (text[1..labelEnd], space < 0 ? target : target[..space]);
    }

    /// <summary>The From of a transition that creates the instance.</summary>
    public const string Creation = "-";

    /// <summary>Tests whether a cell counts as empty: blank, a dash, or a none marker.</summary>
    /// <param name="text">The cleaned cell text.</param>
    /// <returns>True when the cell carries no value.</returns>
    private static bool IsEmptyCell(string text) =>
        text.Length == 0
        || text is "-" or "--" or "—"
        || text.Equals("n/a", StringComparison.OrdinalIgnoreCase)
        || text.Equals("none", StringComparison.OrdinalIgnoreCase)
        || text.Equals("none.", StringComparison.OrdinalIgnoreCase);

    /// <summary>Every handwritten Markdown table outside fenced code and generated regions, each with the heading above it.</summary>
    /// <param name="body">The page body to scan for tables.</param>
    /// <returns>The handwritten tables in body order, each with the heading above it.</returns>
    internal static IReadOnlyList<MarkdownTable> TablesOf(string body)
    {
        var lines = body.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var tables = new List<MarkdownTable>();
        var heading = "";
        var inFence = false;
        var inGenerated = false;
        var index = 0;

        while (index < lines.Length)
        {
            var line = lines[index];
            var trimmed = line.Trim();

            if (trimmed.StartsWith("```", StringComparison.Ordinal)
                || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                inFence = !inFence;
                index++;
                continue;
            }

            if (inFence)
            {
                index++;
                continue;
            }

            if (trimmed.StartsWith("<!-- generated:", StringComparison.Ordinal))
            {
                inGenerated = true;
                index++;
                continue;
            }

            if (trimmed.StartsWith("<!-- /generated:", StringComparison.Ordinal))
            {
                inGenerated = false;
                index++;
                continue;
            }

            if (inGenerated)
            {
                index++;
                continue;
            }

            if (HeadingText(trimmed) is { } text)
            {
                heading = text;
                index++;
                continue;
            }

            if (index + 1 < lines.Length
                && line.Contains('|')
                && trimmed.Length > 0
                && TableDelimiter.IsMatch(lines[index + 1]))
            {
                var headers = Cells(line);
                var rows = new List<IReadOnlyList<string>>();
                var row = index + 2;

                while (row < lines.Length
                       && lines[row].Contains('|')
                       && lines[row].Trim().Length > 0
                       && HeadingText(lines[row].Trim()) is null
                       && !lines[row].Trim().StartsWith("```", StringComparison.Ordinal)
                       && !lines[row].Trim().StartsWith("~~~", StringComparison.Ordinal))
                {
                    rows.Add(Cells(lines[row]));
                    row++;
                }

                if (headers.Count > 0)
                {
                    tables.Add(new MarkdownTable(headers, rows, heading));
                }

                index = row;
                continue;
            }

            index++;
        }

        return tables;
    }

    /// <summary>The text of a Markdown heading line.</summary>
    /// <param name="trimmed">The trimmed line to read.</param>
    /// <returns>The heading text without its hashes, or null when the line is no heading.</returns>
    private static string? HeadingText(string trimmed)
    {
        if (!trimmed.StartsWith('#'))
        {
            return null;
        }

        var hashes = 0;

        while (hashes < trimmed.Length && trimmed[hashes] == '#')
        {
            hashes++;
        }

        if (hashes > 6 || (hashes < trimmed.Length && trimmed[hashes] != ' ' && trimmed[hashes] != '\t'))
        {
            return null;
        }

        return trimmed[hashes..].Trim().TrimEnd('#').Trim();
    }

    /// <summary>The trimmed cells of one Markdown table row line.</summary>
    /// <param name="line">The table row line to split.</param>
    /// <returns>The trimmed cells, or an empty list for a single empty cell.</returns>
    private static List<string> Cells(string line)
    {
        var cells = line.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToList();

        return cells is [{ Length: 0 }] ? [] : cells;
    }

    /// <summary>A copy of an element with one field added.</summary>
    /// <param name="element">The element to copy.</param>
    /// <param name="field">The field name to add.</param>
    /// <param name="writeValue">The action writing the field value.</param>
    /// <returns>The copied element carrying the added field.</returns>
    private static JsonElement WithField(JsonElement element, string field, Action<Utf8JsonWriter> writeValue)
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();

            foreach (var property in element.EnumerateObject())
            {
                property.WriteTo(writer);
            }

            writer.WritePropertyName(field);
            writeValue(writer);
            writer.WriteEndObject();
        }

        using var document = JsonDocument.Parse(stream.ToArray());

        return document.RootElement.Clone();
    }

    /// <summary>Writes transition rows as a JSON array of from, to and by objects.</summary>
    /// <param name="writer">The JSON writer to write the array into.</param>
    /// <param name="rows">The transition rows to write.</param>
    private static void WriteTransitions(Utf8JsonWriter writer, IReadOnlyList<(string From, string To, string? By)> rows)
    {
        writer.WriteStartArray();

        foreach (var (from, to, by) in rows)
        {
            writer.WriteStartObject();
            writer.WriteString(RecordFields.From, from);
            writer.WriteString(RecordFields.To, to);

            if (by is not null)
            {
                writer.WriteString(RecordFields.By, by);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    /// <summary>Writes data rows as a JSON array of name, meaning and classification objects.</summary>
    /// <param name="writer">The JSON writer to write the array into.</param>
    /// <param name="rows">The data rows to write.</param>
    private static void WriteData(Utf8JsonWriter writer, IReadOnlyList<(string Name, string Meaning, string Classification)> rows)
    {
        writer.WriteStartArray();

        foreach (var (name, meaning, classification) in rows)
        {
            writer.WriteStartObject();
            writer.WriteString(RecordFields.Name, name);
            writer.WriteString(RecordFields.Meaning, meaning);
            writer.WriteString(RecordFields.Classification, classification);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    /// <summary>Writes example rows as a JSON array of objects keyed by header.</summary>
    /// <param name="writer">The JSON writer to write the array into.</param>
    /// <param name="rows">The example rows to write.</param>
    private static void WriteExamples(Utf8JsonWriter writer, IReadOnlyList<IReadOnlyDictionary<string, string>> rows)
    {
        writer.WriteStartArray();

        foreach (var row in rows)
        {
            writer.WriteStartObject();

            foreach (var (key, value) in row)
            {
                writer.WriteString(key, value);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    /// <summary>Builds one record from its element, inheriting page owner, status, decider and decide-by.</summary>
    /// <param name="element">The written element carrying the record fields.</param>
    /// <param name="file">The absolute path of the page.</param>
    /// <param name="line">The 1-based line of the record on its page.</param>
    /// <param name="isEntry">Whether the record is an entry on its page rather than the page itself.</param>
    /// <param name="pageId">The id of the carrying page, or null for a page record.</param>
    /// <param name="pageOwner">The page owner inherited when the record names none.</param>
    /// <param name="pageStatus">The page status inherited when the record names none.</param>
    /// <param name="pageDecider">The page decider inherited when the record names none.</param>
    /// <param name="pageDecideBy">The page decide-by date inherited when the record names none.</param>
    /// <returns>The built record.</returns>
    private static Record BuildRecord(
        JsonElement element,
        string file,
        int line,
        bool isEntry,
        string? pageId,
        string? pageOwner,
        string? pageStatus,
        string? pageDecider,
        string? pageDecideBy) =>
        new()
        {
            Type = StringField(element, RecordFields.Type) ?? "",
            Id = StringField(element, RecordFields.Id) ?? "",
            Title = StringField(element, RecordFields.Title) ?? "",
            Owner = StringField(element, RecordFields.Owner) ?? pageOwner,
            Status = StringField(element, RecordFields.Status) ?? pageStatus,
            Decider = StringField(element, RecordFields.Decider) ?? pageDecider,
            DecideBy = StringField(element, RecordFields.DecideBy) ?? pageDecideBy,
            ApprovedBy = StringField(element, RecordFields.ApprovedBy),
            ApprovedOn = StringField(element, RecordFields.ApprovedOn),
            ApprovedHash = StringField(element, RecordFields.ApprovedHash),
            File = file,
            Line = line,
            IsEntry = isEntry,
            PageId = pageId,
            Raw = element.Clone(),
        };

    /// <summary>
    ///     Derives the generic refusal results: forbidden for every use case with a permission
    ///     grant, and invalid-input for every use case with a validated input. Scenarios and
    ///     tests point at them; they are never written as records.
    /// </summary>
    /// <param name="records">The written records, appended in place with the derived results.</param>
    /// <param name="forbiddenCode">The refusal code the derived forbidden results carry.</param>
    /// <param name="invalidInputCode">The refusal code the derived invalid-input results carry.</param>
    private static void DeriveResults(List<Record> records, string forbiddenCode, string invalidInputCode)
    {
        var taken = records
            .Where(record => record.Id.Length > 0)
            .Select(record => record.Id)
            .ToHashSet(StringComparer.Ordinal);
        var derived = new List<Record>();

        foreach (var useCase in records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (!Identifiers.TrySplit(useCase.Id, out _, out var home, out var name) || home is null)
            {
                continue;
            }

            if (ModelBuilder.GrantedPermission(useCase) is not null
                && taken.Add($"result/{name}.forbidden"))
            {
                derived.Add(DerivedResult(
                    useCase,
                    $"result/{name}.forbidden",
                    "Forbidden",
                    "The caller lacks the grant the use case requires.",
                    forbiddenCode));
            }

            if (ModelBuilder.HasValidatedInput(useCase)
                && taken.Add($"result/{name}.invalid-input"))
            {
                derived.Add(DerivedResult(
                    useCase,
                    $"result/{name}.invalid-input",
                    "Invalid input",
                    "An input value breaks the rules the use case declares.",
                    invalidInputCode));
            }
        }

        records.AddRange(derived);
    }

    /// <summary>One derived generic result, carried by its use case's page.</summary>
    /// <param name="useCase">The use case the derived result belongs to.</param>
    /// <param name="id">The derived result id.</param>
    /// <param name="title">The derived result title.</param>
    /// <param name="meaning">The derived result meaning sentence.</param>
    /// <param name="code">The refusal code the derived result carries.</param>
    /// <returns>The derived result record, marked derived and carried by the use-case page.</returns>
    private static Record DerivedResult(Record useCase, string id, string title, string meaning, string code)
    {
        var json = JsonSerializer.Serialize(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [RecordFields.Type] = RecordTypes.Result,
            [RecordFields.Id] = id,
            [RecordFields.Title] = title,
            [RecordFields.Outcome] = "refusal",
            [RecordFields.Code] = code,
            [RecordFields.Meaning] = meaning,
        });

        using var document = JsonDocument.Parse(json);

        return new Record
        {
            Type = RecordTypes.Result,
            Id = id,
            Title = title,
            Owner = useCase.Owner,
            Status = useCase.Status,
            Decider = useCase.Decider,
            DecideBy = useCase.DecideBy,
            File = useCase.File,
            Line = useCase.Line,
            IsEntry = true,
            PageId = useCase.Id,
            Derived = true,
            Raw = document.RootElement.Clone(),
        };
    }

    /// <summary>Reads the glossary terms into records, reporting a glossary that is not JSON.</summary>
    /// <param name="config">The consumer configuration holding the glossary path.</param>
    /// <param name="records">The records read so far, appended in place.</param>
    /// <param name="problems">The problems found so far, appended in place.</param>
    private static void ReadTerms(ConsumerConfig config, List<Record> records, List<Finding> problems)
    {
        if (!File.Exists(config.GlossaryPath))
        {
            return;
        }

        var text = SourceTree.Read(config.GlossaryPath);

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException exception)
        {
            problems.Add(new Finding(
                "field-invalid", $"The glossary is not JSON: {exception.Message}",
                config.GlossaryPath, 1, null));
            return;
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty(RecordFields.Terms, out var terms)
                || terms.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var term in terms.EnumerateArray())
            {
                if (term.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var id = StringField(term, RecordFields.Id) ?? "";
                var line = id.Length > 0 ? LineOf(text, id) : 1;

                records.Add(new Record
                {
                    Type = RecordTypes.Term,
                    Id = id,
                    Title = StringField(term, RecordFields.Term) ?? "",
                    Owner = StringField(term, RecordFields.Owner),
                    Status = StringField(term, RecordFields.Status),
                    Decider = StringField(term, RecordFields.Decider),
                    DecideBy = StringField(term, RecordFields.DecideBy),
                    ApprovedBy = StringField(term, RecordFields.ApprovedBy),
                    ApprovedOn = StringField(term, RecordFields.ApprovedOn),
                    ApprovedHash = StringField(term, RecordFields.ApprovedHash),
                    File = config.GlossaryPath,
                    Line = line,
                    IsEntry = true,
                    Raw = term.Clone(),
                });
            }
        }
    }

    /// <summary>Reads the provision ids of the carried LiteSpec release.</summary>
    /// <returns>The provision ids, or an empty set when no release is found.</returns>
    private static HashSet<string> ReadProvisions()
    {
        var home = LiteSpecHome.Root;

        if (home is null || !Directory.Exists(Path.Combine(home, "docs")))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return new HashSet<string>(
            Provisions.Collect(home).Select(row => row.Id),
            StringComparer.Ordinal);
    }

    /// <summary>Reads one optional string field of an element.</summary>
    /// <param name="element">The element to read.</param>
    /// <param name="name">The field name to read.</param>
    /// <returns>The field string value, or null when the field is missing or not a string.</returns>
    private static string? StringField(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>The 1-based line where the needle first occurs, or 1.</summary>
    /// <param name="text">The text to search.</param>
    /// <param name="needle">The text whose first occurrence is located.</param>
    /// <returns>The 1-based line of the first occurrence, or 1 when the needle does not occur.</returns>
    public static int LineOf(string text, string needle)
    {
        var at = text.IndexOf(needle, StringComparison.Ordinal);

        if (at < 0)
        {
            return 1;
        }

        var line = 1;

        for (var index = 0; index < at; index++)
        {
            if (text[index] == '\n')
            {
                line++;
            }
        }

        return line;
    }
}
