using System;
using System.Text;

namespace LiteSpec.Analyzers.Generation.Sources;

/// <summary>The text rules every generated source shares: a C# string literal escaped, and a record name turned into a PascalCase identifier.</summary>
/// <follows>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</follows>
internal static class GeneratedText
{
    /// <summary>
    /// Capitalizes each kebab word of the name, keeping letters and digits only. A name that
    /// stays empty yields null, and one opening with a digit takes a leading underscore, so the
    /// result always carries an identifier.
    /// </summary>
    /// <param name="name">The kebab-case name.</param>
    /// <returns>The PascalCase name, or null when the name carries none.</returns>
    internal static string? PascalName(string name)
    {
        var built = new StringBuilder();
        var words = name.Split(['-'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            var kept = new StringBuilder();

            foreach (var character in word)
            {
                if (IsAsciiLetterOrDigit(character))
                {
                    kept.Append(character);
                }
            }

            if (kept.Length == 0)
            {
                continue;
            }

            kept[0] = char.ToUpperInvariant(kept[0]);
            built.Append(kept.ToString());
        }

        if (built.Length == 0)
        {
            return null;
        }

        var text = built.ToString();

        if (text[0] >= '0' && text[0] <= '9')
        {
            text = "_" + text;
        }

        return text;
    }

    /// <summary>Escapes the value for use inside a generated string literal.</summary>
    /// <param name="value">The value to convert or test.</param>
    /// <returns>The string as text.</returns>
    internal static string Escape(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>Returns true when the character is ascii letter or digit; otherwise false.</summary>
    /// <param name="character">The character to convert.</param>
    /// <returns>True when the character is ascii letter or digit; otherwise false.</returns>
    private static bool IsAsciiLetterOrDigit(char character) =>
        (character >= 'a' && character <= 'z')
        || (character >= 'A' && character <= 'Z')
        || (character >= '0' && character <= '9');
}
