using System.Globalization;
using System.Text.RegularExpressions;

namespace LiteSpec.Authoring;

/// <summary>
///     The shared builder for <c>docs/reference/provisions.md</c>. The validator and the
///     generator both call <see cref="BuildIndex"/>, so the checked-in page and its
///     freshness gate cannot disagree.
/// </summary>
/// <follows>litespec/core/authoring#regenerate-the-provision-index</follows>
public static class Provisions
{
    /// <summary>The generated provision index, relative to the repository root.</summary>
    public const string IndexPath = "docs/reference/provisions.md";

    /// <summary>Every provision kind (PROVISION_KINDS).</summary>
    public static readonly IReadOnlyList<string> Kinds = ["convention", "pattern", "rule"];

    /// <summary>The provision identifier pattern as a .NET regex source (PROVISION_ID_SOURCE).</summary>
    public static readonly string IdSource = "litespec\\/[a-z0-9]+(?:-[a-z0-9]+)*(?:\\/[a-z0-9]+(?:-[a-z0-9]+)*)*#[a-z0-9]+(?:-[a-z0-9]+)*";

    // Every provision identifier is litespec/<page>#<heading-slug>. The page is the
    // owning page path relative to docs/ without its extension, and the slug is the
    // heading slug. The ID line right below the heading states it, so a reader
    // copies the identifier without composing it. No segment carries a sequence number.
    /// <summary>The pattern a whole provision identifier matches.</summary>
    static readonly Regex ProvisionIdPattern = new($"^{IdSource}$");
    /// <summary>The pattern that splits a provision identifier into its page and slug.</summary>
    static readonly Regex ParseIdPattern = new(@"^litespec\/([a-z0-9]+(?:-[a-z0-9]+)*(?:\/[a-z0-9]+(?:-[a-z0-9]+)*)*)#([a-z0-9]+(?:-[a-z0-9]+)*)$");
    /// <summary>The trailing explicit anchor suffix a heading carries.</summary>
    static readonly Regex AnchorSuffix = new(@"\s+\{#[^}]+\}\s*$");
    /// <summary>The characters a heading slug drops.</summary>
    static readonly Regex SlugStrip = new(@"[^a-z0-9\s-]");
    /// <summary>One run of whitespace in a heading.</summary>
    static readonly Regex WhitespaceRun = new(@"\s+");
    /// <summary>One run of dashes in a slug.</summary>
    static readonly Regex DashRun = new(@"-+");

    /// <summary>Turns a heading into the anchor slug the index links to.</summary>
    /// <param name="value">The heading text.</param>
    /// <returns>The anchor slug.</returns>
    public static string HeadingSlug(string value)
    {
        var slug = AnchorSuffix.Replace(value, string.Empty).ToLowerInvariant().Replace("`", string.Empty, StringComparison.Ordinal);
        slug = SlugStrip.Replace(slug, string.Empty).Trim();
        slug = WhitespaceRun.Replace(slug, "-");
        return DashRun.Replace(slug, "-");
    }

    /// <summary>
    ///     Splits a provision identifier into its page and slug. The page is the owning
    ///     page path relative to <c>docs/</c> without its extension, so two pages named
    ///     <c>structure.md</c> in two folders stay distinct. Returns null when the
    ///     identifier is malformed.
    /// </summary>
    /// <param name="id">The provision identifier.</param>
    /// <returns>The parsed identifier, or null when the identifier is malformed.</returns>
    public static ProvisionId? Parse(string id)
    {
        var match = ParseIdPattern.Match(id);

        if (!match.Success)
        {
            return null;
        }

        return new ProvisionId(match.Groups[1].Value, match.Groups[2].Value);
    }

    /// <summary>Whether the identifier matches the provision identifier pattern.</summary>
    /// <param name="id">The provision identifier.</param>
    /// <returns>True when the identifier is a provision identifier.</returns>
    public static bool IsProvisionId(string id) => ProvisionIdPattern.IsMatch(id);

    // A provision is a ### heading inside a page's Standards or Conventions section,
    // on any page under docs/ except the lookup areas and the navigation pages, which
    // own no provisions.
    /// <summary>The repository-relative areas that own no provisions.</summary>
    static readonly string[] ExcludedAreas = ["docs/tools/", "docs/maintain/", "docs/reference/", "docs/tutorial/", "docs/guide/", "docs/learn/", "docs/analyzers/", "docs/example/"];

