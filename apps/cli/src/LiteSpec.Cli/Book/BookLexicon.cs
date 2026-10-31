using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Book;

/// <summary>One record type's book words: its display label, plural, layer and question.</summary>
/// <param name="Label">The display label, such as Use case.</param>
/// <param name="Plural">The display plural, such as Use cases.</param>
/// <param name="Layer">The record layer the record-type table states, verbatim.</param>
/// <param name="Question">The question the record-type table states, verbatim.</param>
sealed record LexiconType(string Label, string Plural, string Layer, string Question);

/// <summary>One record layer's book words: its display label, writers and reviewers.</summary>
/// <param name="Label">The display label, such as Intent.</param>
/// <param name="Writes">Who writes the layer, in the four-layer table's words.</param>
/// <param name="Reviews">Who reviews the layer, in the four-layer table's words.</param>
sealed record LexiconLayer(string Label, string Writes, string Reviews);

/// <summary>One Links-table row's book words: the field, its carriers and both phrases.</summary>
/// <param name="Field">The link field, such as serves or access.grant.</param>
/// <param name="Carriers">The record types carrying the link, as kebab-case ids.</param>
/// <param name="Forward">How the link reads from carrier to target, in the table's words.</param>
/// <param name="Inverse">How the link reads from target to carrier, in the table's words.</param>
sealed record LexiconLink(string Field, IReadOnlyList<string> Carriers, string Forward, string Inverse);

/// <summary>One glossary term's book words: its definition, layer and source.</summary>
/// <param name="Term">The term, in the glossary heading's words.</param>
/// <param name="Definition">The definition paragraphs flattened to one line.</param>
/// <param name="Layer">The layer the entry states, or null when it states none.</param>
/// <param name="Source">The source the entry states, or null when it states none.</param>
sealed record LexiconGlossaryEntry(string Term, string Definition, string? Layer, string? Source);

/// <summary>One model-page section's book words: its first paragraph and its source.</summary>
/// <param name="Text">The section's first paragraph, verbatim.</param>
/// <param name="Source">The LiteSpec source, such as model.md, Statuses.</param>
sealed record LexiconSection(string Text, string Source);

/// <summary>One finding code's book words: its tier and its meaning.</summary>
/// <param name="Tier">The tier the finding-code table states.</param>
/// <param name="Meaning">The meaning the finding-code table states, verbatim.</param>
sealed record LexiconFinding(string Tier, string Meaning);

/// <summary>A carried LiteSpec page the lexicon needs is missing or lost its shape.</summary>
sealed class BookLexiconException : Exception
{
    /// <summary>Builds the failure for the page at <paramref name="file"/>.</summary>
    /// <param name="file">The carried page that is missing or lost its shape.</param>
    /// <param name="message">What the page lacks.</param>
    public BookLexiconException(string file, string message)
        : base(message)
    {
        File = file;
    }

    /// <summary>The carried page that is missing or lost its shape.</summary>
    public string File { get; }
}

/// <summary>
///     The LiteSpec words the book renders: type labels, layer writers, link phrases,
///     field labels, glossary definitions, section texts and finding meanings. Every
///     word comes from the carried release; nothing is written by hand.
/// </summary>
/// <follows>litespec/core/model#name-book-labels-with-litespec-terms</follows>
sealed class BookLexicon
{
    /// <summary>The record types by id, in record-type table order.</summary>
    public IReadOnlyDictionary<string, LexiconType> Types { get; }

    /// <summary>The record layers by key, in four-layer table order, then governance and glossary.</summary>
    public IReadOnlyDictionary<string, LexiconLayer> Layers { get; }

    /// <summary>The link phrases, in Links-table row order.</summary>
    public IReadOnlyList<LexiconLink> Links { get; }

    /// <summary>The field labels by field name, seeded from the book's field-label table.</summary>
    public IReadOnlyDictionary<string, string> Fields { get; } = FieldLabels;

    /// <summary>The glossary entries by lowercase term, in glossary order.</summary>
    public IReadOnlyDictionary<string, LexiconGlossaryEntry> Glossary { get; }

    /// <summary>The model-page sections by title, in display order.</summary>
    public IReadOnlyDictionary<string, LexiconSection> Sections { get; }

    /// <summary>The finding codes by code, in finding-code table order.</summary>
    public IReadOnlyDictionary<string, LexiconFinding> Findings { get; }

