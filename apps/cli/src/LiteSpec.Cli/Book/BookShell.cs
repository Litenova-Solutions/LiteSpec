using System.Text;

namespace LiteSpec.Book;

/// <summary>
///     The book's single shell: the carried header and three columns with the
///     manifest summary as static text for readers without scripting.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
static class BookShell
{
    /// <summary>Renders <c>index.html</c>: the carried shell with its title and no-script summary.</summary>
    /// <param name="title">The page title.</param>
    /// <param name="noscript">The no-script summary HTML.</param>
    /// <returns>The shell HTML.</returns>
    public static string Render(string title, string noscript)
    {
        using var stream = typeof(BookShell).Assembly.GetManifestResourceStream("LiteSpec.Book.Assets.book-shell.html")
            ?? throw new InvalidOperationException("Missing embedded asset 'book-shell.html'.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var shell = reader.ReadToEnd();
        return shell
            .Replace("{{title}}", Escape(title), StringComparison.Ordinal)
            .Replace("{{noscript}}", noscript, StringComparison.Ordinal);
    }

    /// <summary>The static no-script summary: the project title with its total and per-type record counts.</summary>
    /// <param name="projectTitle">The project title.</param>
    /// <param name="totals">The record count per record type.</param>
    /// <returns>The no-script summary HTML.</returns>
    public static string Noscript(string projectTitle, IReadOnlyDictionary<string, int> totals)
    {
        var total = 0;
        foreach (var count in totals.Values)
        {
            total += count;
        }

        var counts = string.Join(
            ", ",
            totals.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{Escape(pair.Key)}: {pair.Value}"));
        var records = total == 1 ? "1 record" : $"{total} records";
        return counts.Length == 0
            ? $"<p>This book needs JavaScript. <b>{Escape(projectTitle)}</b> holds {records}.</p>"
            : $"<p>This book needs JavaScript. <b>{Escape(projectTitle)}</b> holds {records}: {counts}.</p>";
    }

    /// <summary>Escapes one HTML text span, leaving an apostrophe as itself, as the shell has always printed it.</summary>
    /// <param name="text">The text to escape.</param>
    /// <returns>The escaped text.</returns>
    public static string Escape(string text) => HtmlText.EscapeWithoutApostrophe(text);
}
