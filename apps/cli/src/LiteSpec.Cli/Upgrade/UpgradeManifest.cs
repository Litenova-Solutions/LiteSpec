using System.Text.Json.Nodes;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Upgrade;

/// <summary>
///     The structured release changes an upgrade reports: the embedded
///     <c>migrations/&lt;version&gt;.yaml</c> manifests, read from the tool assembly
///     so the report works offline and matches the carried release.
/// </summary>
/// <follows>litespec/core/authoring#record-the-reviewed-litespec-release</follows>
static class UpgradeManifest
{
    /// <summary>One schema field rename between the reviewed and the carried release.</summary>
    /// <param name="From">The removed field name.</param>
    /// <param name="To">The replacement field name, or null when the field is removed.</param>
    /// <param name="RecordTypes">The record types carrying the field, as the manifest names them.</param>
    /// <param name="Note">Why the field moved, in one sentence.</param>
    public sealed record FieldRename(string From, string? To, IReadOnlyList<string> RecordTypes, string Note);

    /// <summary>One finding code rename between the reviewed and the carried release.</summary>
    /// <param name="From">The retired code.</param>
    /// <param name="To">The replacement code.</param>
    public sealed record CodeRename(string From, string To);

    /// <summary>One retired command between the reviewed and the carried release.</summary>
    /// <param name="From">The retired command.</param>
    /// <param name="To">The replacement command, or null when nothing replaces it.</param>
    public sealed record CommandChange(string From, string? To);

    /// <summary>One release of structured changes between the reviewed and the carried release.</summary>
    /// <param name="Version">The release version the changes ship in.</param>
    /// <param name="Summary">What the release changes, in one sentence.</param>
    /// <param name="FieldRenames">The schema field renames.</param>
    /// <param name="FindingRenames">The renamed finding codes.</param>
    /// <param name="FindingAdded">The new finding codes.</param>
    /// <param name="FindingRemoved">The removed finding codes.</param>
    /// <param name="AnalyzerAdded">The new analyzer IDs.</param>
    /// <param name="AnalyzerChanged">The analyzer IDs with changed behavior.</param>
    /// <param name="AnalyzerRemoved">The removed analyzer IDs.</param>
    /// <param name="CommandsRetired">The retired commands.</param>
    /// <param name="ExtensionsFolded">The extensions folded into Core.</param>
    /// <param name="ExtensionsRetired">The removed extensions.</param>
    /// <param name="StacksRemoved">The removed stacks.</param>
    /// <param name="PackChanges">The pack changes in plain sentences.</param>
    public sealed record Migration(
        string Version,
        string Summary,
        IReadOnlyList<FieldRename> FieldRenames,
        IReadOnlyList<CodeRename> FindingRenames,
        IReadOnlyList<string> FindingAdded,
        IReadOnlyList<string> FindingRemoved,
        IReadOnlyList<string> AnalyzerAdded,
        IReadOnlyList<string> AnalyzerChanged,
        IReadOnlyList<string> AnalyzerRemoved,
        IReadOnlyList<CommandChange> CommandsRetired,
        IReadOnlyList<string> ExtensionsFolded,
        IReadOnlyList<string> ExtensionsRetired,
        IReadOnlyList<string> StacksRemoved,
        IReadOnlyList<string> PackChanges);

    /// <summary>The resource prefix of the embedded migration manifests.</summary>
    const string Prefix = "LiteSpec.Migrations.";

    /// <summary>The resource suffix of the embedded migration manifests.</summary>
    const string Suffix = ".yaml";

    /// <summary>Loads every embedded migration manifest, ordered by version.</summary>
    /// <returns>The migrations, or the read error when a manifest cannot be read.</returns>
    public static (IReadOnlyList<Migration> Migrations, string? Error) Load()
    {
        var assembly = typeof(UpgradeManifest).Assembly;
        var names = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal)
                && name.EndsWith(Suffix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var migrations = new List<Migration>();

        foreach (var name in names)
        {
            var version = name[Prefix.Length..^Suffix.Length];
            string text;

            using (var stream = assembly.GetManifestResourceStream(name))
            {
                if (stream is null)
                {
                    return ([], $"The embedded migration '{name}' cannot be read.");
                }

                using var reader = new StreamReader(stream);
                text = reader.ReadToEnd();
            }

            JsonNode? node;

            try
            {
                node = RecordText.Parse(text, yaml: true);
            }
            catch (LiteSpec.Model.JsonTextException exception)
            {
                return ([], $"The embedded migration '{name}' is not valid YAML: {exception.Message}");
            }

            if (node is not JsonObject manifest)
            {
                return ([], $"The embedded migration '{name}' holds no mapping.");
            }

            migrations.Add(ReadMigration(version, manifest));
        }

        migrations.Sort((left, right) => CompareVersions(left.Version, right.Version));
        return (migrations, null);
    }

    /// <summary>Compares two release versions by their base version, falling back to text order.</summary>
    /// <param name="left">The first version.</param>
    /// <param name="right">The second version.</param>
    /// <returns>The comparison of the two versions.</returns>
    public static int CompareVersions(string left, string right)
    {
        if (TryBase(left, out var leftBase) && TryBase(right, out var rightBase))
        {
            return leftBase.CompareTo(rightBase);
        }

        return string.CompareOrdinal(left, right);
    }

