using System.Text.RegularExpressions;
using LiteSpec.Standards;

namespace LiteSpec.Maintain.Validate;

/// <summary>
///     The prose lints: the paragraph-length and banned-phrase scans over standards pages.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static partial class ProseLints
{
    /// <summary>The most words one prose paragraph may hold.</summary>
    public const int MaxParagraphWords = 120;

    // The style card bans LiteSpec-ese names for record homes and marketing
    // intensifiers. The list holds the literal phrases; the holds-the-values
    // shape is a regex beside them. Matching is case-insensitive.
    /// <summary>Gets the literal banned phrases, matched case-insensitively as whole words.</summary>
    public static IReadOnlyList<string> BannedPhrases { get; } =
    [
        "gains a home",
        "refuses other folders",
        "names no ",
        "the homes of records",
        "seamlessly",
        "instantly",
        "effortlessly",
        "unlock the power",
        "leverage",
        "robust",
        "cutting-edge",
        "game-changer",
        "best-in-class",
        "world-class",
        "supercharge",
    ];

    /// <summary>A prose paragraph over the word limit: its 1-based first line and its word count.</summary>
    internal sealed record LongParagraph(int FirstLine, int WordCount);

    /// <summary>A banned phrase hit: its 1-based line and the phrase that matched.</summary>
    internal sealed record BannedHit(int Line, string Phrase);

    /// <summary>Reports whether a repository-relative path carries prose the lints read.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <returns>True when the lints read the path.</returns>
    public static bool IsProseTarget(string relative) =>
        relative.Equals("README.md", StringComparison.Ordinal)
        || relative.Equals("WRITING.md", StringComparison.Ordinal)
        || relative.Equals("CONTRIBUTING.md", StringComparison.Ordinal)
        || (relative.StartsWith("docs/", StringComparison.Ordinal) && relative.EndsWith(".md", StringComparison.OrdinalIgnoreCase));

    /// <summary>Finds the prose paragraphs over the word limit in document order.</summary>
    /// <param name="reader">The parsed page.</param>
    /// <returns>The long paragraphs.</returns>
    public static IReadOnlyList<LongParagraph> FindLongParagraphs(PageReader reader)
    {
        var excluded = StructuralLines(reader);
        var result = new List<LongParagraph>();
        var run = new List<string>();
        var first = 0;
        for (var index = 0; index < reader.Lines.Length; index++)
        {
            var number = index + 1;
            if (IsParagraphLine(reader.Lines[index], number, excluded))
            {
                if (run.Count == 0)
                {
                    first = number;
                }

                run.Add(reader.Lines[index]);
                continue;
            }

            FlushRun(result, run, first);
        }

        FlushRun(result, run, first);
        return result;
    }

    /// <summary>Finds the banned phrases outside code spans, tables, block quotes and provision label lines. A block quote quotes other text, such as the before side of a rewrite example.</summary>
    /// <param name="reader">The parsed page.</param>
    /// <returns>The banned phrase hits in document order.</returns>
    public static IReadOnlyList<BannedHit> FindBannedPhrases(PageReader reader)
    {
        var excluded = StructuralLines(reader);
        var result = new List<BannedHit>();
        for (var index = 0; index < reader.Lines.Length; index++)
        {
            var number = index + 1;
            var line = reader.Lines[index];
            if (excluded.Contains(number) || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var trimmed = line.TrimStart();
            if (trimmed.StartsWith('|') || trimmed.StartsWith('>') || IsLabelLine(trimmed))
            {
                continue;
            }

            // A code span holds a name, not prose, so it is blanked to spaces:
            // the offsets stay aligned with the page lines.
            var masked = CodeSpanPattern().Replace(line, match => new string(' ', match.Length));
            foreach (var (phrase, pattern) in PhrasePatterns)
            {
                foreach (Match match in pattern.Matches(masked))
                {
                    result.Add(new BannedHit(number, phrase));
                }
            }

            for (var holds = HoldsValuesSpeaksPattern().Match(masked); holds.Success; holds = holds.NextMatch())
            {
                result.Add(new BannedHit(number, holds.Value.Trim()));
            }
        }

        return result;
    }

    // A paragraph is a run of non-blank lines outside fenced code, tables,
    // headings, list items, block quotes and HTML comments. Generated regions
    // hold tool output, not authored prose, so they break runs like fences.
    // Table header and row lines come from the parsed tables; a pipe-leading
    // line is also structural, so a delimiter the parser skips still breaks.
    /// <summary>Lists the 1-based lines that never belong to a paragraph or carry a phrase.</summary>
    /// <param name="reader">The parsed page.</param>
    /// <returns>The structural line numbers.</returns>
    static HashSet<int> StructuralLines(PageReader reader)
    {
        var lines = new HashSet<int>(reader.FencedLines);
        lines.UnionWith(reader.GeneratedLines);
        foreach (var table in reader.Tables)
        {
            lines.Add(table.HeaderLine);
            foreach (var row in table.Rows)
            {
                lines.Add(row.Line);
            }
        }

        foreach (var heading in reader.Headings)
        {
            lines.Add(heading.Line);
        }

        return lines;
    }

    /// <summary>Reports whether a line can belong to a prose paragraph.</summary>
    /// <param name="line">The line to check.</param>
    /// <param name="number">The 1-based line number.</param>
    /// <param name="excluded">The structural line numbers.</param>
    /// <returns>True when the line can belong to a paragraph.</returns>
    static bool IsParagraphLine(string line, int number, HashSet<int> excluded)
    {
        if (excluded.Contains(number) || string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        return !ListItemPattern().IsMatch(line)
            && !QuotePattern().IsMatch(line)
            && !HeadingPattern().IsMatch(line)
            && !ThematicBreakPattern().IsMatch(line)
            && !CommentPattern().IsMatch(line)
            && !line.TrimStart().StartsWith('|')
            && !IsLabelLine(line.TrimStart());
    }

    // Provision label lines are structured fields, not prose paragraphs: a
    // verification line stays one line however many conditions it lists, and the
    // finding conditions quote check wording, so both scans skip them.
    /// <summary>Reports whether a trimmed line opens a skipped provision label.</summary>
    /// <param name="trimmed">The trimmed line.</param>
    /// <returns>True when the line opens a provision label.</returns>
    static bool IsLabelLine(string trimmed) =>
        trimmed.StartsWith("**ID:**", StringComparison.Ordinal)
        || trimmed.StartsWith("**Strength:**", StringComparison.Ordinal)
        || trimmed.StartsWith("**Applies to:**", StringComparison.Ordinal)
        || trimmed.StartsWith("**Statement:**", StringComparison.Ordinal)
        || trimmed.StartsWith("**Checked by:**", StringComparison.Ordinal)
        || trimmed.StartsWith("**Reviewed for:**", StringComparison.Ordinal);

    /// <summary>Adds a finished paragraph run to the results when it exceeds the word limit.</summary>
    /// <param name="result">The long paragraphs.</param>
    /// <param name="run">The finished run.</param>
    /// <param name="first">The 1-based first line of the run.</param>
    static void FlushRun(List<LongParagraph> result, List<string> run, int first)
    {
        if (run.Count == 0)
        {
            return;
        }

        var count = CountWords(string.Join(" ", run));
        if (count > MaxParagraphWords)
        {
            result.Add(new LongParagraph(first, count));
        }

        run.Clear();
    }

    // Code spans hold names, not prose, and link targets repeat the address
    // the link text already names, so neither counts toward the paragraph.
    /// <summary>Counts the words of a paragraph after removing code spans and link targets.</summary>
    /// <param name="text">The paragraph text.</param>
    /// <returns>The word count.</returns>
    static int CountWords(string text)
    {
        var cleaned = CodeSpanPattern().Replace(text, " ");
        cleaned = InlineLinkPattern().Replace(cleaned, "$1");
        cleaned = BareUrlPattern().Replace(cleaned, " ");
        return cleaned.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    // Each literal matches as a whole word so a longer word that merely starts
    // the same way never matches. The trailing-space phrase keeps its space as
    // the boundary instead.
    /// <summary>The literal phrases with their compiled whole-word patterns.</summary>
    static readonly (string Phrase, Regex Pattern)[] PhrasePatterns = BannedPhrases
        .Select(phrase => (phrase, new Regex(
            phrase.EndsWith(' ') ? "\\b" + Regex.Escape(phrase) : "\\b" + Regex.Escape(phrase) + "\\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)))
        .ToArray();

    /// <summary>Gets the pattern that matches the banned holds-the-values phrase.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\bholds the values.*speaks\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HoldsValuesSpeaksPattern();

    /// <summary>Gets the pattern that matches an inline code span.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex("`[^`]*`")]
    private static partial Regex CodeSpanPattern();

    /// <summary>Gets the pattern that matches an inline link, capturing its text.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex InlineLinkPattern();

    /// <summary>Gets the pattern that matches a bare URL.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"https?://\S+")]
    private static partial Regex BareUrlPattern();

    /// <summary>Gets the pattern that matches a list item line.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^\s*(?:[-*+]|\d{1,9}[.)])\s+")]
    private static partial Regex ListItemPattern();

    /// <summary>Gets the pattern that matches a block quote line.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^\s*>")]
    private static partial Regex QuotePattern();

    /// <summary>Gets the pattern that matches an ATX heading line.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^\s*#{1,6}(?:\s|$)")]
    private static partial Regex HeadingPattern();

    /// <summary>Gets the pattern that matches a thematic break line.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^\s*(?:-{3,}|\*{3,}|_{3,})\s*$")]
    private static partial Regex ThematicBreakPattern();

    /// <summary>Gets the pattern that matches an HTML comment line.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^\s*<!--")]
    private static partial Regex CommentPattern();
}
