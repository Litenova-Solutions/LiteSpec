using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace LiteSpec.Model;

/// <summary>
///     Identifier shapes and citation matching.
/// </summary>
/// <follows>litespec/backend/identifiers#follow-the-identifier-form-of-each-record-type</follows>
/// <follows>litespec/backend/identifiers#name-records-with-one-to-six-words</follows>
/// <follows>litespec/backend/identifiers#resolve-every-identifier-citation</follows>
public static class Identifiers
{
    /// <summary>A citation of a project record.</summary>
    public static readonly Regex Citation = BuildCitation();

    private static readonly Regex SharedCitationRegex = new Regex(
        @"\blitespec/[a-z0-9]+(?:-[a-z0-9]+)*(?:/[a-z0-9]+(?:-[a-z0-9]+)*)*#[a-z0-9]+(?:-[a-z0-9]+)*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex EventIdRegex = new Regex(
        @"^event/[a-z0-9]+(?:-[a-z0-9]+)*\.[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RefusalCodeRegex = new Regex(
        @"\b[a-z0-9]+(?:-[a-z0-9]+)*\.[a-z0-9]+(?:-[a-z0-9]+)*\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ImplementsTagRegex = new Regex(
        @"<implements>\s*(?<id>use-case/[A-Za-z0-9][A-Za-z0-9._-]*)\s*</implements>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ProvesTagRegex = new Regex(
        @"<proves>\s*(?<id>(?:scenario|rule)/[A-Za-z0-9][A-Za-z0-9._-]*)\s*</proves>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ProvisionHeadingRegex = new Regex(
        @"^###\s+(?<heading>.+?)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>A citation of a shared standards provision: litespec/&lt;page&gt;#&lt;slug&gt;.</summary>
    public static Regex SharedCitation() => SharedCitationRegex;

    /// <summary>A whole event record id, such as <c>event/expense.submitted</c>.</summary>
    public static Regex EventId() => EventIdRegex;

    /// <summary>A refusal code in identifier form: module and name in kebab-case.</summary>
    public static Regex RefusalCode() => RefusalCodeRegex;

    /// <summary>A written anchor on a reaction or job.</summary>
    public static Regex ImplementsTag() => ImplementsTagRegex;

    /// <summary>A test's claim of the scenario or rule it proves.</summary>
    public static Regex ProvesTag() => ProvesTagRegex;

    /// <summary>A shared provision carried as a heading, which holds no ID text.</summary>
    public static Regex ProvisionHeading() => ProvisionHeadingRegex;

    private static Regex BuildCitation()
    {
        var kinds = string.Join(
            "|",
            RecordTypes.All.OrderBy(kind => kind, StringComparer.Ordinal).Select(Regex.Escape));

        return new Regex(
            $@"(?<![A-Za-z0-9_./-])(?:{kinds})/[A-Za-z0-9][A-Za-z0-9._-]*",
            RegexOptions.Compiled);
    }

    /// <summary>
    ///     Splits an identifier into its prefix, home and name.
    /// </summary>
    public static bool TrySplit(string id, out string prefix, out string? home, out string name)
    {
        prefix = "";
        home = null;
        name = "";

        var slash = id.IndexOf('/');

        if (slash <= 0 || slash == id.Length - 1)
        {
            return false;
        }

        prefix = id.Substring(0, slash);

        var rest = id.Substring(slash + 1);
        var dot = rest.IndexOf('.');

        if (dot < 0)
        {
            name = rest;
            return true;
        }

        home = rest.Substring(0, dot);
        name = rest.Substring(dot + 1);

        return home.Length > 0 && name.Length > 0 && name.IndexOf('.') < 0;
    }

    /// <summary>
    ///     Whether the name follows the name rules: one to six lowercase words,
    ///     digits allowed inside a word or as a word, never as the whole name.
    ///     A decision name opens with its four-digit number.
    /// </summary>
    public static bool ValidName(string name, bool decision)
    {
        var words = name.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length < 1 || words.Length > 6 || string.Join("-", words) != name)
        {
            return false;
        }

        if (decision)
        {
            if (words.Length < 2 || words[0].Length != 4 || !AllDigits(words[0]))
            {
                return false;
            }
        }

        foreach (var word in words)
        {
            if (word.Length == 0 || !AsciiWord(word))
            {
                return false;
            }
        }

        if (!decision)
        {
            return true;
        }

        for (var index = 1; index < words.Length; index++)
        {
            if (!AllDigits(words[index]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the whole name is digits, which no name may be.</summary>
    public static bool AllDigitsName(string name) =>
        name.Length > 0 && name.Replace("-", "").All(char.IsDigit);

    private static bool AllDigits(string word) =>
        word.Length > 0 && word.All(char.IsDigit);

    private static bool AsciiWord(string word)
    {
        foreach (var character in word)
        {
            if (!AsciiLetterOrDigit(character) || char.IsUpper(character))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AsciiLetterOrDigit(char character) =>
        (character >= 'a' && character <= 'z')
        || (character >= 'A' && character <= 'Z')
        || (character >= '0' && character <= '9');

    /// <summary>
    ///     Splits a journey step link of the form journey/name#step into the journey id and the step.
    /// </summary>
    public static bool TrySplitStep(string id, out string journey, out string step)
    {
        journey = "";
        step = "";

        var hash = id.IndexOf('#');

        if (hash <= 0 || hash == id.Length - 1)
        {
            return false;
        }

        journey = id.Substring(0, hash);
        step = id.Substring(hash + 1);

        return journey.StartsWith("journey/", StringComparison.Ordinal) && step.Length > 0;
    }

    /// <summary>
    ///     The link without its number-sign fragment: a journey step resolves through its
    ///     journey, and an external-system operation through its system.
    /// </summary>
    public static string BaseId(string id)
    {
        var hash = id.IndexOf('#');

        return hash < 0 ? id : id.Substring(0, hash);
    }

    /// <summary>
    ///     Trims prose punctuation from a matched citation.
    /// </summary>
    public static string TrimCitation(string match) =>
        match.TrimEnd('.', ',', ';', ':', '!', '?', ')', ']', '"', '\'', '`');
}
