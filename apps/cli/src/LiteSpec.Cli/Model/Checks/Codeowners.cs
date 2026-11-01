using System.Text;
using System.Text.RegularExpressions;

namespace LiteSpec.Model.Checks;

/// <summary>
///     The consumer CODEOWNERS file, when one exists: a record's owner, or a CODEOWNERS
///     handle its project owners entry lists, matches an owner of the last pattern its
///     path matches. Basename patterns match in any folder, directory patterns own
///     everything beneath, and `*`, `**` and `?` span as usual.
/// </summary>
/// <follows>litespec/quality/ci#map-record-areas-to-owners</follows>
internal sealed class Codeowners
{
    /// <summary>The parsed ownership rules in file order, where a later rule overrides an earlier one for the same path.</summary>
    private readonly IReadOnlyList<CodeownerRule> _rules;

    /// <summary>Initializes a new instance of the <see cref="Codeowners" /> class with rules already parsed.</summary>
    /// <param name="rules">The parsed ownership rules in file order.</param>
    private Codeowners(IReadOnlyList<CodeownerRule> rules) => _rules = rules;

    /// <summary>Reads the CODEOWNERS file from its usual homes, or no rules when none exists.</summary>
    /// <param name="root">The consumer root that holds the CODEOWNERS file in one of its usual locations.</param>
    /// <returns>The parsed rules, or no rules when no CODEOWNERS file exists.</returns>
    public static Codeowners Load(string root)
    {
        foreach (var candidate in new[] { ".github/CODEOWNERS", "CODEOWNERS", "docs/CODEOWNERS" })
        {
            var file = Path.Combine(root, candidate.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(file))
            {
                return new Codeowners(Parse(SourceTree.Read(file)));
            }
        }

        return new Codeowners([]);
    }

    /// <summary>Whether a candidate matches an owner of the file's path, or no rule constrains the path.</summary>
    /// <param name="root">The consumer root the file path is made relative to.</param>
    /// <param name="file">The full path of the file whose owners decide coverage.</param>
    /// <param name="candidates">The owner handles claimed for the record, with or without the leading at sign.</param>
    /// <returns>True when no rule constrains the path or a candidate matches an owner of the last matching rule; otherwise false.</returns>
    public bool Covers(string root, string file, IReadOnlyList<string> candidates)
    {
        if (_rules.Count == 0)
        {
            return true;
        }

        var relative = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
        IReadOnlyList<string>? hit = null;

        foreach (var rule in _rules)
        {
            if (rule.Matcher.IsMatch(relative))
            {
                hit = rule.Owners;
            }
        }

        return hit is null || candidates.Any(candidate => hit.Any(entry => OwnerMatches(entry, candidate)));
    }

    /// <summary>Parses CODEOWNERS text into rules, skipping blank lines, comments and lines that name no owner.</summary>
    /// <param name="text">The full text of the CODEOWNERS file.</param>
    /// <returns>The ownership rules in file order.</returns>
    private static List<CodeownerRule> Parse(string text)
    {
        var rules = new List<CodeownerRule>();

        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();

            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                continue;
            }

            rules.Add(new CodeownerRule(RuleMatcher(parts[0]), parts[1..]));
        }

        return rules;
    }

    /// <summary>Decides whether a rule owner entry names the claimed owner, ignoring case and the leading at sign.</summary>
    /// <param name="entry">The owner entry written in the CODEOWNERS rule.</param>
    /// <param name="owner">The owner handle claimed for the record.</param>
    /// <returns>True when the entry equals the claim or ends with the claim as a team member suffix; otherwise false.</returns>
    private static bool OwnerMatches(string entry, string owner)
    {
        var named = entry.StartsWith('@') ? entry[1..] : entry;
        var claimed = owner.StartsWith('@') ? owner[1..] : owner;

        return named.Equals(claimed, StringComparison.OrdinalIgnoreCase)
            || named.EndsWith("/" + claimed, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Builds the regular expression that decides whether a CODEOWNERS pattern owns a relative path.</summary>
    /// <param name="pattern">The CODEOWNERS pattern as written, with any leading slash kept for anchoring.</param>
    /// <returns>The compiled matcher for the pattern.</returns>
    private static Regex RuleMatcher(string pattern)
    {
        var mine = pattern.StartsWith('/') ? pattern[1..] : pattern;
        string full;

        if (mine.EndsWith('/'))
        {
            full = $"^(?:{GlobBody(mine.TrimEnd('/'))})/";
        }
        else if (!mine.Contains('/'))
        {
            full = $"^(?:.*/)?(?:{GlobBody(mine)})$";
        }
        else if (mine.IndexOfAny(['*', '?']) < 0)
        {
            full = $"^(?:{GlobBody(mine)})(?:/.*)?$";
        }
        else
        {
            full = $"^(?:{GlobBody(mine)})$";
        }

        return new Regex(full, RegexOptions.Compiled);
    }

    /// <summary>Turns a CODEOWNERS pattern into its regex body: `**` spans folders, `*` and `?` stay inside one.</summary>
    /// <param name="pattern">The glob pattern without its anchoring slashes.</param>
    /// <returns>The regular expression body that matches the same paths as the glob.</returns>
    private static string GlobBody(string pattern)
    {
        var built = new StringBuilder();

        for (var index = 0; index < pattern.Length; index++)
        {
            if (pattern[index] == '*' && index + 1 < pattern.Length && pattern[index + 1] == '*')
            {
                if (index + 2 < pattern.Length && pattern[index + 2] == '/')
                {
                    built.Append("(?:.*/)?");
                    index += 2;
                }
                else
                {
                    built.Append(".*");
                    index += 1;
                }
            }
            else if (pattern[index] == '*')
            {
                built.Append("[^/]*");
            }
            else if (pattern[index] == '?')
            {
                built.Append("[^/]");
            }
            else
            {
                built.Append(Regex.Escape(pattern[index].ToString()));
            }
        }

        return built.ToString();
    }
}