    /// <summary>The base version of a version: the part before any prerelease suffix.</summary>
    /// <param name="text">A version such as 7.0.0 or 6.0.0-rc.1.</param>
    /// <param name="version">The parsed base version.</param>
    /// <returns>True when the base version parses.</returns>
    public static bool TryBase(string text, out Version version)
    {
        var dash = text.IndexOf('-', StringComparison.Ordinal);
        var bare = (dash < 0 ? text : text[..dash]).Trim();

        if (Version.TryParse(bare, out var parsed))
        {
            version = parsed;
            return true;
        }

        version = new Version(0, 0);
        return false;
    }

    /// <summary>Reads one migration manifest object.</summary>
    /// <param name="version">The release version from the resource name.</param>
    /// <param name="manifest">The parsed manifest object.</param>
    /// <returns>The migration the manifest states.</returns>
    static Migration ReadMigration(string version, JsonObject manifest) =>
        new(
            version,
            StringOf(manifest, "summary"),
            FieldRenamesOf(manifest),
            RenamesOf(manifest, "findingCodes", "renamed"),
            StringsOf(ChildOf(manifest, "findingCodes"), "added"),
            StringsOf(ChildOf(manifest, "findingCodes"), "removed"),
            StringsOf(ChildOf(manifest, "analyzers"), "added"),
            StringsOf(ChildOf(manifest, "analyzers"), "changed"),
            StringsOf(ChildOf(manifest, "analyzers"), "removed"),
            CommandsOf(manifest),
            StringsOf(manifest, "extensionsFolded"),
            StringsOf(manifest, "extensionsRetired"),
            StringsOf(manifest, "stacksRemoved"),
            StringsOf(manifest, "packChanges"));

    /// <summary>The schema field renames of a manifest.</summary>
    /// <param name="manifest">The parsed manifest object.</param>
    /// <returns>The field renames in manifest order.</returns>
    static List<FieldRename> FieldRenamesOf(JsonObject manifest)
    {
        var renames = new List<FieldRename>();

        if (manifest["fieldRenames"] is JsonArray entries)
        {
            foreach (var entry in entries.OfType<JsonObject>())
            {
                renames.Add(new FieldRename(
                    StringOf(entry, "from"),
                    entry["to"] is JsonValue ? StringOf(entry, "to") : null,
                    StringsOf(entry, "recordTypes"),
                    StringOf(entry, "note")));
            }
        }

        return renames;
    }

    /// <summary>The code renames of one manifest section, such as the finding code renames.</summary>
    /// <param name="manifest">The parsed manifest object.</param>
    /// <param name="section">The section field name.</param>
    /// <param name="field">The renames field name inside the section.</param>
    /// <returns>The renames in manifest order.</returns>
    static List<CodeRename> RenamesOf(JsonObject manifest, string section, string field)
    {
        var renames = new List<CodeRename>();

        if (ChildOf(manifest, section) is { } child && child[field] is JsonArray entries)
        {
            foreach (var entry in entries.OfType<JsonObject>())
            {
                renames.Add(new CodeRename(StringOf(entry, "from"), StringOf(entry, "to")));
            }
        }

        return renames;
    }

    /// <summary>The retired commands of a manifest.</summary>
    /// <param name="manifest">The parsed manifest object.</param>
    /// <returns>The retired commands in manifest order.</returns>
    static List<CommandChange> CommandsOf(JsonObject manifest)
    {
        var commands = new List<CommandChange>();

        if (manifest["commandsRetired"] is JsonArray entries)
        {
            foreach (var entry in entries.OfType<JsonObject>())
            {
                commands.Add(new CommandChange(
                    StringOf(entry, "from"),
                    entry["to"] is JsonValue ? StringOf(entry, "to") : null));
            }
        }

        return commands;
    }

    /// <summary>The string entries of a field: one string or an array of strings.</summary>
    /// <param name="parent">The object holding the field, or null.</param>
    /// <param name="field">The field name.</param>
    /// <returns>The string entries in order.</returns>
    static IReadOnlyList<string> StringsOf(JsonObject? parent, string field)
    {
        if (parent is null)
        {
            return [];
        }

        if (parent[field] is JsonValue single && single.TryGetValue<string>(out var text) && text.Length > 0)
        {
            return [text];
        }

        if (parent[field] is JsonArray entries)
        {
            return
            [
                .. entries
                    .OfType<JsonValue>()
                    .Select(entry => entry.TryGetValue<string>(out var item) ? item : null)
                    .OfType<string>()
                    .Where(item => item.Length > 0),
            ];
        }

        return [];
    }

    /// <summary>The object a field holds, or null when it holds none.</summary>
    /// <param name="parent">The object holding the field.</param>
    /// <param name="field">The field name.</param>
    /// <returns>The nested object, or null.</returns>
    static JsonObject? ChildOf(JsonObject parent, string field) =>
        parent[field] as JsonObject;

    /// <summary>The string a field holds, or empty when it holds none.</summary>
    /// <param name="parent">The object holding the field.</param>
    /// <param name="field">The field name.</param>
    /// <returns>The field string, or empty.</returns>
    static string StringOf(JsonObject parent, string field) =>
        parent[field] is JsonValue value && value.TryGetValue<string>(out var text) ? text : "";
}
