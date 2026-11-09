using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace LiteSpec.Authoring;

/// <summary>
///     The shared builder for <c>docs/reference/provisions.md</c> and
///     <c>docs/reference/provisions.json</c>. The validator and the generator both call
///     <see cref="BuildIndex"/> and <see cref="BuildJson"/>, so the checked-in pages and their
///     freshness gate cannot disagree.
/// </summary>
/// <follows>litespec/core/authoring#regenerate-the-provision-index</follows>
public static class Provisions
{
    /// <summary>The generated provision index, relative to the repository root.</summary>
    public const string IndexPath = "docs/reference/provisions.md";

    /// <summary>The generated provision data, relative to the repository root.</summary>
    public const string JsonPath = "docs/reference/provisions.json";

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

    // The index groups the top-level docs folders into four without moving any folder, so a
    // physical move never renames the provision IDs derived from page paths. A folder the map
    // does not name joins Standard: a new area states new concerns beside the standard ones.
    /// <summary>The index group each top-level docs folder belongs to.</summary>
    static readonly Dictionary<string, string> DocGroups = new(StringComparer.Ordinal)
    {
        ["backend"] = "Standard",
        ["core"] = "Standard",
        ["ext"] = "Standard",
        ["frontend"] = "Standard",
        ["quality"] = "Standard",
        ["workspace"] = "Standard",
        ["analyzers"] = "Reference",
        ["assets"] = "Reference",
        ["maintain"] = "Reference",
        ["reference"] = "Reference",
        ["tools"] = "Reference",
        ["example"] = "Learn",
        ["guide"] = "Learn",
        ["learn"] = "Learn",
        ["tutorial"] = "Learn",
        ["implementation"] = "LiteSpec's Own Records",
        ["intent"] = "LiteSpec's Own Records",
    };

    /// <summary>The index groups in display order.</summary>
    static readonly IReadOnlyList<string> GroupOrder = ["Standard", "Reference", "Learn", "LiteSpec's Own Records"];

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

    // A page takes its conformance level from the folder it sits in: the record-model
    // pages under docs/core belong to records, and every other standards area belongs
    // to full. No folder defaults to traced; a provision about marks, proof, or the
    // model builder states conformance: traced on its Applies to line when its level
    // differs from its page. docs/core/scopes.md states this table.
    /// <summary>The conformance level a page gives its provisions by default.</summary>
    /// <param name="relative">The repository-relative page path, with forward slashes.</param>
    /// <returns>The page conformance level, or empty when the path sits outside <c>docs/</c>.</returns>
    public static string PageConformance(string relative) =>
        relative.StartsWith("docs/core/", StringComparison.Ordinal)
            ? "records"
            : relative.StartsWith("docs/", StringComparison.Ordinal)
                ? "full"
                : "";

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
            var scope = Scopes.ReadPageScope(reader);
            var pageStacks = JoinScope(scope, "stack");
            var pageLayers = JoinScope(scope, "layer");
            var section = "";
            var headings = reader.Headings;

            for (var index = 0; index < headings.Count; index++)
            {
                var heading = headings[index];
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
                var end = headings
                    .Skip(index + 1)
                    .Where(next => next.Level == 2 || next.Level == 3 || (pattern && next.Level == 4))
                    .Select(next => next.Line)
                    .Append(reader.Lines.Length + 1)
                    .First();
                var body = ReadBody(reader, heading.Line, end, section == "Standards");
                var verification = marks.TryGetValue(id, out var found) ? found : (Mark: "", Evidence: "");
                rows.Add(new ProvisionRow(
                    id,
                    title,
                    relative,
                    slug,
                    pattern ? "pattern" : section == "Standards" ? "rule" : "convention",
                    verification.Mark,
                    verification.Evidence,
                    body.Stacks.Length > 0 ? body.Stacks : pageStacks,
                    body.Layers.Length > 0 ? body.Layers : pageLayers,
                    body.Artifact,
                    body.Conformance.Length > 0 ? body.Conformance : PageConformance(relative),
                    body.Strength,
                    body.Statement,
                    body.Rationale,
                    body.Example));
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
            "Use this generated page to resolve a provision ID to its heading, owning page, scope, conformance level, strength, and verification mark, and to read the checked share of rules for each page.",
            "",
            "Run `litespec-maintain provisions` after any provision change. The repository validator fails when this page and the active standards disagree.",
            "",
            $"The active release states {rows.Count} {(rows.Count == 1 ? "provision" : "provisions")} in {GroupOrder.Count} groups across {areas.Count} {(areas.Count == 1 ? "area" : "areas")}.",
            "",
            "## Checked Share by Page",
            "",
            "| Page | Rules | Checked | Checked Share |",
            "|:---|:---|:---|:---|",
        };

