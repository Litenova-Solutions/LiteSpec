using System.Text.RegularExpressions;

namespace LiteSpec.Consumer;

/// <summary>
///     The heading checks of the consumer validator: the page title and every
///     section heading use Title Case.
/// </summary>
/// <follows>litespec/core/authoring#use-controlled-capitalization</follows>
static class HeadingChecks
{
    // The words that stay lowercase inside a Title Case heading, unless first or
    // last: articles, coordinating conjunctions, and prepositions of four letters
    // or fewer. Every other word starts with a capital.
    /// <summary>The words that stay lowercase inside a Title Case heading.</summary>
    static readonly HashSet<string> TitleSmallWords = new(
        ["a", "an", "the", "and", "but", "for", "nor", "or", "so", "yet",
         "as", "at", "by", "from", "in", "into", "of", "off", "on", "onto",
         "over", "per", "to", "up", "upon", "via", "with"],
        StringComparer.Ordinal);

    // Each pattern below runs on every page of a large consumer, so each one
    // compiles once instead of interpreting on every match. Compilation changes
    // only the speed, never what matches.
    /// <summary>One word of a heading.</summary>
    static readonly Regex TitleToken = new("[A-Za-z0-9][A-Za-z0-9'./+-]*", RegexOptions.Compiled);

    /// <summary>A token that passes Title Case unchanged: a version or an all-capitals token.</summary>
    static readonly Regex TitleAtypical = new("^(?:v?\\d|[A-Z0-9.]+$)", RegexOptions.Compiled);

    /// <summary>A camel-case token.</summary>
    static readonly Regex TitleCamel = new("[a-z][A-Z]|[A-Z][a-z]+[A-Z]", RegexOptions.Compiled);

    /// <summary>A token starting with a capital.</summary>
    static readonly Regex TitleCapital = new("^[A-Z]", RegexOptions.Compiled);

    /// <summary>
    ///     Whether a heading uses Title Case: every word starts with a capital except
    ///     a small word in the middle, with version numbers, all-capitals tokens,
    ///     camel-case names and dotted names passing through.
    /// </summary>
    /// <param name="value">The heading text.</param>
    /// <returns>True when the heading uses Title Case.</returns>
    public static bool TitleCase(string value)
    {
        var tokens = TitleToken.Matches(value).Select(match => match.Value).ToList();

        return tokens.Select((token, index) => (token, index)).All(item =>
        {
            if (TitleAtypical.IsMatch(item.token) || TitleCamel.IsMatch(item.token) || item.token.Contains('.'))
            {
                return true;
            }

            if (item.index > 0 && item.index < tokens.Count - 1 && TitleSmallWords.Contains(item.token.ToLowerInvariant()))
            {
                return !item.token.Any(character => character is >= 'A' and <= 'Z');
            }

            return TitleCapital.IsMatch(item.token);
        });
    }

    /// <summary>
    ///     Checks that the page title and every section heading use Title Case.
    ///     Headings inside fenced blocks and generated regions are tool output or
    ///     quoted examples rather than authored headings, so they are skipped.
    /// </summary>
    /// <param name="relative">The page path the findings name.</param>
    /// <param name="lines">The page lines.</param>
    /// <param name="add">Collects one finding.</param>
    public static void CheckHeadings(string relative, string[] lines, Action<string> add)
    {
        var inFence = false;
        var inGenerated = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (inFence)
            {
                continue;
            }

            if (trimmed.StartsWith("<!-- generated:", StringComparison.Ordinal))
            {
                inGenerated = true;
                continue;
            }

            if (trimmed.StartsWith("<!-- /generated:", StringComparison.Ordinal))
            {
                inGenerated = false;
                continue;
            }

            if (inGenerated)
            {
                continue;
            }

            if (trimmed.StartsWith("# ", StringComparison.Ordinal))
            {
                var title = trimmed[2..].Trim();

                if (title.Length > 0 && !TitleCase(title))
                {
                    add($"{relative}: H1 '{title}' must use Title Case");
                }
            }
            else if (trimmed.StartsWith("## ", StringComparison.Ordinal))
            {
                var name = trimmed[3..].Trim();

                if (name.Length > 0 && !TitleCase(name))
                {
                    add($"{relative}: H2 '{name}' must use Title Case");
                }
            }
        }
    }
}
