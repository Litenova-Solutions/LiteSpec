using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     The carried-page reading the lexicon parsers share: page lines, headings,
///     Markdown tables, paragraphs, and the small text readings over them.
/// </summary>
/// <follows>litespec/core/model#name-book-labels-with-litespec-terms</follows>
static class BookLexiconTables
{
    /// <summary>Reads the carried page's lines, failing with the file when it cannot be read.</summary>
    /// <param name="file">The carried page.</param>
    /// <returns>The page lines.</returns>
    public static string[] ReadLines(string file)
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
    /// <param name="lines">The page lines.</param>
    /// <param name="heading">The heading text.</param>
    /// <returns>The heading line index, or -1.</returns>
    public static int HeadingIndex(string[] lines, string heading)
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
    /// <param name="lines">The page lines.</param>
    /// <param name="heading">The heading line index.</param>
    /// <returns>The section lines.</returns>
    public static List<string> SectionBody(string[] lines, int heading)
    {
        var body = new List<string>();
        for (var i = heading + 1; i < lines.Length && !lines[i].StartsWith('#'); i++)
        {
            body.Add(lines[i]);
        }

        return body;
    }

    /// <summary>Whether the line is a Markdown table row.</summary>
    /// <param name="line">The page line.</param>
    /// <returns>Whether the line is a table row.</returns>
    public static bool IsTableLine(string line) => line.StartsWith('|') && line.TrimEnd().EndsWith('|');

    /// <summary>Whether the line is a Markdown table separator row.</summary>
    /// <param name="line">The page line.</param>
    /// <returns>Whether the line is a separator row.</returns>
    public static bool IsSeparator(string line) =>
        IsTableLine(line)
        && Cells(line).All(cell => cell.Length > 0 && cell.All(ch => ch is '-' or ':') && cell.Contains('-'));

    /// <summary>Splits a table row into its trimmed cells.</summary>
    /// <param name="line">The table row.</param>
    /// <returns>The trimmed cells.</returns>
    public static string[] Cells(string line) =>
        line.Split('|').Skip(1).SkipLast(1).Select(cell => cell.Trim()).ToArray();

    /// <summary>Strips one pair of backticks from a table cell.</summary>
    /// <param name="cell">The table cell.</param>
    /// <returns>The cell without its backticks.</returns>
    public static string StripCode(string cell)
    {
        cell = cell.Trim();
        return cell.Length >= 2 && cell.StartsWith('`') && cell.EndsWith('`') ? cell[1..^1] : cell;
    }

    /// <summary>The first table holding every named column, with each column's position and its rows; extra columns are ignored.</summary>
    /// <param name="file">The carried page.</param>
    /// <param name="body">The section lines.</param>
    /// <param name="names">The column names the table must hold.</param>
    /// <param name="what">What the table holds, for a failure message.</param>
    /// <returns>The column positions and the rows.</returns>
    public static (IReadOnlyDictionary<string, int> Positions, List<string[]> Rows) NamedTable(
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
    /// <param name="file">The carried page.</param>
    /// <param name="body">The section lines.</param>
    /// <param name="header">The header cells.</param>
    /// <param name="what">What the table holds, for a failure message.</param>
    /// <returns>The rows.</returns>
    public static List<string[]> TableRows(string file, IReadOnlyList<string> body, string[] header, string what)
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
    /// <param name="body">The section lines.</param>
    /// <returns>The paragraphs.</returns>
    public static List<string> Paragraphs(List<string> body)
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
    /// <param name="body">The section lines.</param>
    /// <returns>The first paragraph.</returns>
    public static string FirstParagraph(List<string> body)
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
    /// <param name="type">The record type.</param>
    /// <returns>The display label.</returns>
    public static string TypeLabel(string type)
    {
        var words = type.Replace('-', ' ');
        return char.ToUpperInvariant(words[0]) + words[1..];
    }

    /// <summary>The display plural of a record type: the single product and research stay whole, a consonant before y turns to ies, the rest take s.</summary>
    /// <param name="type">The record type.</param>
    /// <returns>The display plural.</returns>
    public static string TypePlural(string type)
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
    /// <param name="value">The text, or null.</param>
    /// <returns>The JSON node, or null.</returns>
    public static JsonNode? StringOrNull(string? value) => BookRender.StrNode(value);

    /// <summary>The metadata value of a glossary paragraph, without its prefix and closing period.</summary>
    /// <param name="paragraph">The glossary paragraph.</param>
    /// <param name="prefix">The metadata prefix.</param>
    /// <returns>The metadata value.</returns>
    public static string Unfield(string paragraph, string prefix) =>
        paragraph[prefix.Length..].Trim().TrimEnd('.').TrimEnd();

    /// <summary>Null when the metadata value holds nothing.</summary>
    /// <param name="value">The metadata value.</param>
    /// <returns>The value, or null when it holds nothing.</returns>
    public static string? EmptyToNull(string value) => value.Length == 0 ? null : value;
}