    BookLexicon(
        IReadOnlyDictionary<string, LexiconType> types,
        IReadOnlyDictionary<string, LexiconLayer> layers,
        IReadOnlyList<LexiconLink> links,
        IReadOnlyDictionary<string, LexiconGlossaryEntry> glossary,
        IReadOnlyDictionary<string, LexiconSection> sections,
        IReadOnlyDictionary<string, LexiconFinding> findings)
    {
        Types = types;
        Layers = layers;
        Links = links;
        Glossary = glossary;
        Sections = sections;
        Findings = findings;
    }

    /// <summary>Reads the lexicon from the LiteSpec release rooted at <paramref name="liteSpecRoot"/>.</summary>
    public static BookLexicon Load(string liteSpecRoot)
    {
        var modelFile = Path.Combine(liteSpecRoot, "docs", "core", "model.md");
        var toolsFile = Path.Combine(liteSpecRoot, "docs", "tools", "model.md");
        var glossaryFile = Path.Combine(liteSpecRoot, "docs", "reference", "glossary.md");

        var model = ReadLines(modelFile);
        var layers = ParseLayers(modelFile, model);
        var types = ParseTypes(modelFile, model);
        var links = ParseLinks(modelFile, model);
        var sections = ParseSections(modelFile, model);
        var findings = ParseFindings(toolsFile, ReadLines(toolsFile));
        var glossary = ParseGlossary(glossaryFile, ReadLines(glossaryFile));

        return new BookLexicon(types, layers, links, glossary, sections, findings);
    }

    /// <summary>
    ///     Resolves a link field path to its Links-table phrases: the longest dotted prefix
    ///     naming a table field wins, so effects.calls reads as its effects row. A leading
    ///     record type is ignored, so use-case.effects.calls reads the same.
    /// </summary>
    /// <param name="field">The edge's field key, such as effects.calls.</param>
    /// <param name="carrier">The linking record's type, choosing between rows sharing one field, or null for the table's first row.</param>
    /// <param name="forward">How the link reads from carrier to target.</param>
    /// <param name="inverse">How the link reads from target to carrier.</param>
    /// <returns>Whether a Links-table row phrases the path.</returns>
    public bool TryPhrase(string field, string? carrier, out string forward, out string inverse)
    {
        var candidate = field;
        var dot = candidate.IndexOf('.');
        if (dot > 0 && RecordTypes.All.Contains(candidate[..dot]))
        {
            candidate = candidate[(dot + 1)..];
        }

        while (true)
        {
            var matches = Links.Where(link => link.Field == candidate).ToList();
            if (matches.Count > 0)
            {
                var pick = carrier is not null
                    ? matches.FirstOrDefault(link => link.Carriers.Contains(carrier)) ?? matches[0]
                    : matches[0];
                forward = pick.Forward;
                inverse = pick.Inverse;
                return true;
            }

            var nested = candidate.LastIndexOf('.');
            if (nested < 0)
            {
                forward = "";
                inverse = "";
                return false;
            }

            candidate = candidate[..nested];
        }
    }

    /// <summary>The lexicon shard value: types, layers, links, fields, glossary, sections and findings.</summary>
    public JsonObject ToJson()
    {
        var types = new JsonObject();
        foreach (var (key, value) in Types)
        {
            types[key] = new JsonObject
            {
                ["label"] = value.Label,
                ["plural"] = value.Plural,
                ["layer"] = value.Layer,
                ["question"] = value.Question,
            };
        }

        var layers = new JsonObject();
        foreach (var (key, value) in Layers)
        {
            layers[key] = new JsonObject
            {
                ["label"] = value.Label,
                ["writes"] = value.Writes,
                ["reviews"] = value.Reviews,
            };
        }

        var links = new JsonArray([.. Links.Select(link => (JsonNode?)new JsonObject
        {
            ["field"] = link.Field,
            ["carriers"] = new JsonArray([.. link.Carriers.Select(carrier => (JsonNode?)carrier)]),
            ["forward"] = link.Forward,
            ["inverse"] = link.Inverse,
        })]);

        var fields = new JsonObject();
        foreach (var (key, value) in Fields)
        {
            fields[key] = value;
        }

        var glossary = new JsonObject();
        foreach (var (key, value) in Glossary)
        {
            glossary[key] = new JsonObject
            {
                ["term"] = value.Term,
                ["definition"] = value.Definition,
                ["layer"] = StringOrNull(value.Layer),
                ["source"] = StringOrNull(value.Source),
            };
        }

        var sections = new JsonObject();
        foreach (var (key, value) in Sections)
        {
            sections[key] = new JsonObject
            {
                ["text"] = value.Text,
                ["source"] = value.Source,
            };
        }

        var findings = new JsonObject();
        foreach (var (key, value) in Findings)
        {
            findings[key] = new JsonObject
            {
                ["tier"] = value.Tier,
                ["meaning"] = value.Meaning,
            };
        }

        return new JsonObject
        {
            ["types"] = types,
            ["layers"] = layers,
            ["links"] = links,
            ["fields"] = fields,
            ["glossary"] = glossary,
            ["sections"] = sections,
            ["findings"] = findings,
        };
    }

