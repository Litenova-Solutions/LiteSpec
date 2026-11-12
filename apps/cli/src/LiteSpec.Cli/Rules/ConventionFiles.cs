using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Rules;

/// <summary>
///     The project conventions: provision-shaped Markdown files under
///     <c>.litespec/governance/conventions/</c>, each with an identifier,
///     a strength, a scope glob, and a default.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal sealed class ConventionFiles
{
    /// <summary>One loaded convention with where its scope is stated.</summary>
    /// <param name="File">The convention file.</param>
    /// <param name="AppliesToLine">The 1-based line of its Applies to label, or 1 when it states none.</param>
    /// <param name="Entry">The convention entry.</param>
    internal sealed record ConventionFile(string File, int AppliesToLine, ProvisionEntry Entry);

    /// <summary>Whether the path sits in a conventions folder, <c>governance/conventions/</c>, where rules reads project conventions.</summary>
    /// <param name="path">A full or relative file path.</param>
    /// <returns>True when the path sits in a conventions folder; otherwise false.</returns>
    internal static bool InConventionsFolder(string path) =>
        path.Replace('\\', '/').Contains("governance/conventions/", StringComparison.OrdinalIgnoreCase);

    /// <summary>The scope keys a convention Applies to may name, as provisions write them.</summary>
   static readonly HashSet<string> ScopeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "stack", "concern", "artifact", "layer", "trace",
    };

    /// <summary>Loads every convention under the consumer's record roots.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <returns>The conventions in identifier order; a file without an identifier or default is skipped.</returns>
    public static IReadOnlyList<ProvisionEntry> Load(ConsumerConfig config) =>
        [.. ReadAll(config).Select(page => page.Entry)];

    /// <summary>Loads every convention under the consumer's record roots with its file.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <returns>The conventions in identifier order, each with its file; a file without an identifier or default is skipped.</returns>
    public static IReadOnlyList<ConventionFile> LoadFiles(ConsumerConfig config) =>
        ReadAll(config);

    /// <summary>Whether the Applies to text is scope vocabulary: scope keys with values, as provisions write them.</summary>
    /// <param name="appliesTo">The convention Applies to text.</param>
    /// <returns>True when every field names a scope key with a value; otherwise false.</returns>
    public static bool IsScopeForm(string appliesTo)
    {
        var text = appliesTo.Trim();

        if (text.Length == 0 || !text.Contains(':'))
        {
            return false;
        }

        var first = text.Split(',', ';')[0];

        if (!first.Contains(':'))
        {
            return false;
        }

        var fields = Scopes.ParseScopeFields(text);

        if (fields.Count == 0)
        {
            return false;
        }

        foreach (var field in fields)
        {
            if (!ScopeKeys.Contains(field.Name) || field.Values.Count == 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Whether the Applies to text is a path glob: one token holding a path mark rules matches against paths.</summary>
    /// <param name="appliesTo">The convention Applies to text.</param>
    /// <returns>True when the text is one token with no scope separator holding a slash, star, question mark or dot; otherwise false.</returns>
    public static bool IsGlobForm(string appliesTo)
    {
        var text = appliesTo.Trim();

        if (text.Length == 0)
        {
            return false;
        }

        foreach (var character in text)
        {
            if (char.IsWhiteSpace(character) || character is ':' or ',' or ';')
            {
                return false;
            }
        }

        return text.Contains('/') || text.Contains('*') || text.Contains('?') || text.Contains('.');
    }

    /// <summary>
    ///     Whether a scope-form Applies to governs the target: every stated field matches.
    ///     A stated stack or concern equals the target's, a stated artifact holds the
    ///     target artifact or a record the target marks, and <c>artifact: any</c> holds
    ///     every target of its stated concern, or every target when no concern is stated.
    ///     Layer and trace are accepted but narrow nothing: the target names no layer.
    /// </summary>
    /// <param name="appliesTo">The convention Applies to text in scope form.</param>
    /// <param name="stack">The target stack, or empty when the target names none.</param>
    /// <param name="concern">The target concern, or empty when the target names none.</param>
    /// <param name="artifact">The target artifact, or empty when the target names none.</param>
    /// <param name="marked">The record identifiers the target marks, in file order.</param>
    /// <returns>True when every stated field matches the target; false when the text is no scope form or a field misses.</returns>
    public static bool MatchesScope(string appliesTo, string stack, string concern, string artifact, IReadOnlyList<string> marked)
    {
        if (!IsScopeForm(appliesTo))
        {
            return false;
        }

        foreach (var field in Scopes.ParseScopeFields(appliesTo.Trim()))
        {
            if (field.Name.Equals("stack", StringComparison.OrdinalIgnoreCase))
            {
                if (!field.Values.Contains(stack, StringComparer.Ordinal))
                {
                    return false;
                }
            }
            else if (field.Name.Equals("concern", StringComparison.OrdinalIgnoreCase))
            {
                if (!field.Values.Contains(concern, StringComparer.Ordinal))
                {
                    return false;
                }
            }
            else if (field.Name.Equals("artifact", StringComparison.OrdinalIgnoreCase))
            {
                if (!ArtifactMatches(field.Values, artifact, marked))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>Whether the stated artifact values hold the target: its artifact, a marked record of a stated record type, or any.</summary>
    /// <param name="values">The stated artifact values.</param>
    /// <param name="artifact">The target artifact, or empty when the target names none.</param>
    /// <param name="marked">The record identifiers the target marks, in file order.</param>
    /// <returns>True when a value holds the target; otherwise false.</returns>
    static bool ArtifactMatches(IReadOnlyList<string> values, string artifact, IReadOnlyList<string> marked)
    {
        if (values.All(Scopes.IsConcernWide))
        {
            return true;
        }

        foreach (var value in values)
        {
            if (Scopes.IsConcernWide(value))
            {
                continue;
            }

            if (value.StartsWith("record:", StringComparison.Ordinal))
            {
                var type = value["record:".Length..];

                if (type.Length > 0 && marked.Any(id => id.StartsWith(type + "/", StringComparison.Ordinal)))
                {
                    return true;
                }
            }
            else if (value.Equals(artifact, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Reads every convention under the consumer's record roots.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <returns>The conventions in identifier order; a file without an identifier or default is skipped.</returns>
    static List<ConventionFile> ReadAll(ConsumerConfig config)
    {
        var pages = new List<ConventionFile>();

        foreach (var root in config.RecordRoots)
        {
            var folder = Path.Combine(root, "governance", "conventions");

            if (!Directory.Exists(folder))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(folder, "*.md", SearchOption.AllDirectories).OrderBy(name => name, StringComparer.Ordinal))
            {
                if (Read(file) is { } page)
                {
                    pages.Add(page);
                }
            }
        }

        pages.Sort((left, right) => string.CompareOrdinal(left.Entry.Id, right.Entry.Id));
        return pages;
    }

    /// <summary>Reads one convention file.</summary>
    /// <param name="file">The convention file.</param>
    /// <returns>The convention with its file, or null when the file holds no identifier or default.</returns>
    static ConventionFile? Read(string file)
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
        var appliesToLine = 0;
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

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();

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

                if (label == "Applies to" && appliesToLine == 0)
                {
                    appliesToLine = index + 1;
                }

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

        var entry = new ProvisionEntry(
            id,
            title,
            "",
            "convention",
            labels.GetValueOrDefault("Strength", "").Trim().Trim('`').Trim().ToLowerInvariant() switch
            {
                "required" => "required",
                "advisory" => "advisory",
                _ => "default",
            },
            "",
            RecordLayer.Any,
            false,
            Strip(labels.GetValueOrDefault("Applies to", "")),
            statement,
            labels.GetValueOrDefault("Rationale", ""),
            labels.GetValueOrDefault("Example", ""),
            labels.GetValueOrDefault("Reviewed for", labels.GetValueOrDefault("Checked by", "")),
            ProvisionEntry.ConventionSource,
            labels.ContainsKey("Checked by") ? "checkable" : "guidance");

        return new ConventionFile(file, appliesToLine == 0 ? 1 : appliesToLine, entry);
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
