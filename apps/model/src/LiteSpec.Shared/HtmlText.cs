using System.Text;

namespace LiteSpec;

/// <summary>
///     Escapes text for HTML: the one escaping every HTML writer shares, so a value
///     reads the same wherever it lands in a page.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
public static class HtmlText
{
    /// <summary>
    ///     Escapes the five HTML metacharacters in a text span: <c>&amp;</c>, <c>&lt;</c>,
    ///     <c>&gt;</c>, <c>&quot;</c> and <c>&amp;#39;</c> for an apostrophe. A missing
    ///     value escapes to the empty string.
    /// </summary>
    /// <param name="value">The text to escape, or null.</param>
    /// <returns>The escaped text.</returns>
    public static string Escape(string? value) => Escape(value, escapeApostrophe: true);

    /// <summary>
    ///     Escapes the HTML metacharacters in a text span but leaves an apostrophe as
    ///     itself: <c>&amp;</c>, <c>&lt;</c>, <c>&gt;</c> and <c>&quot;</c> only. A missing
    ///     value escapes to the empty string.
    /// </summary>
    /// <param name="value">The text to escape, or null.</param>
    /// <returns>The escaped text.</returns>
    public static string EscapeWithoutApostrophe(string? value) => Escape(value, escapeApostrophe: false);

    /// <summary>Escapes one text span, escaping an apostrophe only when <paramref name="escapeApostrophe"/> asks for it.</summary>
    /// <param name="value">The text to escape, or null.</param>
    /// <param name="escapeApostrophe">Whether an apostrophe escapes to <c>&amp;#39;</c>.</param>
    /// <returns>The escaped text.</returns>
    static string Escape(string? value, bool escapeApostrophe)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var built = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            switch (character)
            {
                case '&': built.Append("&amp;"); break;
                case '<': built.Append("&lt;"); break;
                case '>': built.Append("&gt;"); break;
                case '"': built.Append("&quot;"); break;
                case '\'' when escapeApostrophe: built.Append("&#39;"); break;
                default: built.Append(character); break;
            }
        }

        return built.ToString();
    }
}