    /// <summary>Whether provisions are collected from this repository-relative path.</summary>
    /// <param name="relative">The repository-relative path, with forward slashes.</param>
    /// <returns>True when the path is a standards page that owns provisions.</returns>
    public static bool IsProvisionPage(string relative) =>
        relative.StartsWith("docs/", StringComparison.Ordinal)
        && relative.EndsWith(".md", StringComparison.Ordinal)
        && !relative.EndsWith("/README.md", StringComparison.Ordinal)
        && !ExcludedAreas.Any(area => relative.StartsWith(area, StringComparison.Ordinal));

    /// <summary>
    ///     The page path a provision identifier names: the repository-relative page path
    ///     without the <c>docs/</c> prefix and the <c>.md</c> extension.
    /// </summary>
    /// <param name="relative">The repository-relative page path, with forward slashes.</param>
    /// <returns>The page path the identifier names.</returns>
    public static string PagePath(string relative) => relative["docs/".Length..^".md".Length];

    /// <summary>Collects every provision row from the standards pages, sorted by identifier.</summary>
    /// <param name="root">The repository root.</param>
    /// <returns>Every provision row, sorted by identifier.</returns>
    public static List<ProvisionRow> Collect(string root)
    {
        var rows = new List<ProvisionRow>();

        foreach (var file in Walk(Path.Combine(root, "docs")))
        {
            var relative = Slash(Path.GetRelativePath(root, file));

            if (!IsProvisionPage(relative))
            {
                continue;
            }

            var page = PagePath(relative);
            var reader = PageReader.Parse(File.ReadAllText(file));
            var marks = CollectInlineMarks(reader, page, relative);
            var section = "";

            foreach (var heading in reader.Headings)
            {
                if (heading.Level == 2)
                {
                    section = PageReader.StripAnchorSuffix(heading.Text).Trim();
                }

                if (heading.Level != 3)
                {
                    continue;
                }

                var pattern = section == "Patterns" && Patterns.PatternPage.IsPatternPage(relative);
                if (section != "Standards" && section != "Conventions" && !pattern)
                {
                    continue;
                }

                var title = heading.Text.Trim();
                var slug = HeadingSlug(title);
                var id = IdOf(reader, page, heading);
                var verification = marks.TryGetValue(id, out var found) ? found : (Mark: "", Evidence: "");
                rows.Add(new ProvisionRow(
                    id,
                    title,
                    relative,
                    slug,
                    pattern ? "pattern" : section == "Standards" ? "rule" : "convention",
                    verification.Mark,
                    verification.Evidence));
            }
        }

        rows.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
        return rows;
    }

    /// <summary>Builds the provision index text with <c>\n</c> line ends, without a trailing newline.</summary>
    /// <param name="root">The repository root.</param>
    /// <returns>The provision index text.</returns>
    public static string BuildIndex(string root)
    {
        var rows = Collect(root);
        var areas = new Dictionary<string, List<ProvisionRow>>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            var area = row.Page.Split('/').Length > 1 ? row.Page.Split('/')[1] : "";
            if (!areas.TryGetValue(area, out var group))
            {
                areas[area] = group = [];
            }

            group.Add(row);
        }

        var lines = new List<string>
        {
            "# Provisions",
            "",
            "## Intent",
            "",
            "Use this generated page to resolve a provision ID to its heading, owning page, and verification mark.",
            "",
            "Run `litespec-maintain provisions` after any provision change. The repository validator fails when this page and the active standards disagree.",
            "",
            $"The active release states {rows.Count} {(rows.Count == 1 ? "provision" : "provisions")} across {areas.Count} {(areas.Count == 1 ? "area" : "areas")}.",
            "",
        };

        foreach (var area in areas.Keys.OrderBy(name => name, StringComparer.Ordinal))
        {
            lines.Add($"## {area}");
            lines.Add("");
            lines.Add("| ID | Provision | Kind | Mark | Evidence |");
            lines.Add("|:---|:---|:---|:---|:---|");

            foreach (var row in areas[area])
            {
                var page = PagePath(row.Page);
                var mark = row.Mark.Length > 0 ? row.Mark : "-";
                var evidence = EscapeCell(row.Evidence);
                lines.Add($"| {row.Id} | [{EscapeCell(row.Heading)}](../{page}.md#{row.Anchor}) | {row.Kind} | {mark} | {(evidence.Length > 0 ? evidence : "-")} |");
            }

            lines.Add("");
        }

