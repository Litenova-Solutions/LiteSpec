using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Rules;

/// <summary>
///     The provisions the consumer's selection applies: every provision on an applied
///     page except the provisions a selected extension replaces. The catalog prefers
///     the generated <c>docs/reference/provisions.json</c> beside the carried release
///     and parses the pages themselves when it is missing or broken.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal sealed class ProvisionCatalog
{
    /// <summary>Initializes a new instance of the <see cref="ProvisionCatalog" /> class.</summary>
    /// <param name="entries">The applied provisions in identifier order.</param>
    ProvisionCatalog(IReadOnlyList<ProvisionEntry> entries) => Entries = entries;

    /// <summary>Gets the applied provisions in identifier order.</summary>
    public IReadOnlyList<ProvisionEntry> Entries { get; }

    /// <summary>The generated provisions file inside the carried release.</summary>
    public const string JsonFile = "docs/reference/provisions.json";

    /// <summary>Loads the catalog for the consumer's selection.</summary>
    /// <param name="home">The carried LiteSpec release root.</param>
    /// <param name="manifest">The release manifest object.</param>
    /// <param name="shape">The shape the project names.</param>
    /// <param name="stacks">The stacks the project selects.</param>
    /// <param name="extensions">The extensions the project selects.</param>
    /// <returns>The catalog of applied provisions.</returns>
    public static ProvisionCatalog Load(
        string home, JsonObject manifest, string shape, IReadOnlyList<string> stacks, IReadOnlyList<string> extensions)
    {
        var applied = new HashSet<string>(StackSelection.AppliedPages(home, manifest, shape, stacks, extensions), StringComparer.Ordinal);
        var replaced = new HashSet<string>(StackSelection.ReplacedProvisions(manifest, extensions), StringComparer.Ordinal);

        if (LoadJson(home) is { Count: > 0 } json)
        {
            var kept = json
                .Where(entry => applied.Contains(entry.Page) && !replaced.Contains(entry.Id))
                .OrderBy(entry => entry.Id, StringComparer.Ordinal)
                .ToList();

            if (kept.Count > 0)
            {
                return new ProvisionCatalog(kept);
            }
        }

        var rows = Provisions.Collect(home)
            .Where(row => applied.Contains(row.Page) && !replaced.Contains(row.Id))
            .ToList();
        var entries = new List<ProvisionEntry>();

        foreach (var group in rows.GroupBy(row => row.Page, StringComparer.Ordinal))
        {
            var file = Path.Combine(home, group.Key.Replace('/', Path.DirectorySeparatorChar));
            var fields = ReadPageFields(file);

            foreach (var row in group)
            {
                fields.TryGetValue(row.Id, out var found);
                found ??= new Dictionary<string, string>(StringComparer.Ordinal);
                entries.Add(EntryOf(row, fields[""], found));
            }
        }

        entries.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
        return new ProvisionCatalog(entries);
    }

    /// <summary>One page's provision fields by provision identifier, with the page concern under the empty key.</summary>
    /// <param name="file">The page file.</param>
    /// <returns>The fields by provision identifier.</returns>
    static Dictionary<string, Dictionary<string, string>> ReadPageFields(string file)
    {
        var fields = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal)
        {
            [""] = new(StringComparer.Ordinal),
        };

        if (!File.Exists(file))
        {
            return fields;
        }

        var lines = File.ReadAllLines(file);

        foreach (var line in lines)
        {
            if (line.StartsWith("**Concern:**", StringComparison.Ordinal))
            {
                fields[""][""] = NormalizeConcern(line["**Concern:**".Length..]);
                break;
            }
        }

        var fenced = new HashSet<int>();
        var fencedOpen = false;

        for (var number = 0; number < lines.Length; number++)
        {
            if (lines[number].StartsWith("```", StringComparison.Ordinal))
            {
                fencedOpen = !fencedOpen;
            }

            if (fencedOpen)
            {
                fenced.Add(number);
            }
        }

        for (var index = 0; index < lines.Length; index++)
        {
            if (fenced.Contains(index) || !lines[index].StartsWith("### ", StringComparison.Ordinal))
            {
                continue;
            }

            var id = "";
            var cursor = index + 1;

            while (cursor < lines.Length && (fenced.Contains(cursor) || lines[cursor].Trim().Length == 0))
            {
                cursor++;
            }

            if (cursor < lines.Length && PageReader.TryParseIdLine(lines[cursor].Trim(), out var stated))
            {
                id = stated;
            }

            if (id.Length == 0)
            {
                continue;
            }

            var labels = new Dictionary<string, string>(StringComparer.Ordinal);
            string? label = null;
            var value = new List<string>();
            var inFence = false;

            void Flush()
            {
                if (label is not null && !labels.ContainsKey(label))
                {
                    labels[label] = Collapse(string.Join("\n", value));
                }

                label = null;
                value.Clear();
            }

            cursor++;

            for (; cursor < lines.Length; cursor++)
            {
                var line = lines[cursor];

                if (line.StartsWith('#'))
                {
                    break;
                }

                if (line.StartsWith("```", StringComparison.Ordinal))
                {
                    inFence = !inFence;
                    continue;
                }

                if (inFence)
                {
                    continue;
                }

                if (line.StartsWith("**", StringComparison.Ordinal) && line.Contains(":**", StringComparison.Ordinal))
                {
                    Flush();
                    var end = line.IndexOf(":**", StringComparison.Ordinal);
                    label = line[2..end];
                    var rest = line[(end + 3)..].Trim();

                    if (rest.Length > 0)
                    {
                        value.Add(rest);
                    }
                }
                else if (label is not null && line.Trim().Length > 0)
                {
                    value.Add(line.Trim());
                }
            }

            Flush();
            fields[id] = labels;
            index = cursor - 1;
        }

        return fields;
    }

    /// <summary>One catalog entry from its index row and its parsed fields.</summary>
    /// <param name="row">The provision index row.</param>
    /// <param name="page">The page fields, with the concern under the empty key.</param>
    /// <param name="found">The provision fields by label.</param>
    /// <returns>The catalog entry.</returns>
    static ProvisionEntry EntryOf(ProvisionRow row, Dictionary<string, string> page, Dictionary<string, string> found)
    {
        var strength = NormalizeStrength(found.GetValueOrDefault("Strength", ""));
        var appliesTo = ParseScope(found.GetValueOrDefault("Applies to", ""));

        // The page file alone does not state the record layer, so a fallback entry governs every claim.
        // The generated index holds the layer and trace, and every normal read uses it.
        var mark = row.Mark.Trim().ToLowerInvariant();

        if (mark.Length == 0)
        {
            mark = found.ContainsKey("Checked by") ? "checkable" : found.ContainsKey("Reviewed for") ? "guidance" : "";
        }

        var statement = found.GetValueOrDefault("Statement", "");

        if (statement.Length == 0)
        {
            statement = found.GetValueOrDefault("Default", "");
        }

        if (statement.Length == 0)
        {
            statement = found.GetValueOrDefault("Question", "");
        }

        if (statement.Length == 0)
        {
            statement = row.Heading;
        }

        var reviewedFor = found.GetValueOrDefault("Reviewed for", "");

        if (reviewedFor.Length == 0)
        {
            reviewedFor = found.GetValueOrDefault("Checked by", row.Evidence);
        }

        return new ProvisionEntry(
            row.Id,
            row.Heading,
            row.Page,
            row.Kind,
            strength.Length > 0 ? strength : row.Kind switch
            {
                "rule" => "required",
                "convention" => "default",
                _ => "pattern",
            },
            page.GetValueOrDefault("", ""),
            LayerOf(row.Layer),
            row.Trace,
            appliesTo,
            statement,
            found.GetValueOrDefault("Rationale", ""),
            found.GetValueOrDefault("Example", ""),
            reviewedFor,
            ProvisionEntry.LiteSpecSource,
            mark);
    }

    /// <summary>Reads the generated provisions file, or null when it is missing or broken.</summary>
    /// <param name="home">The carried LiteSpec release root.</param>
    /// <returns>The entries the file holds, or null.</returns>
    static List<ProvisionEntry>? LoadJson(string home)
    {
        var file = Path.Combine(home, JsonFile.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(file))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            var items = document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.EnumerateArray()
                : document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("provisions", out var list)
                    && list.ValueKind == JsonValueKind.Array
                        ? list.EnumerateArray()
                        : Enumerable.Empty<JsonElement>();
            var entries = new List<ProvisionEntry>();

            foreach (var item in items)
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var id = Text(item, "id");

                if (!Provisions.IsProvisionId(id))
                {
                    continue;
                }

                var page = Text(item, "page");

                if (page.Length == 0)
                {
                    continue;
                }

                var kind = Text(item, "kind");
                var strength = NormalizeStrength(Text(item, "strength"));

                entries.Add(new ProvisionEntry(
                    id,
                    Text(item, "heading"),
                    page,
                    kind.Length > 0 ? kind : "rule",
                    strength.Length > 0 ? strength : kind switch
                    {
                        "convention" => "default",
                        "pattern" => "pattern",
                        _ => "rule",
                    },
                    NormalizeConcern(Text(item, "concern")),
                    LayerOf(Text(item, "layer")),
                    item.TryGetProperty("trace", out var trace) && trace.ValueKind == JsonValueKind.True,
                    NormalizeScope(Text(item, "artifact")),
                    Text(item, "statement"),
                    Text(item, "rationale"),
                    Text(item, "example"),
                    Text(item, "evidence"),
                    ProvisionEntry.LiteSpecSource,
                    Text(item, "mark").Trim().ToLowerInvariant()));
            }

            return entries;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>One string property of a JSON object, or empty when it holds none.</summary>
    /// <param name="item">The JSON object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The property text, or empty.</returns>
    static string Text(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    /// <summary>Normalizes a concern value: backticks stripped, trimmed, lowercased.</summary>
    /// <param name="value">The raw value.</param>
    /// <returns>The normalized concern, such as domain or tests.</returns>
    static string NormalizeConcern(string value) => value.Trim().Trim('`').Trim().ToLowerInvariant();

    /// <summary>Reads a record layer name, any when it names none or an unknown layer.</summary>
    /// <param name="value">The raw value.</param>
    /// <returns>The record layer.</returns>
    static RecordLayer LayerOf(string value) =>
        LayerClaims.TryParse(value.Trim().Trim('`').Trim().ToLowerInvariant(), out var layer) ? layer : RecordLayer.Any;

    /// <summary>Normalizes a strength value to required, default or advisory, or empty.</summary>
    /// <param name="value">The raw value.</param>
    /// <returns>The normalized strength.</returns>
    static string NormalizeStrength(string value) => value.Trim().Trim('`').Trim().ToLowerInvariant() switch
    {
        "required" => "required",
        "default" => "default",
        "advisory" => "advisory",
        _ => "",
    };

    /// <summary>Normalizes an Applies to value to its artifact, or empty.</summary>
    /// <param name="value">The raw value.</param>
    /// <returns>The normalized artifact.</returns>
    static string NormalizeScope(string value) => ParseScope(value);

    /// <summary>Reads the artifact field of an Applies to value.</summary>
    /// <param name="value">The raw value.</param>
    /// <returns>The normalized artifact, or empty.</returns>
    static string ParseScope(string value)
    {
        var text = value.Trim().Trim('`').Trim();

        if (text.Length == 0)
        {
            return "";
        }

        var artifact = "";

        foreach (var part in text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var segment = part.Trim().Trim('`').Trim();

            if (segment.StartsWith("artifact:", StringComparison.OrdinalIgnoreCase))
            {
                artifact = StripArtifact(segment["artifact:".Length..]);
            }
            else if (segment.StartsWith("record:", StringComparison.OrdinalIgnoreCase))
            {
                artifact = StripArtifact(segment);
            }
            else if (artifact.Length == 0 && !segment.Contains(':', StringComparison.Ordinal))
            {
                artifact = segment.ToLowerInvariant();
            }
        }

        if (artifact.Length == 0 && !text.Contains(';', StringComparison.Ordinal))
        {
            artifact = StripArtifact(text);
        }

        return artifact;
    }

    /// <summary>Strips the artifact and record prefixes from an artifact value.</summary>
    /// <param name="value">The raw value.</param>
    /// <returns>The normalized artifact.</returns>
    static string StripArtifact(string value)
    {
        var text = value.Trim().Trim('`').Trim();

        if (text.StartsWith("artifact:", StringComparison.OrdinalIgnoreCase))
        {
            text = text["artifact:".Length..].Trim().Trim('`').Trim();
        }

        if (text.StartsWith("record:", StringComparison.OrdinalIgnoreCase))
        {
            text = text["record:".Length..].Trim().Trim('`').Trim();
        }

        return text.ToLowerInvariant();
    }

    /// <summary>Collapses whitespace runs to single spaces.</summary>
    /// <param name="value">The raw value.</param>
    /// <returns>The collapsed value.</returns>
    static string Collapse(string value) =>
        string.Join(" ", value.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
}