        foreach (var pageRows in rows.GroupBy(row => row.Page, StringComparer.Ordinal).OrderBy(pageGroup => pageGroup.Key, StringComparer.Ordinal))
        {
            var rules = pageRows.Where(row => row.Strength == "rule").ToList();
            var checkedCount = rules.Count(row => row.Mark == "checkable");
            var share = rules.Count == 0
                ? "-"
                : ((int)Math.Round(100.0 * checkedCount / rules.Count)).ToString(CultureInfo.InvariantCulture) + "%";
            var page = PagePath(pageRows.Key);
            lines.Add($"| [{page}](../{page}.md) | {rules.Count} | {checkedCount} | {share} |");
        }

        lines.Add("");

        foreach (var group in GroupOrder)
        {
            lines.Add($"## {group}");
            lines.Add("");
            var grouped = areas.Keys
                .Where(area => GroupOf(area) == group)
                .OrderBy(area => area, StringComparer.Ordinal)
                .ToList();
            if (grouped.Count == 0)
            {
                lines.Add("None.");
                lines.Add("");
                continue;
            }

            foreach (var area in grouped)
            {
                lines.Add($"### {area}");
                lines.Add("");
                lines.Add("| ID | Provision | Kind | Stack | Layer | Artifact | Conformance | Strength | Mark | Evidence |");
                lines.Add("|:---|:---|:---|:---|:---|:---|:---|:---|:---|:---|");

                foreach (var row in areas[area])
                {
                    var page = PagePath(row.Page);
                    lines.Add($"| {row.Id} | [{EscapeCell(row.Heading)}](../{page}.md#{row.Anchor}) | {row.Kind} | {Cell(row.Stack)} | {Cell(row.Layer)} | {Cell(row.Artifact)} | {Cell(row.Conformance)} | {Cell(row.Strength)} | {Cell(row.Mark)} | {Cell(row.Evidence)} |");
                }

                lines.Add("");
            }
        }

