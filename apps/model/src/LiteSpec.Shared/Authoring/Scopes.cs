using System.Text.RegularExpressions;

namespace LiteSpec.Authoring;

/// <summary>
///     The scope vocabulary of <c>docs/core/scopes.md</c>: the stack, layer, artifact, and
///     conformance values every Scope block and Applies to line uses, with the parsers for
///     those lines. Strength values need no table: the Strength line diagnostic names
///     <c>rule</c>, <c>default</c>, and <c>advice</c> itself. Conformance values also stand
///     in code, because the level names are the same three the project file declares.
/// </summary>
/// <follows>litespec/core/scopes#use-only-declared-scope-values</follows>
public sealed class Scopes
{
    /// <summary>One named field of a Scope block or an Applies to line, with its values in order.</summary>
    /// <param name="Name">The field name as written.</param>
    /// <param name="Values">The field values in order.</param>
    public sealed record ScopeField(string Name, IReadOnlyList<string> Values);

    /// <summary>The scope vocabulary page, relative to the repository root.</summary>
    public const string PagePath = "docs/core/scopes.md";

    /// <summary>The artifact value that governs every file of the provision layer.</summary>
    public const string AnyArtifact = "any";

    /// <summary>Gets the Strength values a Strength line names.</summary>
    public static readonly IReadOnlySet<string> StrengthValues = new HashSet<string>(
        ["rule", "default", "advice"],
        StringComparer.Ordinal);

    /// <summary>Gets the conformance level values an Applies to line names.</summary>
    public static readonly IReadOnlySet<string> ConformanceValues = new HashSet<string>(
        ["records", "traced", "full"],
        StringComparer.Ordinal);

    /// <summary>The pattern of one backticked span.</summary>
    static readonly Regex BacktickSpan = new("`([^`]+)`", RegexOptions.Compiled);

    /// <summary>
    ///     Initializes a new instance of the <see cref="Scopes"/> class.
    /// </summary>
    /// <param name="stacks">The stack values.</param>
    /// <param name="layers">The layer values.</param>
    /// <param name="artifacts">The artifact values.</param>
    /// <param name="artifactPrefixes">The artifact value prefixes, such as <c>record:</c>.</param>
    /// <param name="conformances">The conformance level values.</param>
    Scopes(
        IReadOnlySet<string> stacks,
        IReadOnlySet<string> layers,
        IReadOnlySet<string> artifacts,
        IReadOnlyList<string> artifactPrefixes,
        IReadOnlySet<string> conformances)
    {
        Stacks = stacks;
        Layers = layers;
        Artifacts = artifacts;
        ArtifactPrefixes = artifactPrefixes;
        Conformances = conformances;
    }

    /// <summary>Gets the stack values.</summary>
    public IReadOnlySet<string> Stacks { get; }

    /// <summary>Gets the layer values.</summary>
    public IReadOnlySet<string> Layers { get; }

    /// <summary>Gets the artifact values.</summary>
    public IReadOnlySet<string> Artifacts { get; }

    /// <summary>Gets the artifact value prefixes, each with its trailing colon.</summary>
    public IReadOnlyList<string> ArtifactPrefixes { get; }

    /// <summary>Gets the conformance level values the scope page declares.</summary>
    public IReadOnlySet<string> Conformances { get; }

    /// <summary>Gets a value indicating whether every vocabulary table parsed, so value checks can run.</summary>
    public bool Readable => Stacks.Count > 0 && Layers.Count > 0 && Artifacts.Count > 0;

