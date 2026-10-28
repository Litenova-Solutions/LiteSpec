using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace LiteSpec.Authoring;

/// <summary>
/// One Markdig-based read of a page: headings, labeled paragraphs, fences, links, lists, tables, generated regions, and visible text.
/// </summary>
/// <remarks>
/// Structure comes from the Markdig document. Regular expressions match only words or tokens inside plain text this reader
/// extracted, such as modal words, ASCII checks, bare URLs, anchor suffixes, and the identifier grammar.
/// </remarks>
sealed class PageReader
{
    static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UsePipeTables().UsePreciseSourceLocation().Build();

    static readonly Regex BareUrlToken = new(@"https?://\S+", RegexOptions.Compiled);
    static readonly Regex AnchorToken = new(@"\{#[^}]+\}", RegexOptions.Compiled);
    static readonly Regex EmphasisToken = new(@"[*_~]", RegexOptions.Compiled);
    static readonly Regex WhitespaceToken = new(@"\s+", RegexOptions.Compiled);

    static readonly HashSet<string> StrippedLabels = new(
        ["Statement", "Deviation", "Rationale", "Example", "Default", "Replacement"],
        StringComparer.Ordinal);

    /// <summary>A top-level ATX heading: its level, raw text, display text with code spans blanked, and 1-based line.</summary>
    public sealed record ReaderHeading(int Level, string Text, string DisplayText, int Line);

    /// <summary>A fenced code block: its info string and 1-based first and last lines, fence markers included.</summary>
    public sealed record ReaderFence(string Info, int FirstLine, int LastLine);

    /// <summary>A link or image: its target and 1-based line.</summary>
    public sealed record ReaderLink(string Target, int Line);

    /// <summary>A pipe table row: its cell texts and 1-based line.</summary>
    public sealed record ReaderTableRow(IReadOnlyList<string> Cells, int Line);

    /// <summary>A pipe table: its header cells, header line, and data rows.</summary>
    public sealed record ReaderTable(IReadOnlyList<string> Header, int HeaderLine, IReadOnlyList<ReaderTableRow> Rows);

    /// <summary>The page text.</summary>
    public string Raw { get; }

    /// <summary>The source lines.</summary>
    public string[] Lines { get; }

    /// <summary>The Markdig document.</summary>
    public MarkdownDocument Document { get; }

    /// <summary>Top-level ATX headings in document order, outside generated regions.</summary>
    public IReadOnlyList<ReaderHeading> Headings { get; }

    /// <summary>Labeled top-level paragraphs: 1-based first line to label name, outside generated regions.</summary>
    public IReadOnlyDictionary<int, string> LabelAtLine { get; }

    /// <summary>1-based lines inside fenced code blocks, fence markers included.</summary>
    public IReadOnlySet<int> FencedLines { get; }

    /// <summary>1-based lines inside paired generated regions, comment fences included.</summary>
    public IReadOnlySet<int> GeneratedLines { get; }

    /// <summary>Links and images in document order, outside fenced blocks and generated regions.</summary>
    public IReadOnlyList<ReaderLink> Links { get; }

    /// <summary>Top-level pipe tables in document order, outside generated regions.</summary>
    public IReadOnlyList<ReaderTable> Tables { get; }

    /// <summary>1-based lines of ordered list items with a dot delimiter, sorted, outside generated regions.</summary>
    public IReadOnlyList<int> OrderedStepLines { get; }

    /// <summary>1-based lines of top-level dash bullets starting with a link, sorted, outside generated regions.</summary>
    public IReadOnlyList<int> NavBulletLines { get; }

    /// <summary>Fenced code blocks in document order.</summary>
    public IReadOnlyList<ReaderFence> Fences { get; }

    /// <summary>Anchors the page defines: explicit {#anchors} and heading slugs.</summary>
    public IReadOnlySet<string> Anchors { get; }

    PageReader(
        string raw,
        string[] lines,
        MarkdownDocument document,
        IReadOnlyList<ReaderHeading> headings,
        IReadOnlyDictionary<int, string> labelAtLine,
        IReadOnlySet<int> fencedLines,
        IReadOnlySet<int> generatedLines,
        IReadOnlyList<ReaderLink> links,
        IReadOnlyList<ReaderTable> tables,
        IReadOnlyList<int> orderedStepLines,
        IReadOnlyList<int> navBulletLines,
        IReadOnlyList<ReaderFence> fences,
        IReadOnlySet<string> anchors)
    {
        Raw = raw;
        Lines = lines;
        Document = document;
        Headings = headings;
        LabelAtLine = labelAtLine;
        FencedLines = fencedLines;
        GeneratedLines = generatedLines;
        Links = links;
        Tables = tables;
        OrderedStepLines = orderedStepLines;
        NavBulletLines = navBulletLines;
        Fences = fences;
        Anchors = anchors;
    }

