using System.Globalization;
using System.Text.RegularExpressions;
using LiteSpec.Maintain.Validate.Pages;
using LiteSpec.Standards;
using static LiteSpec.Maintain.Validate.PageText;
using static LiteSpec.Maintain.Validate.RepositoryWalk;

namespace LiteSpec.Maintain.Validate.Repository;

/// <summary>The repository-wide checks: page identity, citations, root index routing, generated counts, and the docs tree.</summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal sealed partial class RepositoryValidator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RepositoryValidator"/> class.
    /// </summary>
    /// <param name="context">The shared validation context.</param>
    public RepositoryValidator(ValidationContext context)
    {
        Context = context;
    }

    /// <summary>Gets the shared validation context.</summary>
    private ValidationContext Context { get; }

    /// <summary>Checks that every docs path segment is lowercase kebab case.</summary>
    /// <param name="parsedPages">The parsed pages.</param>
    internal void CheckPageIdentity(List<ParsedPage> parsedPages)
    {
        var add = Context.Add;
        // The identifier names the page path, so every folder and file stem on that
        // path is lowercase kebab case and the navigation pages keep their README.md name.
        foreach (var page in parsedPages)
        {
            if (!page.Relative.StartsWith("docs/", StringComparison.Ordinal))
            {
                continue;
            }

            var segments = page.Relative["docs/".Length..].Split('/');
            for (var index = 0; index < segments.Length; index++)
            {
                var last = index == segments.Length - 1;
                if (last && segments[index] == "README.md")
                {
                    continue;
                }

                var name = last && segments[index].EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                    ? segments[index][..^".md".Length]
                    : segments[index];
                if (!KebabSegmentPattern().IsMatch(name))
                {
                    add(page.Relative, 1, MaintainerDiagnostics.Codes.IdPageFilename, $"path segment '{name}' is not lowercase kebab case");
                }
            }
        }
    }

    /// <summary>The longest repository path a packaged file may have; NuGet refuses longer ones inside the tool package (NU5123).</summary>
    private const int MaxPackagedPathLength = 128;

    /// <summary>Checks that every file the tool package carries has a path NuGet can pack.</summary>
    internal void CheckPackagedPathLength()
    {
        foreach (var folder in new[] { "docs", "schemas", "templates" })
        {
            var full = Path.Combine(Context.Root, folder);
            if (!Directory.Exists(full))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(Context.Root, file).Replace('\\', '/');
                if (relative.Length > MaxPackagedPathLength)
                {
                    Context.Add(relative, 1, MaintainerDiagnostics.Codes.PackagedPathLong, $"path is {relative.Length} characters, longer than the {MaxPackagedPathLength} the tool package can carry");
                }
            }
        }
    }

    /// <summary>Checks that every provision citation in active content names an active provision.</summary>
    /// <param name="parsedPages">The parsed pages.</param>
    /// <param name="activeIds">The active provision identifiers.</param>
    internal void CheckCitations(List<ParsedPage> parsedPages, HashSet<string> activeIds)
    {
        var add = Context.Add;
        foreach (var page in parsedPages)
        {
            // Templates carry placeholders and generated regions the template
            // citation check reads, so they are its pages, not this check's.
            if (page.Relative.StartsWith("templates/", StringComparison.Ordinal))
            {
                continue;
            }

            var prose = string.Join('\n', page.Reader.Lines.Select((line, index) =>
                page.Reader.FencedLines.Contains(index + 1) || page.Reader.GeneratedLines.Contains(index + 1) ? "" : line));
            foreach (Match match in CitationPattern.Matches(prose))
            {
                if (!activeIds.Contains(match.Value))
                {
                    add(page.Relative, LineNumber(prose, match.Index), MaintainerDiagnostics.Codes.IdUnknownReference, $"active content references unknown provision '{match.Value}'");
                }
            }
        }
    }

    /// <summary>Checks that the documentation root routes the reader before its navigation groups.</summary>
    internal void CheckRootIndexRouting()
    {
        var root = Context.Root;
        var add = Context.Add;
        const string relative = "docs/README.md";
        var file = JoinRoot(root, relative);
        if (!File.Exists(file))
        {
            return;
        }

        var reader = PageReader.Parse(FileText.ReadText(file));
        var routingLine = -1;
        var firstGroupLine = -1;
        var h2s = reader.Headings.Where(heading => heading.Level == 2).ToList();
        foreach (var heading in h2s)
        {
            if (heading.Text.Trim() == RoutingHeading && routingLine < 0)
            {
                routingLine = heading.Line;
                break;
            }
        }

        // A navigation group is a section whose body lists links. Intent introduces
        // the page and names no destination, so a link inside it starts no group.
        foreach (var bulletLine in reader.NavBulletLines)
        {
            string? currentHeading = null;
            foreach (var heading in h2s)
            {
                if (heading.Line < bulletLine)
                {
                    currentHeading = heading.Text.Trim();
                }
                else
                {
                    break;
                }
            }

            if (currentHeading is not null && currentHeading.Length > 0 && currentHeading != "Intent" && currentHeading != RoutingHeading)
            {
                firstGroupLine = bulletLine;
                break;
            }
        }

        if (routingLine < 0)
        {
            add(relative, 1, MaintainerDiagnostics.Codes.IndexMissingRouting, $"documentation root index has no '{RoutingHeading}' section, so a reader cannot tell which page class answers their question");
            return;
        }

        if (firstGroupLine >= 0 && routingLine > firstGroupLine)
        {
            add(relative, routingLine, MaintainerDiagnostics.Codes.IndexMissingRouting, $"'{RoutingHeading}' appears after the first navigation group at line {firstGroupLine}");
        }
    }

    // Counts that tools own live in generated regions, so they cannot drift from their
    // sources: `<!-- generated:counts:<source> -->` states the count its source computes,
    // and the gate fails a stale region like a stale index. Pages without such regions
    // face no check, and fenced regions are examples rather than owned counts.
    /// <summary>Checks that every generated counts region states its source count.</summary>
    /// <param name="parsedPages">The parsed pages.</param>
    internal void CheckGeneratedCounts(List<ParsedPage> parsedPages)
    {
        var add = Context.Add;
        var counts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["provisions"] = Provisions.Collect(Context.Root).Count,
            ["analyzer-rules"] = AnalyzerRulePages(Context.Root),
        };
        foreach (var page in parsedPages)
        {
            var lines = page.Reader.Lines;
            for (var index = 0; index < lines.Length; index++)
            {
                if (page.Reader.FencedLines.Contains(index + 1) || CountsOpener(lines[index]) is not string source)
                {
                    continue;
                }

                var closer = -1;
                for (var scan = index + 1; scan < lines.Length; scan++)
                {
                    if (lines[scan].Contains("/generated:", StringComparison.Ordinal))
                    {
                        closer = scan;
                        break;
                    }
                }

                if (closer < 0)
                {
                    add(page.Relative, index + 1, MaintainerDiagnostics.Codes.GeneratedStale, "generated counts region has no closing marker");
                    continue;
                }

                var content = string.Join('\n', lines[(index + 1)..closer]);
                if (source.Length == 0)
                {
                    add(page.Relative, index + 1, MaintainerDiagnostics.Codes.GeneratedStale, "generated counts region names no count source; name provisions or analyzer-rules");
                }
                else if (!counts.TryGetValue(source, out var expected))
                {
                    add(page.Relative, index + 1, MaintainerDiagnostics.Codes.GeneratedStale, $"generated counts region names unknown count source '{source}'");
                }
                else
                {
                    var match = CountNumberPattern().Match(content);
                    if (!match.Success || !int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stated) || stated != expected)
                    {
                        add(page.Relative, index + 1, MaintainerDiagnostics.Codes.GeneratedStale, match.Success
                            ? $"generated counts region states {match.Value} but the source holds {expected}"
                            : "generated counts region states no count");
                    }
                }

                index = closer;
            }
        }
    }

    /// <summary>Reads the count source a generated counts opener names, or null when the line opens none.</summary>
    /// <param name="line">The raw source line.</param>
    /// <returns>The named source, or null.</returns>
    private static string? CountsOpener(string line)
    {
        var open = line.IndexOf("<!--", StringComparison.Ordinal);
        if (open < 0)
        {
            return null;
        }

        var close = line.IndexOf("-->", open + 4, StringComparison.Ordinal);
        if (close < 0)
        {
            return null;
        }

        var inner = line.Substring(open + 4, close - open - 4).Trim();
        const string prefix = "generated:counts";
        if (!inner.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var rest = inner[prefix.Length..].Trim();
        if (rest.Length == 0)
        {
            return "";
        }

        return rest.StartsWith(':') ? rest[1..].Trim() : null;
    }

    /// <summary>Counts the analyzer rule pages under docs/analyzers/rules.</summary>
    /// <param name="root">The repository root.</param>
    /// <returns>The rule page count.</returns>
    private static int AnalyzerRulePages(string root)
    {
        var rules = Path.Combine(root, "docs", "analyzers", "rules");
        if (!Directory.Exists(rules))
        {
            return 0;
        }

        return Directory.GetFiles(rules, "*.md", SearchOption.TopDirectoryOnly).Count(file => RulePagePattern().IsMatch(Path.GetFileName(file)));
    }

    /// <summary>Gets the pattern that matches a lowercase kebab path segment.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")] private static partial Regex KebabSegmentPattern();

    /// <summary>Gets the pattern that matches the first count in a generated counts region.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\d+")] private static partial Regex CountNumberPattern();

    /// <summary>Gets the pattern that matches one analyzer rule page file name.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^ls[0-9]+\.md$", RegexOptions.IgnoreCase)] private static partial Regex RulePagePattern();
}