    /// <summary>Reports whether a value is a declared artifact: one listed kind or one prefixed record.</summary>
    /// <param name="value">The artifact value.</param>
    /// <returns>True when the value is a declared artifact.</returns>
    public bool IsArtifact(string value) =>
        Artifacts.Contains(value)
        || ArtifactPrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal) && value.Length > prefix.Length);

    /// <summary>Reports whether a value is the layer-wide artifact, governing every file of its layer.</summary>
    /// <param name="value">The artifact value.</param>
    /// <returns>True when the value is <see cref="AnyArtifact"/>.</returns>
    public static bool IsLayerWide(string value) =>
        string.Equals(value.Trim(), AnyArtifact, StringComparison.Ordinal);

    /// <summary>Splits one artifact field into its values, in written order.</summary>
    /// <param name="artifact">The artifact field text, with values separated by a comma.</param>
    /// <returns>The artifact values, each trimmed, with no empty value.</returns>
    public static IReadOnlyList<string> ArtifactValues(string artifact) =>
        artifact.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .ToList();

    /// <summary>
    ///     Loads the vocabulary from the scope page under <paramref name="root"/>. A missing or
    ///     unreadable page loads as unreadable rather than failing, so callers skip value checks.
    /// </summary>
    /// <param name="root">The repository root.</param>
    /// <returns>The loaded vocabulary.</returns>
    public static Scopes Load(string root)
    {
        var empty = new Scopes(
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            [],
            new HashSet<string>(StringComparer.Ordinal));
        string raw;
        try
        {
            var file = Path.Combine(root, PagePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(file))
            {
                return empty;
            }

            raw = File.ReadAllText(file);
        }
        catch (IOException)
        {
            return empty;
        }
        catch (UnauthorizedAccessException)
        {
            return empty;
        }

        var stacks = new HashSet<string>(StringComparer.Ordinal);
        var layers = new HashSet<string>(StringComparer.Ordinal);
        var artifacts = new HashSet<string>(StringComparer.Ordinal);
        var prefixes = new List<string>();
        var conformances = new HashSet<string>(StringComparer.Ordinal);
        var lines = raw.Split(["\r\n", "\n"], StringSplitOptions.None);
        string? section = null;
        var table = new List<string>();
        var fenced = false;
        void Flush()
        {
            ReadTable(table, section, stacks, layers, artifacts, prefixes, conformances);
            table.Clear();
        }

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                fenced = !fenced;
                continue;
            }

            if (fenced)
            {
                continue;
            }

            if (trimmed.StartsWith("### ", StringComparison.Ordinal))
            {
                Flush();
                section = FieldSection(trimmed["### ".Length..]);
                continue;
            }

            if (trimmed.StartsWith("## ", StringComparison.Ordinal) || trimmed.StartsWith("# ", StringComparison.Ordinal))
            {
                Flush();
                section = null;
                continue;
            }

            if (trimmed.StartsWith('|'))
            {
                table.Add(line);
            }
            else
            {
                Flush();
            }
        }

        Flush();
        return new Scopes(stacks, layers, artifacts, prefixes, conformances);
    }

    /// <summary>Removes one surrounding backtick pair from a value, with the spaces around it.</summary>
    /// <param name="value">The value as written.</param>
    /// <returns>The value without its backticks.</returns>
    public static string Unquote(string value)
    {
        var text = value.Trim();
        return text.Length >= 2 && text.StartsWith('`') && text.EndsWith('`') ? text[1..^1].Trim() : text;
    }

    // One scope line or Applies to line: `field: value` segments separated by a comma or
    // a semicolon, where a segment without a colon continues the values of the field
    // before it. Fields keep the order stack, layer, artifact, conformance, separated
    // by `; ` on an Applies to line. Each segment is unquoted first, so a wholly
    // backticked `artifact: aggregate` reads as its inner text. Stray text before the
    // first field is ignored; the value checks own what remains.
    /// <summary>Parses the fields of one scope line or Applies to line.</summary>
    /// <param name="text">The line text after its label.</param>
    /// <returns>The fields in first-seen order, with their values in order.</returns>
    public static IReadOnlyList<ScopeField> ParseScopeFields(string text)
    {
        var fields = new List<ScopeField>();
        var values = new List<List<string>>();
        string? current = null;
        foreach (var segment in text.Split(',', ';'))
        {
            var cleaned = Unquote(segment);
            var colon = cleaned.IndexOf(':');
            if (colon < 0)
            {
                if (current is not null)
                {
                    var continued = Unquote(cleaned);
                    if (continued.Length > 0)
                    {
                        values[fields.FindIndex(field => field.Name == current)].Add(continued);
                    }
                }

                continue;
            }

            var name = cleaned[..colon].Trim();
            current = name;
            var value = Unquote(cleaned[(colon + 1)..]);
            var index = fields.FindIndex(field => field.Name == name);
            if (index < 0)
            {
                var collected = new List<string>();
                fields.Add(new ScopeField(name, collected));
                values.Add(collected);
                index = fields.Count - 1;
            }

            if (value.Length > 0)
            {
                values[index].Add(value);
            }
        }

        return fields;
    }

    // The page scope: the merged stack and layer fields from the `**Stack:**` and `**Layer:**`
    // lines between the page title and its first H2. A page with no single title has no block
    // position, so it reads as unscopable here and the title check owns the defect. Fenced
    // lines are examples, never the block.
    /// <summary>Reads the merged scope fields declared under a page title.</summary>
    /// <param name="reader">The page read.</param>
    /// <returns>The scope fields in first-seen order, or none when the page declares none.</returns>
    public static IReadOnlyList<ScopeField> ReadPageScope(PageReader reader)
    {
        var titles = reader.Headings.Where(heading => heading.Level == 1).ToList();
        if (titles.Count != 1)
        {
            return [];
        }

        var end = reader.Headings
            .Where(heading => heading.Level == 2 && heading.Line > titles[0].Line)
            .Select(heading => heading.Line)
            .Append(reader.Lines.Length + 1)
            .Min();
        var merged = new List<ScopeField>();
        var values = new List<List<string>>();
        for (var number = titles[0].Line + 1; number < end; number++)
        {
            if (reader.FencedLines.Contains(number) || !PageReader.TryParseScopeLine(reader.Lines[number - 1], out var rest))
            {
                continue;
            }

            foreach (var field in ParseScopeFields(rest))
            {
                var index = merged.FindIndex(entry => entry.Name == field.Name);
                if (index < 0)
                {
                    var collected = new List<string>();
                    merged.Add(new ScopeField(field.Name, collected));
                    values.Add(collected);
                    index = merged.Count - 1;
                }

                values[index].AddRange(field.Values);
            }
        }

        return merged;
    }

    /// <summary>Names the vocabulary field a section title opens, or null when it opens none.</summary>
    /// <param name="title">The section title.</param>
    /// <returns>The field name, or null.</returns>
    static string? FieldSection(string title)
    {
        var first = title.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        return first.ToLowerInvariant() switch
        {
            "stack" => "stack",
            "layer" => "layer",
            "artifact" => "artifact",
            "conformance" => "conformance",
            _ => null,
        };
    }

    // One raw table: the per-field Value tables under their H3 sections, and the overview table
    // whose first header is Field. Per-field cells hold one value each, backticked as written;
    // overview Values cells hold backticked spans inside prose, so only the spans read as values.
    // An artifact cell holding a placeholder, such as `record:<type>`, declares a prefix.
    /// <summary>Reads one raw table into the vocabulary sets.</summary>
    /// <param name="table">The consecutive table lines.</param>
    /// <param name="section">The vocabulary section holding the table, or null.</param>
    /// <param name="stacks">The stack values to add to.</param>
    /// <param name="layers">The layer values to add to.</param>
    /// <param name="artifacts">The artifact values to add to.</param>
    /// <param name="prefixes">The artifact prefixes to add to.</param>
    /// <param name="conformances">The conformance level values to add to.</param>
    static void ReadTable(
        List<string> table,
        string? section,
        HashSet<string> stacks,
        HashSet<string> layers,
        HashSet<string> artifacts,
        List<string> prefixes,
        HashSet<string> conformances)
    {
        if (table.Count == 0)
        {
            return;
        }

        var rows = table.Select(SplitRow).ToList();
        var first = 1;
        if (rows.Count > 1 && IsSeparator(table[1]))
        {
            first = 2;
        }

        if (Unquote(rows[0].FirstOrDefault() ?? "").Equals("field", StringComparison.OrdinalIgnoreCase))
        {
            var valuesColumn = rows[0].FindIndex(cell => Unquote(cell).Equals("values", StringComparison.OrdinalIgnoreCase));
            if (valuesColumn < 0)
            {
                valuesColumn = rows[0].FindIndex(cell => Unquote(cell).Equals("value", StringComparison.OrdinalIgnoreCase));
            }

            if (valuesColumn < 0)
            {
                valuesColumn = rows[0].Count - 1;
            }

            for (var index = first; index < rows.Count; index++)
            {
                if (valuesColumn >= rows[index].Count)
                {
                    continue;
                }

                var field = Unquote(rows[index][0]).ToLowerInvariant();
                var target = field switch
                {
                    "stack" => stacks,
                    "layer" => layers,
                    "artifact" => artifacts,
                    "conformance" => conformances,
                    _ => null,
                };
                if (target is null)
                {
                    continue;
                }

                foreach (var span in BacktickSpans(rows[index][valuesColumn]))
                {
                    AddArtifactValue(span, field == "artifact", target, prefixes);
                }
            }

            return;
        }

        var fields = section switch
        {
            "stack" => stacks,
            "layer" => layers,
            "artifact" => artifacts,
            "conformance" => conformances,
            _ => null,
        };
        if (fields is null)
        {
            return;
        }

        for (var index = first; index < rows.Count; index++)
        {
            if (rows[index].Count == 0)
            {
                continue;
            }

            var spans = BacktickSpans(rows[index][0]);
            if (spans.Count > 0)
            {
                foreach (var span in spans)
                {
                    AddArtifactValue(span, section == "artifact", fields, prefixes);
                }
            }
            else
            {
                var raw = rows[index][0].Trim();
                if (raw.Length > 0)
                {
                    AddArtifactValue(raw, section == "artifact", fields, prefixes);
                }
            }
        }
    }

    /// <summary>Adds one table value to its set, reading a placeholder as an artifact prefix.</summary>
    /// <param name="value">The table value.</param>
    /// <param name="artifact">Whether the value sits in the artifact vocabulary.</param>
    /// <param name="target">The value set to add to.</param>
    /// <param name="prefixes">The artifact prefixes to add to.</param>
    static void AddArtifactValue(string value, bool artifact, HashSet<string> target, List<string> prefixes)
    {
        var text = value.Trim();
        if (text.Length == 0)
        {
            return;
        }

        var placeholder = text.IndexOf('<');
        if (artifact && placeholder > 0 && text.EndsWith('>'))
        {
            var prefix = text[..placeholder];
            if (prefix.Length > 0 && !prefixes.Contains(prefix))
            {
                prefixes.Add(prefix);
            }

            return;
        }

        target.Add(text);
    }

    /// <summary>Splits one raw table line into its trimmed cells.</summary>
    /// <param name="line">The raw table line.</param>
    /// <returns>The trimmed cells.</returns>
    static List<string> SplitRow(string line)
    {
        var cells = line.Split('|').Select(cell => cell.Trim()).ToList();
        if (cells.Count > 0 && cells[0].Length == 0)
        {
            cells.RemoveAt(0);
        }

        if (cells.Count > 0 && cells[^1].Length == 0)
        {
            cells.RemoveAt(cells.Count - 1);
        }

        return cells;
    }

    /// <summary>Reports whether a raw table line is its separator line.</summary>
    /// <param name="line">The raw table line.</param>
    /// <returns>True when the line is a separator line.</returns>
    static bool IsSeparator(string line) => line.Trim().Trim('|', ' ', '\t', ':', '-').Length == 0;

    /// <summary>Reads the backticked spans of one table cell.</summary>
    /// <param name="cell">The table cell.</param>
    /// <returns>The span texts in order.</returns>
    static List<string> BacktickSpans(string cell) =>
        BacktickSpan.Matches(cell).Cast<Match>().Select(match => match.Groups[1].Value.Trim()).Where(span => span.Length > 0).ToList();
}