    /// <summary>The model-page sections the book explains from, in display order.</summary>
    static readonly IReadOnlyList<string> SectionHeadings =
    [
        "Statuses", "Approval as Data", "Risk Tiers", "Check Levels", "Transcription Mode", "Questions and Ballots",
    ];

    /// <summary>Reads the carried page's lines, failing with the file when it cannot be read.</summary>
    static string[] ReadLines(string file)
    {
        try
        {
            return File.ReadAllText(file).Replace("\r\n", "\n").Split('\n');
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new BookLexiconException(file, $"Cannot read '{file}': {exception.Message}");
        }
    }

    /// <summary>The line holding the ### heading, or -1 when the page holds none.</summary>
    static int HeadingIndex(string[] lines, string heading)
    {
        var want = $"### {heading}";
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim() == want)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>The lines under a heading, up to the next heading.</summary>
    static List<string> SectionBody(string[] lines, int heading)
    {
        var body = new List<string>();
        for (var i = heading + 1; i < lines.Length && !lines[i].StartsWith('#'); i++)
        {
            body.Add(lines[i]);
        }

        return body;
    }

    /// <summary>Whether the line is a Markdown table row.</summary>
    static bool IsTableLine(string line) => line.StartsWith('|') && line.TrimEnd().EndsWith('|');

    /// <summary>Whether the line is a Markdown table separator row.</summary>
    static bool IsSeparator(string line) =>
        IsTableLine(line)
        && Cells(line).All(cell => cell.Length > 0 && cell.All(ch => ch is '-' or ':') && cell.Contains('-'));

    /// <summary>Splits a table row into its trimmed cells.</summary>
    static string[] Cells(string line) =>
        line.Split('|').Skip(1).SkipLast(1).Select(cell => cell.Trim()).ToArray();

    /// <summary>Strips one pair of backticks from a table cell.</summary>
    static string StripCode(string cell)
    {
        cell = cell.Trim();
        return cell.Length >= 2 && cell.StartsWith('`') && cell.EndsWith('`') ? cell[1..^1] : cell;
    }

    /// <summary>The first table holding every named column, with each column's position and its rows; extra columns are ignored.</summary>
    static (IReadOnlyDictionary<string, int> Positions, List<string[]> Rows) NamedTable(
        string file, IReadOnlyList<string> body, string[] names, string what)
    {
        for (var i = 0; i < body.Count; i++)
        {
            if (!IsTableLine(body[i]))
            {
                continue;
            }

            var header = Cells(body[i]);
            var positions = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var c = 0; c < header.Length; c++)
            {
                positions.TryAdd(header[c], c);
            }

            if (!names.All(positions.ContainsKey))
            {
                continue;
            }

            if (i + 1 >= body.Count || !IsSeparator(body[i + 1]))
            {
                throw new BookLexiconException(file, $"'{file}' {what} lost its table shape.");
            }

            var rows = new List<string[]>();
            for (var j = i + 2; j < body.Count && IsTableLine(body[j]); j++)
            {
                var row = Cells(body[j]);
                if (row.Length != header.Length)
                {
                    throw new BookLexiconException(file, $"'{file}' {what} lost its table shape.");
                }

                rows.Add(row);
            }

            if (rows.Count == 0)
            {
                throw new BookLexiconException(file, $"'{file}' {what} holds no rows.");
            }

            return (positions, rows);
        }