        return string.Join("\n", lines);
    }

    /// <summary>Writes a path with forward slashes.</summary>
    /// <param name="value">The path as the operating system writes it.</param>
    /// <returns>The path with forward slashes.</returns>
    static string Slash(string value) => value.Replace('\\', '/');

    /// <summary>Lists every Markdown file under the folder, in ordinal order.</summary>
    /// <param name="directory">The folder to walk.</param>
    /// <returns>Every Markdown file under the folder.</returns>
    static List<string> Walk(string directory)
    {
        var result = new List<string>();

        if (!Directory.Exists(directory))
        {
            return result;
        }

        foreach (var entry in Directory.GetFileSystemEntries(directory).OrderBy(name => name, StringComparer.Ordinal))
        {
            if (Directory.Exists(entry))
            {
                result.AddRange(Walk(entry));
            }
            else if (entry.EndsWith(".md", StringComparison.Ordinal))
            {
                result.Add(entry);
            }
        }

        return result;
    }

    // A provision carries its verification beside its heading: an ID line first,
    // then its fields, then exactly one verification line, last. The label names
    // the mark: "Checked by" is checkable with the command that checks it, and
    // "Reviewed for" is guidance with the question a reviewer answers. The index
    // keeps its Mark column, derived from the label.
    /// <summary>Reads each provision's inline verification mark from its page.</summary>
    /// <param name="reader">The page read.</param>
    /// <param name="page">The page path the identifiers name.</param>
    /// <param name="relative">The repository-relative page path, with forward slashes.</param>
    /// <returns>The verification mark and evidence by provision identifier.</returns>
    static Dictionary<string, (string Mark, string Evidence)> CollectInlineMarks(PageReader reader, string page, string relative)
    {
        var marks = new Dictionary<string, (string Mark, string Evidence)>(StringComparer.Ordinal);
        var verifications = new Dictionary<int, (string Mark, string Evidence)>();
        foreach (var entry in reader.LabelAtLine)
        {
            if (entry.Value is "Checked by" or "Reviewed for"
                && PageReader.TryParseVerificationLine(reader.Lines[entry.Key - 1], out var label, out var evidence))
            {
                verifications[entry.Key] = (label == "Checked by" ? "checkable" : "guidance", evidence);
            }
        }

        var section = "";
        string? current = null;
        var headings = reader.Headings.Where(heading => heading.Level == 2 || heading.Level == 3).ToList();
        var lines = headings.Select(heading => heading.Line).Concat(verifications.Keys).OrderBy(line => line).ToList();
        var byLine = headings.ToDictionary(heading => heading.Line);

        foreach (var line in lines)
        {
            if (verifications.TryGetValue(line, out var verification))
            {
                if (current is not null && !marks.ContainsKey(current))
                {
                    marks[current] = verification;
                }

                continue;
            }

            var heading = byLine[line];
            if (heading.Level == 2)
            {
                section = PageReader.StripAnchorSuffix(heading.Text).Trim();
                current = null;
                continue;
            }

            var pattern = section == "Patterns" && Patterns.PatternPage.IsPatternPage(relative);
            if (section == "Standards" || section == "Conventions" || pattern)
            {
                current = IdOf(reader, page, heading);
            }
        }

        return marks;
    }

    /// <summary>
    ///     The identifier a provision states on the ID line under its heading, which is its
    ///     identity; the heading slug stands in only where no ID line names this page.
    /// </summary>
    /// <param name="reader">The page read.</param>
    /// <param name="page">The page path the identifiers name.</param>
    /// <param name="heading">The provision heading.</param>
    /// <returns>The provision identifier.</returns>
    static string IdOf(PageReader reader, string page, PageReader.ReaderHeading heading)
    {
        for (var index = heading.Line; index < reader.Lines.Length; index++)
        {
            var line = reader.Lines[index];

            if (line.Trim().Length == 0)
            {
                continue;
            }

            if (PageReader.TryParseIdLine(line, out var stated) && Parse(stated)?.Page == page)
            {
                return stated;
            }

            break;
        }

        return $"litespec/{page}#{HeadingSlug(heading.Text.Trim())}";
    }

    /// <summary>Escapes a table cell value.</summary>
    /// <param name="value">The cell text.</param>
    /// <returns>The cell text with each bar escaped.</returns>
    static string EscapeCell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);
}