    /// <summary>Parses the page once with Markdig.</summary>
    public static PageReader Parse(string raw)
    {
        var lines = raw.Split(["\r\n", "\n"], StringSplitOptions.None);
        var document = Markdown.Parse(raw, Pipeline);
        var starts = LineStarts(raw);
        int LineForOffset(int offset)
        {
            var found = starts.BinarySearch(offset);
            return found >= 0 ? found + 1 : ~found;
        }

        var headings = new List<ReaderHeading>();
        var fences = new List<ReaderFence>();
        var fenced = new HashSet<int>();
        var links = new List<ReaderLink>();
        var tables = new List<ReaderTable>();
        var orderedSteps = new List<int>();
        var navBullets = new List<int>();
        var labelAtLine = new Dictionary<int, string>();
        var comments = new List<HtmlComment>();

        foreach (var block in document)
        {
            WalkBlock(block, false, false);
        }

        var generated = PairGenerated(comments);

        headings.RemoveAll(heading => generated.Contains(heading.Line));
        links.RemoveAll(link => generated.Contains(link.Line));
        tables.RemoveAll(table => generated.Contains(table.HeaderLine));
        orderedSteps.RemoveAll(line => generated.Contains(line));
        navBullets.RemoveAll(line => generated.Contains(line));
        foreach (var line in labelAtLine.Keys.Where(line => generated.Contains(line)).ToList())
        {
            labelAtLine.Remove(line);
        }

        orderedSteps.Sort();
        navBullets.Sort();

        var anchors = new HashSet<string>(StringComparer.Ordinal);
        foreach (var heading in headings)
        {
            anchors.Add(ExplicitAnchor(heading.Text) ?? Provisions.HeadingSlug(heading.Text));
        }

        return new PageReader(raw, lines, document, headings, labelAtLine, fenced, generated, links, tables, orderedSteps, navBullets, fences, anchors);

        void WalkBlock(Block block, bool inQuote, bool inTable)
        {
            switch (block)
            {
                case HeadingBlock heading when heading.Parent is MarkdownDocument && heading.Column == 0 && !heading.IsSetext:
                    headings.Add(new ReaderHeading(
                        heading.Level,
                        AtxText(raw, heading),
                        HeadingDisplayText(raw, heading),
                        heading.Line + 1));
                    CollectLinks(heading.Inline, heading.Line + 1);
                    break;
                case HeadingBlock heading:
                    CollectLinks(heading.Inline, heading.Line + 1);
                    break;
                case ParagraphBlock paragraph:
                    if (paragraph.Parent is MarkdownDocument && paragraph.Column == 0)
                    {
                        var label = LabelName(paragraph.Inline);
                        if (label is not null)
                        {
                            labelAtLine[paragraph.Line + 1] = label;
                        }
                    }

                    CollectLinks(paragraph.Inline, paragraph.Line + 1);
                    break;
                case FencedCodeBlock fence:
                    var fenceFirst = fence.Line + 1;
                    var fenceLast = fence.Span.IsEmpty ? fenceFirst : LineForOffset(fence.Span.End);
                    if (fenceLast < fenceFirst)
                    {
                        fenceLast = fenceFirst;
                    }

                    for (var line = fenceFirst; line <= fenceLast; line++)
                    {
                        fenced.Add(line);
                    }

                    fences.Add(new ReaderFence((fence.Info ?? "").Trim(), fenceFirst, fenceLast));
                    break;
                case HtmlBlock html:
                    var htmlFirst = html.Line + 1;
                    var htmlLast = html.Span.IsEmpty ? htmlFirst : LineForOffset(html.Span.End);
                    if (htmlLast < htmlFirst)
                    {
                        htmlLast = htmlFirst;
                    }

                    CollectComments(html.Lines.ToString(), htmlFirst, htmlLast);
                    break;
                case ListBlock list:
                    CollectList(list, inQuote, inTable);
                    foreach (var child in list)
                    {
                        WalkBlock(child, inQuote, inTable);
                    }

                    break;
                case Table table:
                    if (table.Parent is MarkdownDocument && table.Column == 0)
                    {
                        tables.Add(ReadTable(table));
                    }

                    foreach (var child in table)
                    {
                        WalkBlock(child, inQuote, true);
                    }

                    break;
                case QuoteBlock quote:
                    foreach (var child in quote)
                    {
                        WalkBlock(child, true, inTable);
                    }

                    break;
                case ContainerBlock container:
                    foreach (var child in container)
                    {
                        WalkBlock(child, inQuote, inTable);
                    }

                    break;
                case LeafBlock leaf:
                    CollectLinks(leaf.Inline, leaf.Line + 1);
                    break;
            }
        }

        void CollectLinks(Inline? inline, int fallbackLine)
        {
            for (var current = inline; current is not null; current = current.NextSibling)
            {
                switch (current)
                {
                    case AutolinkInline autolink when !string.IsNullOrEmpty(autolink.Url):
                        links.Add(new ReaderLink(autolink.Url, InlineLine(autolink, fallbackLine)));
                        break;
                    case LinkInline link when !string.IsNullOrEmpty(link.Url):
                        links.Add(new ReaderLink(link.Url, InlineLine(link, fallbackLine)));
                        CollectLinks(link.FirstChild, fallbackLine);
                        break;
                    case LinkInline link:
                        CollectLinks(link.FirstChild, fallbackLine);
                        break;
                    case HtmlInline htmlInline:
                        var commentLine = InlineLine(htmlInline, fallbackLine);
                        CollectComments(htmlInline.Tag ?? "", commentLine, commentLine);
                        break;
                    case ContainerInline container:
                        CollectLinks(container.FirstChild, fallbackLine);
                        break;
                }
            }
        }

        int InlineLine(Inline inline, int fallbackLine)
        {
            return inline.Span.IsEmpty ? fallbackLine : LineForOffset(inline.Span.Start);
        }

        void CollectList(ListBlock list, bool inQuote, bool inTable)
        {
            if (!inQuote && !inTable && list.IsOrdered && list.OrderedDelimiter == '.')
            {
                foreach (var child in list)
                {
                    if (child is ListItemBlock item)
                    {
                        orderedSteps.Add(item.Line + 1);
                    }
                }
            }

            if (!list.IsOrdered && list.BulletType == '-' && list.Parent is MarkdownDocument && list.Column == 0)
            {
                foreach (var child in list)
                {
                    if (child is ListItemBlock item
                        && item.FirstOrDefault() is ParagraphBlock paragraph
                        && paragraph.Inline is ContainerInline inlines
                        && inlines.FirstChild is LinkInline link
                        && !link.IsImage)
                    {
                        navBullets.Add(item.Line + 1);
                    }
                }
            }
        }

        void CollectComments(string html, int firstLine, int lastLine)
        {
            var search = 0;
            while (true)
            {
                var open = html.IndexOf("<!--", search, StringComparison.Ordinal);
                if (open < 0)
                {
                    return;
                }

                var close = html.IndexOf("-->", open + 4, StringComparison.Ordinal);
                if (close < 0)
                {
                    return;
                }

                var inner = html.Substring(open + 4, close - open - 4).Trim();
                if (!inner.Contains('>'))
                {
                    if (inner.StartsWith("/generated:", StringComparison.Ordinal))
                    {
                        comments.Add(new HtmlComment(false, firstLine, lastLine));
                    }
                    else if (inner.StartsWith("generated:", StringComparison.Ordinal))
                    {
                        comments.Add(new HtmlComment(true, firstLine, lastLine));
                    }
                }

                search = close + 3;
            }
        }
    }

