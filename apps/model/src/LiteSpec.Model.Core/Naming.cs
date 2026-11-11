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
}
