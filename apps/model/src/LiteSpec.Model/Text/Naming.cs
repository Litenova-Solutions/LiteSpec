using System;
using System.Text;

namespace LiteSpec.Model;

/// <summary>
///     The casing conversions between code names and record identifiers. The CLI, the
///     analyzers and the code fixes all convert here, so a name reads the same in each.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
public static class Naming
{
    /// <summary>The characters that separate the words of a kebab-case or snake_case name.</summary>
    private static readonly char[] WordSeparators = ['-', '_'];

    /// <summary>
    ///     Turns a code name into the kebab-case form identifiers use: <c>SubmitExpense</c> reads
    ///     <c>submit-expense</c>, an acronym stays one word (<c>HTTPServer</c> reads <c>http-server</c>),
    ///     and underscores, dots and spaces read as hyphens.
    /// </summary>
    /// <param name="name">The PascalCase, camelCase or separated name to convert.</param>
    /// <returns>The kebab-case form, lowered, with single hyphens and none at either end.</returns>
    public static string Kebab(string name)
    {
        var built = new StringBuilder(name.Length + 8);

        for (var index = 0; index < name.Length; index++)
        {
            var character = name[index];

            if (character is '_' or '.' or '-' or ' ')
            {
                if (built.Length > 0 && built[built.Length - 1] != '-')
                {
                    built.Append('-');
                }

                continue;
            }

            if (character is >= 'A' and <= 'Z' && built.Length > 0 && built[built.Length - 1] != '-')
            {
                var previous = name[index - 1];
                var next = index + 1 < name.Length ? name[index + 1] : 'a';

                // A capital opens a word after a lowercase letter or a digit, or when the
                // letter after it is lowercase, so the last capital of an acronym opens the next word.
                if (previous is >= 'a' and <= 'z' or >= '0' and <= '9' || next is >= 'a' and <= 'z')
                {
                    built.Append('-');
                }
            }

            built.Append(char.ToLowerInvariant(character));
        }

        var kebab = built.ToString().Trim('-');

        while (kebab.IndexOf("--", StringComparison.Ordinal) >= 0)
        {
            kebab = kebab.Replace("--", "-");
        }

        return kebab;
    }

    /// <summary>
    ///     The key two operation names match under: the PascalCase form of a
    ///     kebab-case, snake_case or camelCase name, so a generated-client
    ///     export and an endpoint's <c>WithName</c> meet on either spelling.
    /// </summary>
    /// <param name="name">The operation name in kebab-case, snake_case or camelCase spelling.</param>
    /// <returns>The PascalCase key the spellings meet on.</returns>
    public static string OperationKey(string name) => Pascal(name);

    /// <summary>
    ///     Turns a kebab-case or snake_case name into the PascalCase form code uses.
    /// </summary>
    /// <param name="name">The name whose words are separated by hyphens or underscores.</param>
    /// <returns>The PascalCase form, with each part capitalized and the separators removed.</returns>
    public static string Pascal(string name)
    {
        var built = new StringBuilder(name.Length);

        foreach (var part in name.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            built.Append(char.ToUpperInvariant(part[0]));
            built.Append(part, 1, part.Length - 1);
        }

        return built.ToString();
    }

    /// <summary>
    ///     The kebab-case plural of an aggregate name, the folder that holds everything of that aggregate
    ///     in every layer: <c>expense</c> reads <c>expenses</c>, <c>chart-of-accounts</c> reads
    ///     <c>charts-of-accounts</c>, <c>person</c> reads <c>people</c>, <c>counterparty</c> reads
    ///     <c>counterparties</c>. A name with an <c>of</c> word pluralizes the word before it; any other
    ///     name pluralizes its last word.
    /// </summary>
    /// <param name="name">The aggregate name in kebab case, the name part of its identifier.</param>
    /// <returns>The plural folder name in kebab case.</returns>
    public static string PluralKebab(string name)
    {
        var words = Kebab(name).Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
        {
            return string.Empty;
        }

        var of = Array.IndexOf(words, "of");
        var at = of > 0 ? of - 1 : words.Length - 1;
        words[at] = PluralWord(words[at]);

        return string.Join("-", words);
    }

    /// <summary>The PascalCase plural of an aggregate name, the folder name .NET code uses: <c>IcpDeclarations</c>.</summary>
    /// <param name="name">The aggregate name in kebab case, the name part of its identifier.</param>
    /// <returns>The plural folder name in PascalCase.</returns>
    public static string PluralPascal(string name) => Pascal(PluralKebab(name));

    /// <summary>The plural of one lowercase English word, by the rules the folder names follow.</summary>
    /// <param name="word">The lowercase word.</param>
    /// <returns>The plural word.</returns>
    private static string PluralWord(string word)
    {
        if (word == "person")
        {
            return "people";
        }

        if (word.Length > 1 && word.EndsWith("y", StringComparison.Ordinal) && "aeiou".IndexOf(word[word.Length - 2]) < 0)
        {
            return word.Substring(0, word.Length - 1) + "ies";
        }

        if (word.EndsWith("s", StringComparison.Ordinal) || word.EndsWith("x", StringComparison.Ordinal)
            || word.EndsWith("z", StringComparison.Ordinal) || word.EndsWith("ch", StringComparison.Ordinal)
            || word.EndsWith("sh", StringComparison.Ordinal))
        {
            return word + "es";
        }

        return word + "s";
    }
}