        return string.Join("\n", lines);
    }

    /// <summary>Builds the provision data text with <c>\n</c> line ends, without a trailing newline.</summary>
    /// <param name="root">The repository root.</param>
    /// <returns>The provision data text.</returns>
    public static string BuildJson(string root)
    {
        var provisions = new JsonArray();

        foreach (var row in Collect(root))
        {
            provisions.Add((JsonNode)new JsonObject
            {
                ["id"] = row.Id,
                ["heading"] = row.Heading,
                ["page"] = row.Page,
                ["anchor"] = row.Anchor,
                ["kind"] = row.Kind,
                ["mark"] = row.Mark,
                ["evidence"] = row.Evidence,
                ["stack"] = row.Stack,
                ["layer"] = row.Layer,
                ["artifact"] = row.Artifact,
                ["conformance"] = row.Conformance,
                ["strength"] = row.Strength,
                ["statement"] = row.Statement,
                ["rationale"] = row.Rationale,
                ["example"] = row.Example,
            });
        }

        return new JsonObject { ["provisions"] = provisions }.ToJsonString(JsonOptions);
    }

    /// <summary>The provision data layout: indented with <c>\n</c> line ends, and the default escaping.</summary>
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, NewLine = "\n" };

    /// <summary>Names the index group a top-level docs folder belongs to.</summary>
    /// <param name="area">The top-level docs folder.</param>
    /// <returns>The index group.</returns>
    static string GroupOf(string area) => DocGroups.TryGetValue(area, out var group) ? group : "Standard";

    /// <summary>Writes one index cell, or a dash when the row carries no value.</summary>
    /// <param name="value">The cell text.</param>
    /// <returns>The cell text, or a dash.</returns>
    static string Cell(string value) => value.Length > 0 ? EscapeCell(value) : "-";

    /// <summary>Joins the values of one scope field from a page scope.</summary>
    /// <param name="scope">The page scope fields.</param>
    /// <param name="field">The field name.</param>
    /// <returns>The values joined with a comma and a space.</returns>
    static string JoinScope(IReadOnlyList<Scopes.ScopeField> scope, string field) =>
        string.Join(", ", scope.Where(entry => entry.Name == field).SelectMany(entry => entry.Values));

    /// <summary>One provision body: its scope, strength, and labeled text.</summary>
    /// <param name="Strength">The named strength, or empty.</param>
    /// <param name="Stacks">The narrowed stack values, or empty.</param>
    /// <param name="Layers">The narrowed layer values, or empty.</param>
    /// <param name="Artifact">The narrowed artifact values, or empty.</param>
    /// <param name="Conformance">The conformance level the provision states, or empty when it takes its page level.</param>
    /// <param name="Statement">The assertive line, or empty.</param>
    /// <param name="Rationale">The Rationale paragraph, or empty.</param>
    /// <param name="Example">The Example block, or empty.</param>
    sealed record ProvisionBody(string Strength, string Stacks, string Layers, string Artifact, string Conformance, string Statement, string Rationale, string Example);

    // One provision body: the lines between its heading and the next heading. The Strength and
    // Applies to lines read as frame; the Statement, or the Default of a convention or pattern,
    // reads as the assertive line; the Rationale paragraph joins with spaces and the Example
    // block keeps its lines, fences included, so generated clients render code as written.
    /// <summary>Reads one provision body between its heading line and its end line.</summary>
    /// <param name="reader">The page read.</param>
    /// <param name="start">The 1-based provision heading line.</param>
    /// <param name="end">The 1-based first line after the body.</param>
    /// <param name="standard">Whether the provision sits in a Standards section.</param>
    /// <returns>The provision body.</returns>
    static ProvisionBody ReadBody(PageReader reader, int start, int end, bool standard)
    {
        var strength = "";
        var stacks = "";
        var layers = "";
        var artifact = "";
        var conformance = "";
        var statement = "";
        var rationale = "";
        var example = "";
        var assertion = standard ? "**Statement:**" : "**Default:**";
        var appliesRead = false;
        for (var number = start + 1; number < end; number++)
        {
            var line = reader.Lines[number - 1];
            if (strength.Length == 0 && PageReader.TryParseStrengthLine(line, out var named))
            {
                strength = Scopes.Unquote(named);
            }

            if (!appliesRead && PageReader.TryParseAppliesToLine(line, out var narrowed))
            {
                appliesRead = true;
                var fields = Scopes.ParseScopeFields(narrowed);
                stacks = JoinScope(fields, "stack");
                layers = JoinScope(fields, "layer");
                artifact = JoinScope(fields, "artifact");
                conformance = JoinScope(fields, "conformance");
            }

            if (statement.Length == 0 && line.StartsWith(assertion, StringComparison.Ordinal))
            {
                statement = line[assertion.Length..].Trim();
            }

            if (rationale.Length == 0 && reader.LabelAtLine.TryGetValue(number, out var rationaleLabel) && rationaleLabel == "Rationale")
            {
                rationale = ReadField(reader, number, end, "**Rationale:**", false);
            }

            if (example.Length == 0 && reader.LabelAtLine.TryGetValue(number, out var exampleLabel) && exampleLabel == "Example")
            {
                example = ReadField(reader, number, end, "**Example:**", true);
            }
        }

        return new ProvisionBody(strength, stacks, layers, artifact, conformance, statement, rationale, example);
    }

    // One labeled field: its label line rest with the lines below it, until the next label, frame
    // line, or verification line. The Rationale paragraph joins with spaces; the Example block
    // keeps its lines so fences survive, and drops fenced lines nowhere.
    /// <summary>Reads one labeled field of a provision body.</summary>
    /// <param name="reader">The page read.</param>
    /// <param name="start">The 1-based label line.</param>
    /// <param name="end">The 1-based first line after the body.</param>
    /// <param name="label">The label prefix.</param>
    /// <param name="keepLines">Whether the field keeps its line breaks.</param>
    /// <returns>The field text.</returns>
    static string ReadField(PageReader reader, int start, int end, string label, bool keepLines)
    {
        var parts = new List<string>();
        for (var number = start; number < end; number++)
        {
            var line = reader.Lines[number - 1];
            if (number > start
                && (reader.LabelAtLine.ContainsKey(number)
                    || PageReader.TryParseIdLine(line, out _)
                    || PageReader.TryParseVerificationLine(line, out _, out _)
                    || PageReader.TryParseStrengthLine(line, out _)
                    || PageReader.TryParseAppliesToLine(line, out _)))
            {
                break;
            }

            if (reader.FencedLines.Contains(number))
            {
                if (keepLines)
                {
                    parts.Add(line);
                }

                continue;
            }

            var text = number == start && line.StartsWith(label, StringComparison.Ordinal) ? line[label.Length..].Trim() : line.Trim();
            if (text.Length > 0 || keepLines)
            {
                parts.Add(text);
            }
        }

        while (parts.Count > 0 && parts[^1].Length == 0)
        {
            parts.RemoveAt(parts.Count - 1);
        }

        return keepLines ? string.Join("\n", parts).Trim() : string.Join(" ", parts).Trim();
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
