using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Book;

/// <summary>
///     The LiteSpec words the book renders: type labels, layer writers, link phrases,
///     field labels, glossary definitions, section texts and finding meanings. Every
///     word comes from the carried release; nothing is written by hand. The tables
///     themselves are read by <see cref="BookLexiconParsers"/>.
/// </summary>
/// <follows>litespec/core/model#name-book-labels-with-litespec-terms</follows>
sealed class BookLexicon
{
    /// <summary>Gets the record types by id, in record-type table order.</summary>
    public IReadOnlyDictionary<string, LexiconType> Types { get; }

    /// <summary>Gets the record layers by key, in four-layer table order, then governance and glossary.</summary>
    public IReadOnlyDictionary<string, LexiconLayer> Layers { get; }

    /// <summary>Gets the link phrases, in Links-table row order.</summary>
    public IReadOnlyList<LexiconLink> Links { get; }

    /// <summary>Gets the field labels by field name, seeded from the book's field-label table.</summary>
    public IReadOnlyDictionary<string, string> Fields { get; } = BookLexiconFieldLabels.Labels;

    /// <summary>Gets the glossary entries by lowercase term, in glossary order.</summary>
    public IReadOnlyDictionary<string, LexiconGlossaryEntry> Glossary { get; }

    /// <summary>Gets the model-page sections by title, in display order.</summary>
    public IReadOnlyDictionary<string, LexiconSection> Sections { get; }

    /// <summary>Gets the finding codes by code, in finding-code table order.</summary>
    public IReadOnlyDictionary<string, LexiconFinding> Findings { get; }

    /// <summary>Builds the lexicon from its parsed tables.</summary>
    /// <param name="types">The record types by id.</param>
    /// <param name="layers">The record layers by key.</param>
    /// <param name="links">The link phrases.</param>
    /// <param name="glossary">The glossary entries by lowercase term.</param>
    /// <param name="sections">The model-page sections by title.</param>
    /// <param name="findings">The finding codes by code.</param>
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
    /// <param name="liteSpecRoot">The LiteSpec release root.</param>
    /// <returns>The lexicon.</returns>
    public static BookLexicon Load(string liteSpecRoot)
    {
        var modelFile = Path.Combine(liteSpecRoot, "docs", "core", "model.md");
        var toolsFile = Path.Combine(liteSpecRoot, "docs", "tools", "model.md");
        var glossaryFile = Path.Combine(liteSpecRoot, "docs", "reference", "glossary.md");

        var model = BookLexiconTables.ReadLines(modelFile);
        var layers = BookLexiconParsers.ParseLayers(modelFile, model);
        var types = BookLexiconParsers.ParseTypes(modelFile, model);
        var links = BookLexiconParsers.ParseLinks(modelFile, model);
        var sections = BookLexiconParsers.ParseSections(modelFile, model);
        var findings = BookLexiconParsers.ParseFindings(toolsFile, BookLexiconTables.ReadLines(toolsFile));
        var glossary = BookLexiconParsers.ParseGlossary(glossaryFile, BookLexiconTables.ReadLines(glossaryFile));

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
    /// <returns>The lexicon shard value.</returns>
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
                ["layer"] = BookLexiconTables.StringOrNull(value.Layer),
                ["source"] = BookLexiconTables.StringOrNull(value.Source),
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
}
