using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Skill;

/// <summary>
///     The LiteSpec skill files: the generator that renders the skill copies from the
///     carried release, the folders each agent reads, and the marker that tells a copy
///     this tool wrote from a folder it must leave alone.
/// </summary>
/// <follows>litespec/core/agent#keep-the-agent-protocol-current</follows>
static class SkillFiles
{
    /// <summary>The skill name: the folder name and the frontmatter name of every copy.</summary>
    internal const string SkillName = "litespec";

    /// <summary>The entry file of a copy, holding the frontmatter with the marker.</summary>
    internal const string FileName = "SKILL.md";

    /// <summary>The placeholder on its own line inside the template metadata, replaced with the release stamp.</summary>
    internal const string Placeholder = "__LITESPEC_RELEASE_METADATA__";

    /// <summary>The environment variable that points the global scope at another home folder.</summary>
    internal const string HomeVariable = "LITESPEC_SKILL_HOME";

    /// <summary>The marker value: every generated copy carries it as metadata.generated-by.</summary>
    internal const string GeneratedBy = "litespec";

    /// <summary>The suffix of the sibling staging folder a copy is written to before it replaces the target.</summary>
    internal const string StagingSuffix = ".new";

    /// <summary>The template files of one copy, as forward-slash paths under templates/skill/litespec/.</summary>
    static readonly string[] Sources = ["SKILL.md", "references/commands.md", "references/report.md"];

    /// <summary>The skill folder of each agent at project scope, as a forward-slash path under the consumer root.</summary>
    static readonly Dictionary<string, string> ProjectPaths = new(StringComparer.Ordinal)
    {
        ["claude"] = ".claude/skills/litespec",
        ["opencode"] = ".opencode/skills/litespec",
        ["agents"] = ".agents/skills/litespec",
    };

    /// <summary>The skill folder of each agent at global scope, as a forward-slash path under the home folder.</summary>
    static readonly Dictionary<string, string> GlobalPaths = new(StringComparer.Ordinal)
    {
        ["claude"] = ".claude/skills/litespec",
        ["opencode"] = ".config/opencode/skills/litespec",
        ["agents"] = ".agents/skills/litespec",
    };

    /// <summary>Gets the agents a skill command selects when --agent is absent.</summary>
    public static IReadOnlyList<string> DefaultAgents { get; } = ["agents", "claude"];

    /// <summary>Whether the name selects an agent the skill command installs for.</summary>
    /// <param name="name">The candidate agent name.</param>
    /// <returns>True when the name is claude, opencode, or agents; otherwise false.</returns>
    public static bool IsAgent(string name) => ProjectPaths.ContainsKey(name);

    /// <summary>
    ///     Renders every file of one skill copy from the release templates, stamping the
    ///     project copy with the release and leaving the global copy release-neutral.
    /// </summary>
    /// <param name="templatesRoot">The release templates folder that holds skill/litespec/.</param>
    /// <param name="release">The carried release for the project stamp, or null for a global copy.</param>
    /// <returns>Every file of the copy: its forward-slash path under the skill folder and its text.</returns>
    /// <exception cref="IOException">A template file is missing, or the entry file carries no placeholder line.</exception>
    /// <exception cref="UnauthorizedAccessException">A template file cannot be read.</exception>
    public static IReadOnlyList<(string RelativePath, string Text)> Render(string templatesRoot, string? release)
    {
        var files = new List<(string RelativePath, string Text)>(Sources.Length);

        foreach (var relative in Sources)
        {
            var source = Path.Combine(templatesRoot, "skill", SkillName, relative.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(source))
            {
                throw new IOException($"The LiteSpec release holds no readable templates/skill/{SkillName}/{relative}.");
            }

            var text = File.ReadAllText(source);

            if (relative == FileName)
            {
                text = Stamp(text, release);
            }

            files.Add((relative, text));
        }

        return files;
    }

    /// <summary>The sibling staging folder the copy is written to before it replaces the target.</summary>
    /// <param name="folder">The skill folder the staging folder stands beside.</param>
    /// <returns>The staging folder path.</returns>
    public static string StagingFolder(string folder) => folder + StagingSuffix;