    /// <summary>The visible words of a Markdown fragment: code spans count as one token and link destinations, HTML, and emphasis markers are removed.</summary>
    public static string VisibleText(string fragment)
    {
        var document = Markdown.Parse(fragment, Pipeline);
        var builder = new StringBuilder();
        var first = true;
        foreach (var block in document)
        {
            AppendFragmentBlock(block, builder, ref first);
        }

        var text = builder.ToString();
        text = BareUrlToken.Replace(text, " URL ");
        text = AnchorToken.Replace(text, "");
        text = EmphasisToken.Replace(text, "");
        return WhitespaceToken.Replace(text, " ").Trim();
    }

    /// <summary>Removes a trailing {#anchor} suffix with the spaces before it.</summary>
    public static string StripAnchorSuffix(string text)
    {
        var end = text.Length;
        while (end > 0 && char.IsWhiteSpace(text[end - 1]))
        {
            end--;
        }

        if (end == 0 || text[end - 1] != '}')
        {
            return text;
        }

        var open = text.LastIndexOf("{#", end - 1, StringComparison.Ordinal);
        if (open < 0 || open == 0 || !char.IsWhiteSpace(text[open - 1]))
        {
            return text;
        }

        for (var index = open + 2; index < end - 1; index++)
        {
            if (text[index] == '}')
            {
                return text;
            }
        }

        if (open + 2 >= end - 1)
        {
            return text;
        }

        var cut = open;
        while (cut > 0 && char.IsWhiteSpace(text[cut - 1]))
        {
            cut--;
        }

        return text[..cut];
    }

