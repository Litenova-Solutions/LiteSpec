using System.Text.RegularExpressions;

namespace LiteSpec.Authoring;

/// <summary>
///     The visible-text helpers both validators share.
/// </summary>
/// <follows>litespec/core/authoring#reject-a-recorded-synonym-inside-its-scope</follows>
/// <follows>litespec/core/authoring#state-mannered-terms-literally</follows>
public static partial class Prose
{
    /// <summary>
    ///     Every diagnostic code the language checks report.
    /// </summary>
    public static readonly IReadOnlyList<string> LanguageDiagnosticCodes =
    [
        "GLOSSARY_MANNERED_TERM",
        "GLOSSARY_REJECTED_SYNONYM",
    ];

    // .NET \b reaches Unicode letters, so every word boundary is written out as
    // an explicit ASCII class. Case-insensitive matches are culture-invariant,
    // so the same text reports the same diagnostics on every machine. Every
    // pattern below runs on each page of a large consumer, so each one compiles
    // once instead of interpreting on every match. Compilation changes only the
    // speed, never what matches.
    /// <summary>The options for word patterns: case-insensitive, culture-invariant, compiled.</summary>
    const RegexOptions WordOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    /// <summary>The options for plain patterns: compiled only.</summary>
    const RegexOptions PlainOptions = RegexOptions.Compiled;

    // The page's metadata block: three dashes, one JSON object, three dashes, with
    // an optional leading byte-order mark (U+FEFF). The mark is built from its
    // code point so this file stays plain ASCII.
    /// <summary>The pattern of a page metadata block.</summary>
    static readonly Regex MetadataBlock = new(
        "\\A(?:" + char.ConvertFromUtf32(0xFEFF) + ")?---\\r?\\n\\{[\\s\\S]*?\\r?\\n\\}\\r?\\n---\\r?\\n", PlainOptions);

    // A generated region holds text the model builder wrote, so the measures do not
    // reach inside one. An opener with no closer is left alone: an unclosed region
    // is a builder defect rather than prose a page author can fix.
    /// <summary>The pattern of a paired generated region.</summary>
    static readonly Regex GeneratedRegion = new(@"<!--\s*generated:[^>]*-->[\s\S]*?<!--\s*/generated:[^>]*-->", PlainOptions);

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

    /// <summary>The pattern of a table row line.</summary>
    static readonly Regex TableRow = new(@"^\s*\|", PlainOptions);

    // A column that exists to list rejected words has to be able to contain them.
    // A glossary states `Avoid` beside each term and a module states
    // `Rejected synonyms`, so those cells are the rule rather than a breach of it.
    /// <summary>The pattern of a rejection column heading.</summary>
    static readonly Regex RejectionColumn = new(@"^\s*(?:avoid|rejected|rejected names?|rejected synonyms?|mannered|mannered terms?)\s*$", WordOptions);

    // The column that explains a rejection has to be able to name the word it
    // explains, for the same reason. It is exempt only inside a table that already
    // carries a rejection column, so an ordinary `Reason` column elsewhere stays in
    // scope. Its length is exempt too: a reason states the collision that produced
    // the rejection, and that is longer than a table cell otherwise gets.
    /// <summary>The pattern of a rejection reason column heading.</summary>
    static readonly Regex RejectionReasonColumn = new(@"^\s*(?:reason|reasons|why|rationale)\s*$", WordOptions);

    /// <summary>
    ///     Blanks the metadata block rather than dropping it, keeping every reported line number equal to the file's.
    /// </summary>
    /// <param name="raw">The page text.</param>
    /// <returns>The text with the metadata block replaced by spaces.</returns>
    public static string BlankMetadata(string raw)
    {
        return MetadataBlock.Replace(raw, BlankKeepingLines);
    }

    /// <summary>
    ///     Blanks every generated region rather than dropping it, keeping every reported line number equal to the file's.
    /// </summary>
    /// <param name="raw">The page text.</param>
    /// <returns>The text with generated regions replaced by spaces.</returns>
    public static string BlankGeneratedRegions(string raw)
    {
        return GeneratedRegion.Replace(raw, BlankKeepingLines);
    }

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

    // Blanks a match while keeping its line breaks, so reported line numbers still
    // map to the file.
    /// <summary>Blanks a match while keeping its line breaks.</summary>
    /// <param name="match">The match to blank.</param>
    /// <returns>The blanked text, with line breaks kept.</returns>
    static string BlankKeepingLines(Match match)
    {
        var chars = match.Value.ToCharArray();

        for (var index = 0; index < chars.Length; index++)
        {
            if (chars[index] != '\n') chars[index] = ' ';
        }

        return new string(chars);
    }
}