    /// <summary>
    ///     Writes one skill copy to the folder through its staging folder, so a failure
    ///     never leaves a partial target: the copy is written complete to the staging
    ///     folder first, then the old folder is deleted and the staging folder is moved
    ///     into place. A leftover staging folder from a crashed run is removed first.
    /// </summary>
    /// <param name="folder">The skill folder to write.</param>
    /// <param name="files">Every file of the copy: its forward-slash path under the skill folder and its text.</param>
    /// <exception cref="IOException">A file cannot be written, or the target cannot be replaced.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to a file or the target folder is denied.</exception>
    public static void WriteCopy(string folder, IReadOnlyList<(string RelativePath, string Text)> files)
    {
        var staging = StagingFolder(folder);

        // A leftover staging folder from a crashed run is removed first, so the
        // fresh copy starts empty. A file at the staging path is left alone, so
        // the write below fails with the target untouched.
        if (Directory.Exists(staging))
        {
            Directory.Delete(staging, recursive: true);
        }

        foreach (var (relative, text) in files)
        {
            var target = Path.Combine(staging, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllText(target, text);
        }

        // The old copy is deleted only after the new copy is complete, then the
        // staging folder moves into place, so a failure never leaves a partial target.
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }

        Directory.Move(staging, folder);
    }

    /// <summary>The skill folder of the agent at project scope.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="agent">The agent name: claude, opencode, or agents.</param>
    /// <returns>The absolute skill folder under the consumer root.</returns>
    public static string ProjectFolder(string root, string agent) =>
        Path.Combine(root, ProjectPaths[agent].Replace('/', Path.DirectorySeparatorChar));

    /// <summary>The skill folder of the agent at global scope.</summary>
    /// <param name="home">The home folder the global scope writes under.</param>
    /// <param name="agent">The agent name: claude, opencode, or agents.</param>
    /// <returns>The absolute skill folder under the home folder.</returns>
    public static string GlobalFolder(string home, string agent) =>
        Path.Combine(home, GlobalPaths[agent].Replace('/', Path.DirectorySeparatorChar));

    /// <summary>
    ///     The home folder the global scope writes under: the test override when it names
    ///     one, else the user's profile folder.
    /// </summary>
    /// <returns>The home folder for global skill copies.</returns>
    public static string HomeFolder() =>
        CliHost.Variable(HomeVariable) is { Length: > 0 } home
            ? home
            : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>Whether the folder holds a skill copy this tool wrote, identified by its frontmatter marker.</summary>
    /// <param name="skillFolder">The candidate skill folder.</param>
    /// <returns>True when the folder's entry file carries the marker in its frontmatter metadata; otherwise false.</returns>
    public static bool CarriesMarker(string skillFolder)
    {
        string text;

        try
        {
            text = File.ReadAllText(Path.Combine(skillFolder, FileName));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }

        if (Frontmatter(text) is not { } front)
        {
            return false;
        }

        JsonNode? node;

        try
        {
            node = RecordText.Parse(front, yaml: true);
        }
        catch (JsonTextException)
        {
            return false;
        }

        return node is JsonObject root
            && root["metadata"] is JsonObject metadata
            && metadata["generated-by"] is JsonValue marker
            && marker.TryGetValue<string>(out var value)
            && value == GeneratedBy;
    }

    /// <summary>Stamps the entry text with the release, or removes the placeholder line for a global copy.</summary>
    /// <param name="text">The template entry text.</param>
    /// <param name="release">The carried release for the project stamp, or null for a global copy.</param>
    /// <returns>The entry text with the placeholder line stamped or removed.</returns>
    /// <exception cref="IOException">The entry text carries no placeholder line.</exception>
    static string Stamp(string text, string? release)
    {
        if (!text.Contains(Placeholder, StringComparison.Ordinal))
        {
            throw new IOException($"The LiteSpec release holds no {Placeholder} line in templates/skill/{SkillName}/{FileName}.");
        }

        // The placeholder stands alone on its line, so the global copy removes the
        // line with its break; the project copy takes the stamp with its indent.
        return release is null
            ? text.Replace(Placeholder + "\r\n", "", StringComparison.Ordinal)
                .Replace(Placeholder + "\n", "", StringComparison.Ordinal)
                .Replace(Placeholder, "", StringComparison.Ordinal)
            : text.Replace(Placeholder, $"  litespec-release: {release}", StringComparison.Ordinal);
    }

    /// <summary>The YAML frontmatter between the leading fences of the entry text, or null when it holds none.</summary>
    /// <param name="text">The entry file text.</param>
    /// <returns>The frontmatter without its fences, or null.</returns>
    static string? Frontmatter(string text)
    {
        var lines = text.Split('\n');

        if (lines.Length < 2 || lines[0].Trim() != "---")
        {
            return null;
        }

        for (var index = 1; index < lines.Length; index++)
        {
            if (lines[index].Trim() == "---")
            {
                return string.Join('\n', lines[1..index]);
            }
        }

        return null;
    }
}
