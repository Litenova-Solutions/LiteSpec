using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteSpec.Book;

/// <summary>
///     A small Markdown renderer for record bodies: headings, paragraphs, lists,
///     tables, code blocks, quotes and inline spans. It escapes everything it does
///     not generate, so a body can never inject markup.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookMarkdown
{
    /// <summary>The pattern of one inline code span delimited by backticks.</summary>
    static readonly Regex CodeSpan = new("`([^`\n]+)`", RegexOptions.Compiled);
    /// <summary>The pattern of one Markdown link with its label and destination.</summary>
    static readonly Regex MdLink = new(@"\[([^\]\n]+)\]\(([^)\s]+)\)", RegexOptions.Compiled);
    /// <summary>The pattern of one bare HTTP or HTTPS address in running text.</summary>
    static readonly Regex RawUrl = new(@"(?<![""'=])\bhttps?://[^\s<]+", RegexOptions.Compiled);
    /// <summary>The pattern of one bold span delimited by double asterisks.</summary>
    static readonly Regex Bold = new(@"\*\*([^*\n]+)\*\*", RegexOptions.Compiled);
    /// <summary>The pattern of one italic span delimited by single asterisks.</summary>
    static readonly Regex Italic = new(@"\*([^*\n]+)\*", RegexOptions.Compiled);
    /// <summary>The pattern of one record identifier with an optional fragment, covering every record type prefix.</summary>
    static readonly Regex RecordId = new(
        @"\b((?:product|goal|problem|feature|constraint|research|decision|topic|requirement|journey|term|module|aggregate|rule|event|integration-event|reaction|job|notification|setting|use-case|result|scenario|actor|role|permission|entry-point|external-system|runbook|exception)/[A-Za-z0-9][A-Za-z0-9._-]*(?:#[A-Za-z0-9_-]+)?)",
        RegexOptions.Compiled);
    /// <summary>The pattern of one heading line with its level marks and text.</summary>
    static readonly Regex Heading = new(@"^(#{1,6})\s+(.*)$", RegexOptions.Compiled);
    /// <summary>The pattern of one horizontal rule line.</summary>
    static readonly Regex Rule = new(@"^\s*(?:---|\*\*\*|___)\s*$", RegexOptions.Compiled);
    /// <summary>The pattern of one list item line with its indentation and text.</summary>
    static readonly Regex ListItem = new(@"^(\s*)(?:[-*+]|\d+[.)])\s+(.*)$", RegexOptions.Compiled);
    /// <summary>The pattern of one table delimiter line that separates the header from the rows.</summary>
    static readonly Regex TableDelimiter = new(@"^\s*\|?\s*:?-{1,}:?\s*(\|\s*:?-{1,}:?\s*)*\|?\s*$", RegexOptions.Compiled);

    /// <summary>Renders Markdown to HTML fragments. Record ids link through <paramref name="linkRecord"/>.</summary>
    /// <param name="markdown">The Markdown text of one record body.</param>
    /// <param name="linkRecord">The resolver that maps a record identifier to its link target and title, or null to leave identifiers unlinked.</param>
    /// <returns>The rendered HTML fragment with trailing blank lines removed.</returns>
    public static string Render(string markdown, Func<string, (string Href, string Title)?>? linkRecord = null)
    {
        var lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var html = new StringBuilder();
        var paragraph = new List<string>();
        var index = 0;

        void Flush()
        {
            if (paragraph.Count == 0)
            {
                return;
            }

            html.Append("<p>");
            html.Append(Inline(string.Join(" ", paragraph), linkRecord));
            html.AppendLine("</p>");
            paragraph.Clear();
        }

        while (index < lines.Length)
        {
            var line = lines[index];
            if (line.Trim().Length == 0)
            {
                Flush();
                index += 1;
                continue;
            }

            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                Flush();
                var code = new List<string>();
                index += 1;
                while (index < lines.Length && !lines[index].TrimStart().StartsWith("```", StringComparison.Ordinal))
                {
                    code.Add(lines[index]);
                    index += 1;
                }

                index += 1;
                html.Append("<pre class=\"codeblock\"><code>");
                html.Append(BookRender.Esc(string.Join("\n", code)));
                html.AppendLine("</code></pre>");
                continue;
            }

            var heading = Heading.Match(line);
            if (heading.Success)
            {
                Flush();
                var level = heading.Groups[1].Value.Length switch
                {
                    <= 2 => 3,
                    3 => 4,
                    4 => 5,
                    _ => 6,
                };
                var text = heading.Groups[2].Value.Trim();
                var slug = Slug(text);
                html.Append(CultureInfo.InvariantCulture, $"<h{level} id=\"body-{(slug.Length > 0 ? slug : "section")}\">");
                html.Append(Inline(text, linkRecord));
                html.AppendLine(CultureInfo.InvariantCulture, $"</h{level}>");
                index += 1;
                continue;
            }

            if (Rule.IsMatch(line))
            {
                Flush();
                html.AppendLine("<hr>");
                index += 1;
                continue;
            }

            if (line.TrimStart().StartsWith('>'))
            {
                Flush();
                var quote = new List<string>();
                while (index < lines.Length && lines[index].TrimStart().StartsWith('>'))
                {
                    quote.Add(lines[index].TrimStart()[1..].TrimStart());
                    index += 1;
                }

                html.Append("<blockquote><p>");
                html.Append(Inline(string.Join(" ", quote.Where(part => part.Length > 0)), linkRecord));
                html.AppendLine("</p></blockquote>");
                continue;
            }

            if (line.Contains('|') && index + 1 < lines.Length && TableDelimiter.IsMatch(lines[index + 1]))
            {
                Flush();
                html.Append(RenderTable(lines, ref index, linkRecord));
                continue;
            }

            if (ListItem.IsMatch(line))
            {
                Flush();
                html.Append(RenderList(lines, ref index, linkRecord));
                continue;
            }

            paragraph.Add(line.Trim());
            index += 1;
        }

        Flush();
        return html.ToString().TrimEnd();
    }

    /// <summary>Renders one table starting at the current line and advances the line index past its rows.</summary>
    /// <param name="lines">The body lines that hold the table.</param>
    /// <param name="index">The current line index at the table header; on return, the index of the first line after the table.</param>
    /// <param name="linkRecord">The resolver that maps a record identifier to its link target and title, or null to leave identifiers unlinked.</param>
    /// <returns>The rendered table HTML.</returns>
    static string RenderTable(string[] lines, ref int index, Func<string, (string Href, string Title)?>? linkRecord)
    {
        var head = Cells(lines[index]);
        var aligns = Cells(lines[index + 1]).Select(cell =>
        {
            var trimmed = cell.Trim();
            return trimmed.StartsWith(':') && trimmed.EndsWith(':') ? "mid"
                : trimmed.EndsWith(':') ? "num"
                : "";
        }).ToList();
        index += 2;
        var rows = new List<IReadOnlyList<string>>();
        while (index < lines.Length && lines[index].Contains('|') && lines[index].Trim().Length > 0)
        {
            rows.Add(Cells(lines[index]));
            index += 1;
        }

        var html = new StringBuilder();
        html.Append("<div class=\"tablewrap\"><table class=\"tbl\"><thead><tr>");
        for (var column = 0; column < head.Count; column++)
        {
            var align = column < aligns.Count && aligns[column].Length > 0 ? $" class=\"{aligns[column]}\"" : "";
            html.Append(CultureInfo.InvariantCulture, $"<th{align}>{Inline(head[column], linkRecord)}</th>");
        }

        html.Append("</tr></thead><tbody>");
        foreach (var row in rows)
        {
            html.Append("<tr>");
            for (var column = 0; column < head.Count; column++)
            {
                var align = column < aligns.Count && aligns[column].Length > 0 ? $" class=\"{aligns[column]}\"" : "";
                var cell = column < row.Count ? row[column] : "";
                html.Append(CultureInfo.InvariantCulture, $"<td{align}>{Inline(cell, linkRecord)}</td>");
            }

            html.Append("</tr>");
        }

        html.AppendLine("</tbody></table></div>");
        return html.ToString();
    }

    /// <summary>Splits one table line into its trimmed cells.</summary>
    /// <param name="line">The table line with pipe separators.</param>
    /// <returns>The trimmed cell texts, or an empty list when the line holds one empty cell.</returns>
    static List<string> Cells(string line)
    {
        var cells = line.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToList();
        return cells.Count == 1 && cells[0].Length == 0 ? [] : cells;
    }

    /// <summary>Renders one list starting at the current line and advances the line index past its items.</summary>
    /// <param name="lines">The body lines that hold the list.</param>
    /// <param name="index">The current line index at the first item; on return, the index of the first line after the list.</param>
    /// <param name="linkRecord">The resolver that maps a record identifier to its link target and title, or null to leave identifiers unlinked.</param>
    /// <returns>The rendered list HTML, ordered when the first item carries a number.</returns>
    static string RenderList(string[] lines, ref int index, Func<string, (string Href, string Title)?>? linkRecord)
    {
        var ordered = char.IsDigit(lines[index].TrimStart()[0]);
        var items = new List<string>();
        while (index < lines.Length)
        {
            var match = ListItem.Match(lines[index]);
            if (match.Success)
            {
                items.Add(match.Groups[2].Value.Trim());
                index += 1;
                continue;
            }

            if (lines[index].Trim().Length > 0 && lines[index].StartsWith(' ') && items.Count > 0)
            {
                items[^1] += " " + lines[index].Trim();
                index += 1;
                continue;
            }

            break;
        }

        var tag = ordered ? "ol" : "ul";
        var html = new StringBuilder();
        html.Append(CultureInfo.InvariantCulture, $"<{tag} class=\"plain\">");
        foreach (var item in items)
        {
            html.Append(CultureInfo.InvariantCulture, $"<li>{Inline(item, linkRecord)}</li>");
        }

        html.AppendLine(CultureInfo.InvariantCulture, $"</{tag}>");
        return html.ToString();
    }

    /// <summary>Renders the inline spans of one text run to HTML, escaping the text and linking record identifiers.</summary>
    /// <param name="text">The raw text run to render.</param>
    /// <param name="linkRecord">The resolver that maps a record identifier to its link target and title, or null to leave identifiers unlinked.</param>
    /// <returns>The rendered HTML for the text run.</returns>
    static string Inline(string text, Func<string, (string Href, string Title)?>? linkRecord)
    {
        var escaped = BookRender.Esc(text);
        var codes = new List<string>();
        escaped = CodeSpan.Replace(escaped, match =>
        {
            codes.Add($"<code>{match.Groups[1].Value}</code>");
            return $"\u0000C{codes.Count - 1}\u0000";
        });

        var links = new List<string>();
        escaped = MdLink.Replace(escaped, match =>
        {
            var label = Parts(match.Groups[1].Value);
            var href = match.Groups[2].Value;
            links.Add(BookRender.IsSafeHref(href) ? $"<a href=\"{href}\">{label}</a>" : label);
            return $"\u0000L{links.Count - 1}\u0000";
        });
        escaped = RawUrl.Replace(escaped, match =>
        {
            var url = match.Value.TrimEnd('.', ',', ';', ':', '!', '?', ')');
            var tail = match.Value[url.Length..];
            links.Add(BookRender.IsSafeHref(url) ? $"<a href=\"{url}\">{url}</a>{tail}" : match.Value);
            return $"\u0000L{links.Count - 1}\u0000";
        });

        escaped = Bold.Replace(escaped, "<strong>$1</strong>");
        escaped = Italic.Replace(escaped, "<em>$1</em>");

        if (linkRecord is not null)
        {
            escaped = RecordId.Replace(escaped, match =>
            {
                var target = linkRecord(match.Groups[1].Value);
                if (target is null || !BookRender.IsSafeHref(target.Value.Href))
                {
                    return match.Value;
                }

                return $"<a href=\"{target.Value.Href}\"><span class=\"reviewer-only\">{BookRender.Esc(target.Value.Title)}</span>"
                    + $"<span class=\"dev-only mono\">{match.Value}</span></a>";
            });
        }

        for (var position = 0; position < links.Count; position++)
        {
            escaped = escaped.Replace($"\u0000L{position}\u0000", links[position], StringComparison.Ordinal);
        }

        for (var position = 0; position < codes.Count; position++)
        {
            escaped = escaped.Replace($"\u0000C{position}\u0000", codes[position], StringComparison.Ordinal);
        }

        return escaped;
    }

    /// <summary>Renders the bold and italic spans of one link label.</summary>
    /// <param name="text">The raw link label text.</param>
    /// <returns>The label with bold and italic spans rendered as HTML.</returns>
    static string Parts(string text)
    {
        text = Bold.Replace(text, "<strong>$1</strong>");
        return Italic.Replace(text, "<em>$1</em>");
    }

    /// <summary>Derives the anchor slug of one heading text.</summary>
    /// <param name="text">The heading text to convert.</param>
    /// <returns>The lowercase slug with spaces, hyphens and underscores as single hyphens and edge hyphens removed, possibly empty.</returns>
    static string Slug(string text)
    {
        var built = new StringBuilder();
        foreach (var ch in text.ToLowerInvariant())
        {
            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                built.Append(ch);
            }
            else if (ch is ' ' or '-' or '_')
            {
                built.Append('-');
            }
        }

        var slug = built.ToString();
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }
}