    /// <summary>The explicit {#anchor} at the end of the text, or null when there is none.</summary>
    public static string? ExplicitAnchor(string text)
    {
        var end = text.Length;
        while (end > 0 && char.IsWhiteSpace(text[end - 1]))
        {
            end--;
        }

        if (end == 0 || text[end - 1] != '}')
        {
            return null;
        }

        var open = text.LastIndexOf("{#", end - 1, StringComparison.Ordinal);
        if (open < 0 || open + 2 >= end - 1)
        {
            return null;
        }

        for (var index = open + 2; index < end - 1; index++)
        {
            if (text[index] == '}')
            {
                return null;
            }
        }

        return text.Substring(open + 2, end - 1 - open - 2);
    }

    /// <summary>Whether the line is an ID line with its stated identifier.</summary>
    public static bool TryParseIdLine(string line, out string stated)
    {
        stated = "";
        const string prefix = "**ID:**";
        if (!line.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var rest = line[prefix.Length..].Trim();
        if (rest.Length < 3 || !rest.StartsWith('`') || !rest.EndsWith('`'))
        {
            return false;
        }

        var inner = rest[1..^1];
        if (inner.Length == 0 || inner.Contains('`'))
        {
            return false;
        }

        stated = inner;
        return true;
    }

    /// <summary>Whether the line is a verification line with its label and evidence.</summary>
    public static bool TryParseVerificationLine(string line, out string label, out string evidence)
    {
        label = "";
        evidence = "";
        foreach (var prefix in new[] { "**Checked by:**", "**Reviewed for:**" })
        {
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                label = prefix == "**Checked by:**" ? "Checked by" : "Reviewed for";
                evidence = line[prefix.Length..].Trim();
                return true;
            }
        }

        return false;
    }

    static string? LabelName(Inline? inline)
    {
        var first = inline is ContainerInline container ? container.FirstChild : inline;
        if (first is not EmphasisInline emphasis || emphasis.DelimiterCount != 2 || emphasis.DelimiterChar != '*')
        {
            return null;
        }

        var inner = InnerText(emphasis);
        if (inner.Length < 2 || !inner.EndsWith(':'))
        {
            return null;
        }

        return inner[..^1];
    }