        throw new BookLexiconException(file, $"'{file}' holds no {what} table.");
    }

    /// <summary>The first table with the header, failing with the file when none holds rows.</summary>
    static List<string[]> TableRows(string file, IReadOnlyList<string> body, string[] header, string what)
    {
        for (var i = 0; i < body.Count; i++)
        {
            if (!IsTableLine(body[i]) || !Cells(body[i]).SequenceEqual(header, StringComparer.Ordinal))
            {
                continue;
            }

            if (i + 1 >= body.Count || !IsSeparator(body[i + 1]))
            {
                throw new BookLexiconException(file, $"'{file}' {what} lost its table shape.");
            }

            var rows = new List<string[]>();
            for (var j = i + 2; j < body.Count && IsTableLine(body[j]); j++)
            {
                var row = Cells(body[j]);
                if (row.Length != header.Length)
                {
                    throw new BookLexiconException(file, $"'{file}' {what} lost its table shape.");
                }

                rows.Add(row);
            }

            if (rows.Count == 0)
            {
                throw new BookLexiconException(file, $"'{file}' {what} holds no rows.");
            }

            return rows;
        }

        throw new BookLexiconException(file, $"'{file}' holds no {what} table.");
    }

    /// <summary>The blank-line paragraphs of a section body, each flattened to one line.</summary>
    static List<string> Paragraphs(List<string> body)
    {
        var paragraphs = new List<string>();
        var current = new List<string>();
        foreach (var line in body)
        {
            if (line.Trim().Length == 0)
            {
                if (current.Count > 0)
                {
                    paragraphs.Add(string.Join(" ", current));
                    current.Clear();
                }
            }
            else
            {
                current.Add(line.Trim());
            }
        }

        if (current.Count > 0)
        {
            paragraphs.Add(string.Join(" ", current));
        }

        return paragraphs;
    }

    /// <summary>The first paragraph of a section body, verbatim.</summary>
    static string FirstParagraph(List<string> body)
    {
        var paragraph = new List<string>();
        var started = false;
        foreach (var line in body)
        {
            if (line.Trim().Length == 0)
            {
                if (started)
                {
                    break;
                }
            }
            else
            {
                started = true;
                paragraph.Add(line.TrimEnd());
            }
        }

        return string.Join("\n", paragraph).Trim();
    }

    /// <summary>The display label of a record type: hyphens become spaces, the first letter turns.</summary>
    static string TypeLabel(string type)
    {
        var words = type.Replace('-', ' ');
        return char.ToUpperInvariant(words[0]) + words[1..];
    }

    /// <summary>The display plural of a record type: the single product and research stay whole, a consonant before y turns to ies, the rest take s.</summary>
    static string TypePlural(string type)
    {
        var label = TypeLabel(type);
        if (type is "product" or "research")
        {
            return label;
        }

        if (label.Length > 1 && label[^1] == 'y' && !"aeiou".Contains(char.ToLowerInvariant(label[^2])))
        {
            return label[..^1] + "ies";
        }

        return label + "s";
    }

    /// <summary>A JSON string, or JSON null when the text is absent.</summary>
    static JsonNode StringOrNull(string? value) =>
        value is null ? JsonNode.Parse("null")! : JsonValue.Create(value)!;

    /// <summary>The metadata value of a glossary paragraph, without its prefix and closing period.</summary>
    static string Unfield(string paragraph, string prefix) =>
        paragraph[prefix.Length..].Trim().TrimEnd('.').TrimEnd();

    /// <summary>Null when the metadata value holds nothing.</summary>
    static string? EmptyToNull(string value) => value.Length == 0 ? null : value;

    /// <summary>The record-type table: layer and question per type, labels derived from the type.</summary>
    static Dictionary<string, LexiconType> ParseTypes(string file, string[] lines)
    {
        var at = HeadingIndex(lines, "Record Types and Page Styles");
        if (at < 0)
        {
            throw new BookLexiconException(file, $"'{file}' holds no '### Record Types and Page Styles' section.");
        }

        var types = new Dictionary<string, LexiconType>(StringComparer.Ordinal);
        foreach (var row in TableRows(file, SectionBody(lines, at), ["Type", "Record layer", "Question"], "record-type"))
        {
            var type = StripCode(row[0]);
            if (type.Length == 0)
            {
                throw new BookLexiconException(file, $"'{file}' record-type table names no type.");
            }

            if (!types.ContainsKey(type))
            {
                types[type] = new LexiconType(TypeLabel(type), TypePlural(type), row[1], row[2]);
            }
        }

        return types;
    }

    /// <summary>The four-layer table, with governance and glossary beside it, which state no writers.</summary>
    static Dictionary<string, LexiconLayer> ParseLayers(string file, string[] lines)
    {
        var at = HeadingIndex(lines, "The Four Record Layers");
        if (at < 0)
        {
            throw new BookLexiconException(file, $"'{file}' holds no '### The Four Record Layers' section.");
        }

        var layers = new Dictionary<string, LexiconLayer>(StringComparer.Ordinal);
        foreach (var row in TableRows(file, SectionBody(lines, at),
            ["Record layer", "Question", "Writes", "Reviews", "Records"], "four-layer"))
        {
            var key = row[0].ToLowerInvariant();
            if (!layers.ContainsKey(key))
            {
                layers[key] = new LexiconLayer(row[0], row[2], row[3]);
            }
        }

        // Governance and glossary group records in the outline but name no writers
        // on the model page, so they read with empty writers beside the four rows.
        foreach (var key in new[] { "governance", "glossary" })
        {
            if (!layers.ContainsKey(key))
            {
                layers[key] = new LexiconLayer(TypeLabel(key), "", "");
            }
        }

        return layers;
    }

    /// <summary>The Links-table rows: one entry per row, split per carrier when the row reads two ways.</summary>
    static List<LexiconLink> ParseLinks(string file, string[] lines)
    {
        var at = HeadingIndex(lines, "Links");
        if (at < 0)
        {
            throw new BookLexiconException(file, $"'{file}' holds no '### Links' section.");
        }

        var links = new List<LexiconLink>();
        foreach (var row in TableRows(file, SectionBody(lines, at),
            ["Field", "Written on", "Points at", "Reads as", "Read back as"], "Links"))
        {
            var field = StripCode(row[0]);
            var carriers = CarriersFor(file, field, row[1]);
            var forwards = row[3].Split(" / ", StringSplitOptions.None).Select(phrase => phrase.Trim()).ToList();
            var inverses = row[4].Split(" / ", StringSplitOptions.None).Select(phrase => phrase.Trim()).ToList();
            if (carriers.Count > 1 && forwards.Count == carriers.Count && inverses.Count == carriers.Count)
            {
                for (var i = 0; i < carriers.Count; i++)
                {
                    links.Add(new LexiconLink(field, [carriers[i]], forwards[i], inverses[i]));
                }
            }
            else
            {
                links.Add(new LexiconLink(field, carriers, row[3], row[4]));
            }
        }

        return links;
    }

    /// <summary>The Written on cell as record-type ids: any record widens to every type.</summary>
    static IReadOnlyList<string> CarriersFor(string file, string field, string writtenOn)
    {
        if (writtenOn.Trim().Equals("any record", StringComparison.OrdinalIgnoreCase))
        {
            return RecordTypes.All.OrderBy(type => type, StringComparer.Ordinal).ToList();
        }

        var carriers = writtenOn
            .Split(',', StringSplitOptions.TrimEntries)
            .Select(carrier => carrier.ToLowerInvariant().Replace(' ', '-'))
            .ToList();

        foreach (var carrier in carriers)
        {
            if (!RecordTypes.All.Contains(carrier))
            {
                throw new BookLexiconException(file, $"'{file}' Links row '{field}' names '{carrier}' in Written on, which is no record type.");
            }
        }

        return carriers;
    }

    /// <summary>The model-page sections the mockup explains from: first paragraph and source each.</summary>
    /// <follows>litespec/core/model#explain-the-book-from-litespec-definitions</follows>
    static Dictionary<string, LexiconSection> ParseSections(string file, string[] lines)
    {
        var sections = new Dictionary<string, LexiconSection>(StringComparer.Ordinal);
        foreach (var heading in SectionHeadings)
        {
            var at = HeadingIndex(lines, heading);
            if (at < 0)
            {
                throw new BookLexiconException(file, $"'{file}' holds no '### {heading}' section.");
            }

            var text = FirstParagraph(SectionBody(lines, at));
            if (text.Length == 0)
            {
                throw new BookLexiconException(file, $"'{file}' section '### {heading}' holds no text.");
            }

            sections[heading] = new LexiconSection(text, $"model.md, {heading}");
        }

        return sections;
    }

    /// <summary>The finding-code table: check level and meaning per code, read by column name.</summary>
    static Dictionary<string, LexiconFinding> ParseFindings(string file, string[] lines)
    {
        var findings = new Dictionary<string, LexiconFinding>(StringComparer.Ordinal);
        var (positions, rows) = NamedTable(file, lines, ["Code", "Level", "Meaning"], "finding-code");
        foreach (var row in rows)
        {
            var code = StripCode(row[positions["Code"]]);
            if (!findings.ContainsKey(code))
            {
                findings[code] = new LexiconFinding(row[positions["Level"]], row[positions["Meaning"]]);
            }
        }

        return findings;
    }

    /// <summary>The glossary entries by lowercase term: definition, layer and source each.</summary>
    /// <follows>litespec/core/model#explain-the-book-from-litespec-definitions</follows>
    static Dictionary<string, LexiconGlossaryEntry> ParseGlossary(string file, string[] lines)
    {
        var glossary = new Dictionary<string, LexiconGlossaryEntry>(StringComparer.Ordinal);
        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith("### ", StringComparison.Ordinal))
            {
                continue;
            }

            var term = lines[i]["### ".Length..].Trim();
            var definition = new List<string>();
            string? layer = null;
            string? source = null;
            foreach (var paragraph in Paragraphs(SectionBody(lines, i)))
            {
                if (paragraph.StartsWith("Layer:", StringComparison.Ordinal))
                {
                    layer = EmptyToNull(Unfield(paragraph, "Layer:"));
                }
                else if (paragraph.StartsWith("Source:", StringComparison.Ordinal))
                {
                    source = EmptyToNull(Unfield(paragraph, "Source:"));
                }
                else if (!paragraph.StartsWith("Aliases:", StringComparison.Ordinal)
                    && !paragraph.StartsWith("Rejected:", StringComparison.Ordinal))
                {
                    definition.Add(paragraph);
                }
            }

            if (definition.Count == 0)
            {
                throw new BookLexiconException(file, $"'{file}' term '{term}' holds no definition.");
            }

            var key = term.ToLowerInvariant();
            if (!glossary.ContainsKey(key))
            {
                glossary[key] = new LexiconGlossaryEntry(term, string.Join(" ", definition), layer, source);
            }
        }

        if (glossary.Count == 0)
        {
            throw new BookLexiconException(file, $"'{file}' holds no glossary terms.");
        }

        return glossary;
    }

    static readonly IReadOnlyDictionary<string, string> FieldLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["boundary"] = "Boundary", ["exclusions"] = "Exclusions", ["addresses"] = "Addresses",
            ["source"] = "Source", ["provenance"] = "Provenance", ["authority"] = "Authority", ["applies"] = "Applies", ["appliesBecause"] = "Applies because",
            ["question"] = "Question", ["sources"] = "Sources", ["readOn"] = "Read on", ["conclusion"] = "Conclusion",
            ["options"] = "Options", ["choice"] = "Choice", ["pros"] = "Pros", ["cons"] = "Cons", ["reopenedWhen"] = "Reopened when",
            ["kind"] = "Kind", ["statement"] = "Statement", ["rationale"] = "Rationale",
            ["measure"] = "Measure", ["target"] = "Target", ["dueBy"] = "Due by", ["plannedFor"] = "Planned for", ["values"] = "Values", ["effectiveFrom"] = "Effective from",
            ["conditions"] = "Conditions", ["check"] = "Check", ["runsOn"] = "Runs on", ["evidence"] = "Evidence",
            ["except"] = "Except", ["condition"] = "Condition", ["fallback"] = "Fallback", ["setBy"] = "Set by", ["risk"] = "Risk",
            ["lifecycle"] = "Lifecycle", ["refusedBecause"] = "Refused because",
            ["scope"] = "Scope", ["selection"] = "Selection", ["code"] = "Code", ["codes"] = "Codes", ["meaning"] = "Meaning", ["recorded"] = "Recorded",
            ["finishedWhen"] = "Finished when", ["actor"] = "Actor", ["dependsOn"] = "Depends on",
            ["summary"] = "Summary", ["states"] = "States", ["transitions"] = "Transitions", ["data"] = "Data",
            ["dataClasses"] = "Data classes", ["steps"] = "Steps", ["exits"] = "Exits", ["drivenBy"] = "Driven by", ["cursor"] = "Cursor",
            ["reaction"] = "Reaction", ["job"] = "Job", ["person"] = "Person", ["dueWithin"] = "Due within", ["binds"] = "Binds", ["compensation"] = "Compensation",
            ["refusal"] = "Refusal", ["questions"] = "Questions", ["recommendation"] = "Recommendation",
            ["input"] = "Input", ["output"] = "Output", ["origin"] = "Origin", ["paging"] = "Paging", ["builtAsynchronously"] = "Built asynchronously",
            ["changes"] = "Changes", ["reads"] = "Reads", ["consistency"] = "Consistency", ["effects"] = "Effects",
            ["idempotency"] = "Idempotency", ["audit"] = "Audit", ["level"] = "Level", ["access"] = "Access", ["computedBy"] = "Computed by",
            ["triggeredBy"] = "Triggered by", ["skippedWhen"] = "Skipped when", ["runs"] = "Runs", ["sends"] = "Sends", ["emits"] = "Emits",
            ["calls"] = "Calls", ["forEach"] = "For each", ["onFailure"] = "On failure", ["onExhausted"] = "On exhausted",
            ["mode"] = "Mode", ["within"] = "Within", ["useCase"] = "Use case", ["operation"] = "Operation", ["event"] = "Event",
            ["retry"] = "Retry", ["maxAttempts"] = "Max attempts", ["initialDelay"] = "Initial delay", ["maxDelay"] = "Max delay",
            ["backoff"] = "Backoff", ["then"] = "Then", ["deadLetter"] = "Dead letter", ["after"] = "After", ["action"] = "Action",
            ["surfacesIn"] = "Surfaces in", ["runbook"] = "Runbook", ["every"] = "Every", ["cron"] = "Cron", ["manual"] = "Manual",
            ["outcome"] = "Outcome",
            ["given"] = "Given", ["when"] = "When", ["then"] = "Then", ["examples"] = "Examples",
            ["carries"] = "Carries", ["path"] = "Path", ["contentType"] = "Content type", ["cacheControl"] = "Cache control", ["versioning"] = "Versioning",
            ["address"] = "Address", ["entryPoint"] = "Entry point", ["floorplan"] = "Floorplan", ["offers"] = "Offers", ["implements"] = "Implements",
            ["receives"] = "Receives", ["failure"] = "Failure", ["operations"] = "Operations", ["timeout"] = "Timeout", ["onTimeout"] = "On timeout",
            ["erasure"] = "Erasure", ["acceptedBy"] = "Accepted by",
            ["direction"] = "Direction", ["eventType"] = "Event type", ["subject"] = "Subject", ["payload"] = "Payload",
            ["dataschema"] = "Dataschema", ["type"] = "Type", ["required"] = "Required", ["of"] = "Of", ["name"] = "Name", ["label"] = "Label",
            ["amount"] = "Amount", ["currency"] = "Currency", ["purpose"] = "Purpose", ["period"] = "Period", ["classification"] = "Classification",
            ["exercises"] = "Exercises", ["background"] = "Background", ["auth"] = "Auth", ["grant"] = "Grant", ["tenant"] = "Tenant",
            ["trust"] = "Trust", ["correlation"] = "Correlation", ["translates"] = "Translates",
            ["template"] = "Template", ["channel"] = "Channel", ["consent"] = "Consent", ["frequency"] = "Frequency",
            ["dedupKey"] = "Dedup key", ["params"] = "Params", ["locale"] = "Locale", ["suppressedWhen"] = "Suppressed when", ["claims"] = "Claims",
            ["schedule"] = "Schedule", ["batchSize"] = "Batch size", ["lease"] = "Lease", ["serviceLevel"] = "Service level",
            ["runsWhere"] = "Runs where", ["guard"] = "Guard",
            ["valueType"] = "Value type", ["default"] = "Default", ["range"] = "Range", ["unit"] = "Unit", ["atBound"] = "At bound", ["secret"] = "Secret", ["restart"] = "Restart",
            ["releasedBy"] = "Released by", ["frozenWhen"] = "Frozen when", ["history"] = "History",
            ["value"] = "Value", ["trigger"] = "Trigger", ["severity"] = "Severity",
            ["reason"] = "Reason", ["reviewBy"] = "Review by",
            ["covers"] = "Covers", ["expiresOn"] = "Expires on", ["paths"] = "Paths",
            ["term"] = "Term", ["labels"] = "Labels", ["rejected"] = "Rejected", ["aliases"] = "Aliases", ["layer"] = "Layer",
            ["broader"] = "Broader", ["narrower"] = "Narrower", ["mannered"] = "Mannered",
            ["decider"] = "Decider", ["decideBy"] = "Decide by", ["owner"] = "Owner",
            ["approvedBy"] = "Approved by", ["approvedOn"] = "Approved on", ["approvedHash"] = "Approval hash",
        };
}
