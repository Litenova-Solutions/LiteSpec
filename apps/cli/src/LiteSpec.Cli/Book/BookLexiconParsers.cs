using LiteSpec.Model;

namespace LiteSpec.Book;

/// <summary>
///     The five carried-release table parsers of the lexicon: the record-type table,
///     the four-layer table, the Links table, the model-page sections, the
///     finding-code table and the glossary, each read from its carried page.
/// </summary>
/// <follows>litespec/core/model#name-book-labels-with-litespec-terms</follows>
static class BookLexiconParsers
{
    /// <summary>Gets the model-page sections the book explains from, in display order.</summary>
    static IReadOnlyList<string> SectionHeadings { get; } =
    [
        "Statuses", "Approval as Data", "Risk Tiers", "Check Levels", "Transcription Mode", "Questions and Ballots",
    ];

    /// <summary>The record-type table: layer and question per type, labels derived from the type.</summary>
    /// <param name="file">The carried page.</param>
    /// <param name="lines">The page lines.</param>
    /// <returns>The record types by id.</returns>
    public static Dictionary<string, LexiconType> ParseTypes(string file, string[] lines)
    {
        var at = BookLexiconTables.HeadingIndex(lines, "Record Types and Page Styles");
        if (at < 0)
        {
            throw new BookLexiconException(file, $"'{file}' holds no '### Record Types and Page Styles' section.");
        }

        var types = new Dictionary<string, LexiconType>(StringComparer.Ordinal);
        foreach (var row in BookLexiconTables.TableRows(file, BookLexiconTables.SectionBody(lines, at), ["Type", "Record layer", "Question"], "record-type"))
        {
            var type = BookLexiconTables.StripCode(row[0]);
            if (type.Length == 0)
            {
                throw new BookLexiconException(file, $"'{file}' record-type table names no type.");
            }

            if (!types.ContainsKey(type))
            {
                types[type] = new LexiconType(BookLexiconTables.TypeLabel(type), BookLexiconTables.TypePlural(type), row[1], row[2]);
            }
        }

        return types;
    }

    /// <summary>The four-layer table, with governance and glossary beside it, which state no writers.</summary>
    /// <param name="file">The carried page.</param>
    /// <param name="lines">The page lines.</param>
    /// <returns>The record layers by key.</returns>
    public static Dictionary<string, LexiconLayer> ParseLayers(string file, string[] lines)
    {
        var at = BookLexiconTables.HeadingIndex(lines, "The Four Record Layers");
        if (at < 0)
        {
            throw new BookLexiconException(file, $"'{file}' holds no '### The Four Record Layers' section.");
        }

        var layers = new Dictionary<string, LexiconLayer>(StringComparer.Ordinal);
        foreach (var row in BookLexiconTables.TableRows(file, BookLexiconTables.SectionBody(lines, at),
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
                layers[key] = new LexiconLayer(BookLexiconTables.TypeLabel(key), "", "");
            }
        }

        return layers;
    }

    /// <summary>The Links-table rows: one entry per row, split per carrier when the row reads two ways.</summary>
    /// <param name="file">The carried page.</param>
    /// <param name="lines">The page lines.</param>
    /// <returns>The link phrases, in table row order.</returns>
    public static List<LexiconLink> ParseLinks(string file, string[] lines)
    {
        var at = BookLexiconTables.HeadingIndex(lines, "Links");
        if (at < 0)
        {
            throw new BookLexiconException(file, $"'{file}' holds no '### Links' section.");
        }

        var links = new List<LexiconLink>();
        foreach (var row in BookLexiconTables.TableRows(file, BookLexiconTables.SectionBody(lines, at),
            ["Field", "Written on", "Points at", "Reads as", "Read back as"], "Links"))
        {
            var field = BookLexiconTables.StripCode(row[0]);
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
    /// <param name="file">The carried page.</param>
    /// <param name="field">The link field.</param>
    /// <param name="writtenOn">The Written on cell.</param>
    /// <returns>The carrier record types.</returns>
    public static IReadOnlyList<string> CarriersFor(string file, string field, string writtenOn)
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
    /// <param name="file">The carried page.</param>
    /// <param name="lines">The page lines.</param>
    /// <returns>The sections by title.</returns>
    /// <follows>litespec/core/model#explain-the-book-from-litespec-definitions</follows>
    public static Dictionary<string, LexiconSection> ParseSections(string file, string[] lines)
    {
        var sections = new Dictionary<string, LexiconSection>(StringComparer.Ordinal);
        foreach (var heading in SectionHeadings)
        {
            var at = BookLexiconTables.HeadingIndex(lines, heading);
            if (at < 0)
            {
                throw new BookLexiconException(file, $"'{file}' holds no '### {heading}' section.");
            }

            var text = BookLexiconTables.FirstParagraph(BookLexiconTables.SectionBody(lines, at));
            if (text.Length == 0)
            {
                throw new BookLexiconException(file, $"'{file}' section '### {heading}' holds no text.");
            }

            sections[heading] = new LexiconSection(text, $"model.md, {heading}");
        }

        return sections;
    }

    /// <summary>The finding-code table: check level and meaning per code, read by column name.</summary>
    /// <param name="file">The carried page.</param>
    /// <param name="lines">The page lines.</param>
    /// <returns>The findings by code.</returns>
    public static Dictionary<string, LexiconFinding> ParseFindings(string file, string[] lines)
    {
        var findings = new Dictionary<string, LexiconFinding>(StringComparer.Ordinal);
        var (positions, rows) = BookLexiconTables.NamedTable(file, lines, ["Code", "Level", "Meaning"], "finding-code");
        foreach (var row in rows)
        {
            var code = BookLexiconTables.StripCode(row[positions["Code"]]);
            if (!findings.ContainsKey(code))
            {
                findings[code] = new LexiconFinding(row[positions["Level"]], row[positions["Meaning"]]);
            }
        }

        return findings;
    }

    /// <summary>The glossary entries by lowercase term: definition, layer and source each.</summary>
    /// <param name="file">The carried page.</param>
    /// <param name="lines">The page lines.</param>
    /// <returns>The glossary entries by lowercase term.</returns>
    /// <follows>litespec/core/model#explain-the-book-from-litespec-definitions</follows>
    public static Dictionary<string, LexiconGlossaryEntry> ParseGlossary(string file, string[] lines)
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
            foreach (var paragraph in BookLexiconTables.Paragraphs(BookLexiconTables.SectionBody(lines, i)))
            {
                if (paragraph.StartsWith("Layer:", StringComparison.Ordinal))
                {
                    layer = BookLexiconTables.EmptyToNull(BookLexiconTables.Unfield(paragraph, "Layer:"));
                }
                else if (paragraph.StartsWith("Source:", StringComparison.Ordinal))
                {
                    source = BookLexiconTables.EmptyToNull(BookLexiconTables.Unfield(paragraph, "Source:"));
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
}