    static string InnerText(ContainerInline container)
    {
        var builder = new StringBuilder();
        foreach (var child in container)
        {
            switch (child)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    builder.Append(code.Content);
                    break;
                case HtmlEntityInline entity:
                    builder.Append(entity.Original.ToString());
                    break;
                case ContainerInline nested:
                    builder.Append(InnerText(nested));
                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    ///     The heading text as written: the source span without the opening and closing
    ///     number signs. Markdig releases a heading's lines once its inlines are parsed, so
    ///     the text comes from the source.
    /// </summary>
    static string AtxText(string raw, HeadingBlock heading)
    {
        var text = raw.Substring(heading.Span.Start, heading.Span.Length).Trim();
        text = text.TrimStart('#').Trim();
        var closing = text.Length;
        while (closing > 0 && text[closing - 1] == '#')
        {
            closing--;
        }

        return closing < text.Length && (closing == 0 || text[closing - 1] == ' ')
            ? text[..closing].Trim()
            : text;
    }

    static string HeadingDisplayText(string raw, HeadingBlock heading)
    {
        if (heading.Inline is null)
        {
            return AtxText(raw, heading);
        }

        var builder = new StringBuilder();
        AppendDisplay(heading.Inline, builder);
        return WhitespaceToken.Replace(builder.ToString(), " ").Trim();
    }

    static void AppendDisplay(Inline? inline, StringBuilder builder)
    {
        for (var current = inline; current is not null; current = current.NextSibling)
        {
            switch (current)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;
                case CodeInline:
                    builder.Append(' ');
                    break;
                case HtmlInline:
                case LineBreakInline:
                    builder.Append(' ');
                    break;
                case HtmlEntityInline entity:
                    builder.Append(entity.Original.ToString());
                    break;
                case ContainerInline container:
                    AppendDisplay(container.FirstChild, builder);
                    break;
            }
        }
    }

    static void AppendVisible(Inline? inline, StringBuilder builder)
    {
        for (var current = inline; current is not null; current = current.NextSibling)
        {
            switch (current)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;
                case CodeInline:
                    builder.Append(" TOKEN ");
                    break;
                case AutolinkInline:
                    builder.Append(" URL ");
                    break;
                case LinkInline link when link.IsAutoLink:
                    builder.Append(" URL ");
                    break;
                case LinkInline link:
                    AppendVisible(link.FirstChild, builder);
                    break;
                case HtmlInline:
                case LineBreakInline:
                    builder.Append(' ');
                    break;
                case HtmlEntityInline entity:
                    builder.Append(entity.Original.ToString());
                    break;
                case ContainerInline container:
                    AppendVisible(container.FirstChild, builder);
                    break;
            }
        }
    }

    static void AppendFragmentBlock(Block block, StringBuilder builder, ref bool first)
    {
        switch (block)
        {
            case ParagraphBlock paragraph when first && LabelName(paragraph.Inline) is string label && StrippedLabels.Contains(label):
                if (paragraph.Inline is ContainerInline container && container.FirstChild?.NextSibling is Inline rest)
                {
                    AppendVisible(rest, builder);
                }

                builder.Append(' ');
                first = false;
                break;
            case LeafBlock leaf when leaf.Inline is not null:
                AppendVisible(leaf.Inline, builder);
                builder.Append(' ');
                first = false;
                break;
            case HtmlBlock:
                builder.Append(' ');
                first = false;
                break;
            case ContainerBlock nested:
                foreach (var child in nested)
                {
                    AppendFragmentBlock(child, builder, ref first);
                }

                break;
            default:
                first = false;
                break;
        }
    }

    static ReaderTable ReadTable(Table table)
    {
        var header = new List<string>();
        var rows = new List<ReaderTableRow>();
        var headerLine = table.Line + 1;
        foreach (var child in table)
        {
            if (child is not TableRow row)
            {
                continue;
            }

            var cells = new List<string>();
            foreach (var cell in row)
            {
                if (cell is TableCell tableCell)
                {
                    cells.Add(CellText(tableCell));
                }
            }

            if (row.IsHeader && header.Count == 0)
            {
                header.AddRange(cells);
                headerLine = row.Line + 1;
            }
            else if (!row.IsHeader)
            {
                rows.Add(new ReaderTableRow(cells, row.Line + 1));
            }
        }

        return new ReaderTable(header, headerLine, rows);
    }

    static string CellText(TableCell cell)
    {
        var builder = new StringBuilder();
        foreach (var block in cell)
        {
            if (block is LeafBlock leaf && leaf.Inline is not null)
            {
                AppendVisible(leaf.Inline, builder);
                builder.Append(' ');
            }
        }

        var text = builder.ToString();
        text = BareUrlToken.Replace(text, " URL ");
        text = AnchorToken.Replace(text, "");
        text = EmphasisToken.Replace(text, "");
        return WhitespaceToken.Replace(text, " ").Trim();
    }

    static List<int> LineStarts(string raw)
    {
        var starts = new List<int> { 0 };
        for (var offset = 0; offset < raw.Length; offset++)
        {
            if (raw[offset] == '\n')
            {
                starts.Add(offset + 1);
            }
        }

        return starts;
    }

    sealed record HtmlComment(bool Opener, int FirstLine, int LastLine);

    static HashSet<int> PairGenerated(List<HtmlComment> comments)
    {
        var generated = new HashSet<int>();
        HtmlComment? pending = null;
        foreach (var comment in comments)
        {
            if (comment.Opener)
            {
                pending ??= comment;
            }
            else if (pending is not null)
            {
                for (var line = pending.FirstLine; line <= comment.LastLine; line++)
                {
                    generated.Add(line);
                }

                pending = null;
            }
        }

        return generated;
    }
}
