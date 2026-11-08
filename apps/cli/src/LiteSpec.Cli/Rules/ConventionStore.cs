using LiteSpec.Model;

namespace LiteSpec.Rules;

/// <summary>
///     The project conventions: provision-shaped Markdown files under
///     <c>.litespec/governance/conventions/</c>, each with an identifier,
///     a strength, a scope glob, and a default.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal sealed class ConventionStore
{
    /// <summary>Loads every convention under the consumer's record roots.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <returns>The conventions in identifier order; a file without an identifier or default is skipped.</returns>
    public static IReadOnlyList<ProvisionEntry> Load(ConsumerConfig config)
    {
        var entries = new List<ProvisionEntry>();

        foreach (var root in config.RecordRoots)
        {
            var folder = Path.Combine(root, "governance", "conventions");

            if (!Directory.Exists(folder))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(folder, "*.md", SearchOption.AllDirectories).OrderBy(name => name, StringComparer.Ordinal))
            {
                if (Read(file) is { } entry)
                {
                    entries.Add(entry);
                }
            }
        }

        entries.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
        return entries;
    }

    /// <summary>Reads one convention file.</summary>
    /// <param name="file">The convention file.</param>
    /// <returns>The entry, or null when the file holds no identifier or default.</returns>
    static ProvisionEntry? Read(string file)
    {
        string text;

        try
        {
            text = File.ReadAllText(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        var lines = text.Split('\n');
        var title = "";
        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        string? label = null;
        var value = new List<string>();

        void Flush()
        {
            if (label is not null && !labels.ContainsKey(label))
            {
                labels[label] = Collapse(string.Join("\n", value));
            }

            label = null;
            value.Clear();
        }

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            if (title.Length == 0 && line.StartsWith("# ", StringComparison.Ordinal))
            {
                title = line[2..].Trim();
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
            else if (label is not null && line.Length > 0 && !line.StartsWith("```", StringComparison.Ordinal))
            {
                value.Add(line);
            }
        }

        Flush();

        var id = Strip(labels.GetValueOrDefault("ID", ""));
        var statement = labels.GetValueOrDefault("Default", "");

        if (id.Length == 0 || statement.Length == 0)
        {
            return null;
        }

        if (title.Length == 0)
        {
            title = id;
        }

        return new ProvisionEntry(
            id,
            title,
            "",
            "convention",
            labels.GetValueOrDefault("Strength", "").Trim().Trim('`').Trim().ToLowerInvariant() switch
            {
                "rule" or "required" => "rule",
                "advice" or "advisory" => "advice",
                _ => "default",
            },
            "",
            Strip(labels.GetValueOrDefault("Applies to", "")),
            statement,
            labels.GetValueOrDefault("Rationale", ""),
            labels.GetValueOrDefault("Example", ""),
            labels.GetValueOrDefault("Reviewed for", labels.GetValueOrDefault("Checked by", "")),
            ProvisionEntry.ConventionSource,
            "",
            labels.ContainsKey("Checked by") ? "checkable" : "guidance");
    }

    /// <summary>Strips backticks and whitespace from a convention field.</summary>
    /// <param name="value">The raw value.</param>
    /// <returns>The stripped value.</returns>
    static string Strip(string value) => value.Trim().Trim('`').Trim();

    /// <summary>Collapses whitespace runs to single spaces.</summary>
    /// <param name="value">The raw value.</param>
    /// <returns>The collapsed value.</returns>
    static string Collapse(string value) =>
        string.Join(" ", value.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
}
