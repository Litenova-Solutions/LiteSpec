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
    private readonly IReadOnlyList<CodeownerRule> _rules;

    private Codeowners(IReadOnlyList<CodeownerRule> rules) => _rules = rules;

    /// <summary>Reads the CODEOWNERS file from its usual homes, or no rules when none exists.</summary>
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

    private static IReadOnlyList<CodeownerRule> Parse(string text)
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

    private static bool OwnerMatches(string entry, string owner)
    {
        var named = entry.StartsWith('@') ? entry[1..] : entry;
        var claimed = owner.StartsWith('@') ? owner[1..] : owner;

        return named.Equals(claimed, StringComparison.OrdinalIgnoreCase)
            || named.EndsWith("/" + claimed, StringComparison.OrdinalIgnoreCase);
    }

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
