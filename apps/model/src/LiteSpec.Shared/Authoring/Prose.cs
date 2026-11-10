using System.Text.RegularExpressions;

namespace LiteSpec.Authoring;

/// <summary>
///     The visible-text helpers the validators share: the visible text of a line, its words, and
///     its sentences, which the one-sentence Statement rule counts.
/// </summary>
/// <follows>litespec/core/authoring#write-atomic-standards-provisions</follows>
public static class Prose
{
    // .NET \b reaches Unicode letters, so every word boundary is written out as
    // an explicit ASCII class. Case-insensitive matches are culture-invariant,
    // so the same text splits the same way on every machine. Every pattern below
    // compiles once instead of interpreting on every match.
    /// <summary>The options for word patterns: case-insensitive, culture-invariant, compiled.</summary>
    const RegexOptions WordOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    /// <summary>The options for plain patterns: compiled only.</summary>
    const RegexOptions PlainOptions = RegexOptions.Compiled;

    /// <summary>The pattern of a labeled paragraph prefix.</summary>
    static readonly Regex LabelPrefix = new(@"^\s*\*\*(?:Statement|Deviation|Rationale|Example|Default|Replacement):\*\*\s*", WordOptions);
    /// <summary>The pattern of an inline code span.</summary>
    static readonly Regex InlineCode = new(@"`[^`]*`", PlainOptions);
    /// <summary>The pattern of an image.</summary>
    static readonly Regex Image = new(@"!\[([^\]]*)\]\([^)]+\)", PlainOptions);
    /// <summary>The pattern of a link.</summary>
    static readonly Regex Link = new(@"\[([^\]]+)\]\([^)]+\)", PlainOptions);
    /// <summary>The pattern of an autolink.</summary>
    static readonly Regex Autolink = new(@"<https?:[^>]+>", PlainOptions);
    /// <summary>The pattern of a bare URL.</summary>
    static readonly Regex BareUrl = new(@"https?://\S+", PlainOptions);
    /// <summary>The pattern of an HTML tag.</summary>
    static readonly Regex HtmlTag = new(@"<[^>]+>", PlainOptions);
    /// <summary>The pattern of an emphasis marker.</summary>
    static readonly Regex Emphasis = new(@"[*_~]", PlainOptions);
    /// <summary>The pattern of a heading anchor.</summary>
    static readonly Regex HeadingAnchor = new(@"\{#[^}]+\}", PlainOptions);
    /// <summary>The pattern of a whitespace run.</summary>
    static readonly Regex Whitespace = new(@"\s+", PlainOptions);
    /// <summary>The pattern of one visible word.</summary>
    static readonly Regex WordPattern = new(@"[A-Za-z0-9][A-Za-z0-9@#%+&'./:-]*", PlainOptions);
    /// <summary>The pattern of a title abbreviation dot.</summary>
    static readonly Regex TitleAbbreviation = new(@"(?<![A-Za-z0-9_])(?:Mr|Mrs|Ms|Dr|vs)\.", PlainOptions);
    /// <summary>The pattern of an initial letter dot.</summary>
    static readonly Regex InitialLetter = new(@"(?<![A-Za-z0-9_])[A-Z]\.", PlainOptions);
    /// <summary>The pattern of a dot before a letter or digit.</summary>
    static readonly Regex DotBeforeAlnum = new(@"\.(?=[A-Za-z0-9])", PlainOptions);
    /// <summary>The pattern of a dot inside a word.</summary>
    static readonly Regex DotInsideAlnum = new(@"(?<=[A-Za-z0-9])\.(?=[A-Za-z0-9])", PlainOptions);
    /// <summary>The pattern of one sentence.</summary>
    static readonly Regex SentencePattern = new(@"[^.!?]+[.!?]+|[^.!?]+$", PlainOptions);

    /// <summary>
    ///     Reduces a value to the words a reader sees: inline code, link destinations and URLs each count as one word.
    /// </summary>
    /// <param name="value">The Markdown fragment.</param>
    /// <returns>The visible text.</returns>
    public static string VisibleText(string value)
    {
        value = LabelPrefix.Replace(value, "");
        value = InlineCode.Replace(value, " TOKEN ");
        value = Image.Replace(value, "$1");
        value = Link.Replace(value, "$1");
        value = Autolink.Replace(value, " URL ");
        value = BareUrl.Replace(value, " URL ");
        value = HtmlTag.Replace(value, " ");
        value = Emphasis.Replace(value, "");
        value = HeadingAnchor.Replace(value, "");
        return Whitespace.Replace(value, " ").Trim();
    }

    /// <summary>
    ///     Splits visible text into words.
    /// </summary>
    /// <param name="value">The Markdown fragment.</param>
    /// <returns>The words in order.</returns>
    public static IReadOnlyList<string> Words(string value)
    {
        return WordPattern.Matches(VisibleText(value)).Select(match => match.Value).ToList();
    }

    /// <summary>
    ///     Splits visible text into sentences, ignoring title abbreviations, initials and dots inside words.
    /// </summary>
    /// <param name="value">The Markdown fragment.</param>
    /// <returns>The sentences in order.</returns>
    public static IReadOnlyList<string> Sentences(string value)
    {
        var text = VisibleText(value);
        text = TitleAbbreviation.Replace(text, match => match.Value.Replace(".", ""));
        text = InitialLetter.Replace(text, match => match.Value.Replace(".", ""));
        text = DotBeforeAlnum.Replace(text, "");
        text = DotInsideAlnum.Replace(text, "");
        text = text.Trim();

        if (text.Length == 0) return [];

        return SentencePattern.Matches(text).Select(match => match.Value.Trim()).Where(sentence => sentence.Length > 0).ToList();
    }
}
