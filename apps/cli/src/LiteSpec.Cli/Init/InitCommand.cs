using System.Text.Json.Nodes;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Skill;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Init;

/// <summary>
///     The <c>litespec init</c> command: writes a consumer's <c>.litespec/project.yaml</c>
///     from one of the ready-made presets in <c>templates/presets/</c>, copies the
///     starting agent context, glossary, and example cast from <c>templates/consumer/</c>,
///     and inserts the agent-protocol region into an existing <c>AGENTS.md</c> that holds none.
/// </summary>
/// <follows>litespec/core/scope#select-a-size-a-shape-and-stacks</follows>
/// <follows>litespec/core/scope#claim-the-record-layers-the-project-uses</follows>
/// <follows>litespec/core/authoring#record-the-reviewed-litespec-release</follows>
static class InitCommand
{
    /// <summary>The preset names <c>litespec init</c> accepts, in list order.</summary>
    static readonly string[] Presets = ["android-client", "blazor-client", "dotnet-react", "dotnet-svelte", "records"];

    /// <summary>
    ///     The ways a project installs the LiteSpec command line, with the command that runs it:
    ///     a local .NET tool runs as <c>dotnet litespec</c>, and the standalone binary from the
    ///     GitHub release runs as <c>litespec</c> on the PATH.
    /// </summary>
    static readonly Dictionary<string, string> Installs = new(StringComparer.Ordinal)
    {
        ["tool"] = "dotnet litespec",
        ["standalone"] = "litespec",
    };

    /// <summary>The setup templates copied beside the project file: the release file and its consumer path.</summary>
    static readonly (string Template, string Target)[] Scaffold =
    [
        ("project-agents.md", "AGENTS.md"),
        ("glossary.yaml", ".litespec/glossary.yaml"),
        ("example-cast.md", ".litespec/example-cast.md"),
    ];

    /// <summary>The command's usage text.</summary>
    public const string Usage = """
        Usage: litespec init --preset <name> --size <small|large> [--install <tool|standalone>] [--root <path>] [--name <project>] [--skill <agent>[,<agent>]] [--skill-global] [--help]

        Writes .litespec/project.yaml into root from the named preset, stamped with the
        LiteSpec release this tool carries, and copies the starting agent context,
        glossary, and example cast into .litespec/ beside it. A fresh AGENTS.md carries the
        agent-protocol region filled with the release, size, shape, stacks, layers,
        extensions, and selection index of the new project, and names the command that
        runs the CLI. An existing AGENTS.md that holds no region gains the region after
        its first heading, or at the end; text outside the region is never touched, and a
        region that is already there is never rewritten. With --install tool, the
        command also writes .config/dotnet-tools.json pinning the LiteSpec.Cli tool at
        the release, and the agent context runs the CLI as dotnet litespec. With
        --install standalone, the project runs the native binary as litespec, and
        reviewedLiteSpecVersion pins the release. The preset selects the shape, the stacks
        and the frontends; a preset that declares layers writes that claim
        instead of the template claim, and a preset that names no stacks or
        no frontends writes a project with none. --size and --name complete the
        project record. The command never overwrites an existing file: it writes
        each missing file and leaves existing ones alone, except for the region it
        inserts into an existing AGENTS.md. With --skill, the command also installs
        the litespec skill at project scope for the named agents, one generated copy
        per agent from the carried release, stamped with the release. With
        --skill-global, the copies are written under the user's home instead and name
        no release. Without --skill and without --skill-global, the command prints the
        skill add command as its last line instead of installing. When the root holds
        a CLAUDE.md that does not import AGENTS.md, the command prints a line saying so.

          --preset   One of: android-client, blazor-client, dotnet-react, dotnet-svelte, records.
          --size     The project size: small or large.
          --install  How the project installs the CLI: tool, a local .NET tool (the default), or standalone, the native binary.
          --root     The consumer repository to write into. Defaults to the working directory.
          --name     The project name. Defaults to the root folder's name.
          --skill    The agents to install the skill for at project scope: one or more of claude, opencode, agents, comma separated. With --skill-global the copies are written at global scope instead.
          --skill-global  Write the skill copies under the user's home instead of the consumer. Without --skill it installs the default agents.
          --help     Print this text and exit.

        Exit codes: 0 written, 1 the release's template, preset, scaffold, or skill file is unreadable or a skill target holds a folder this tool did not write, 2 the project file exists or an option or agent is invalid.
        """;

    /// <summary>Runs the command and returns its exit code.</summary>
    /// <param name="args">The arguments after the init command name, holding the preset, size, root, project name and skill options.</param>
    /// <returns>The process exit code: 0 when the project is written, 1 when a release file cannot be read or written or a skill target holds a folder this tool did not write, and 2 when the project file exists or an option or agent is invalid.</returns>
    public static int Run(string[] args)
    {
        var parsed = CliArgs.Parse(args, ["--skill-global"], ["--preset", "--size", "--install", "--root", "--name", "--skill"]);

        if (parsed.Help)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is not null)
        {
            return CommandFailure.UsageError(parsed.Error, Usage, json: false);
        }

        var size = parsed.Value("--size");

        if (size is not ("small" or "large"))
        {
            return CommandFailure.UsageError(size is null ? "Missing --size; use small or large." : $"Unknown size '{size}'; use small or large.", Usage, json: false);
        }

        var preset = parsed.Value("--preset");

        if (preset is null || !Presets.Contains(preset))
        {
            return CommandFailure.UsageError(preset is null ? $"Missing --preset. Known presets: {string.Join(", ", Presets)}." : $"Unknown preset '{preset}'. Known presets: {string.Join(", ", Presets)}.", Usage, json: false);
        }

        var install = parsed.Value("--install") ?? "tool";

        if (!Installs.TryGetValue(install, out var command))
        {
            return CommandFailure.UsageError($"Unknown install '{install}'; use tool or standalone.", Usage, json: false);
        }

        var skillGlobal = parsed.Has("--skill-global");
        var skillOption = parsed.Value("--skill");
        IReadOnlyList<string> skillAgents;

        if (skillOption is not null)
        {
            if (string.IsNullOrWhiteSpace(skillOption))
            {
                return CommandFailure.UsageError("Option --skill needs a value.", Usage, json: false);
            }

            var names = skillOption.Split(',').Select(name => name.Trim()).ToList();

            if (names.Any(name => name.Length == 0))
            {
                return CommandFailure.UsageError("Option --skill needs a value.", Usage, json: false);
            }

            foreach (var agent in names)
            {
                if (!SkillFiles.IsAgent(agent))
                {
                    return CommandFailure.UsageError($"Unknown agent '{agent}'; use agents, claude, or opencode.", Usage, json: false);
                }
            }

            skillAgents = names.Distinct(StringComparer.Ordinal).ToList();
        }
        else if (skillGlobal)
        {
            skillAgents = SkillFiles.DefaultAgents;
        }
        else
        {
            skillAgents = [];
        }

        var wantsSkill = skillOption is not null || skillGlobal;

        // init creates the project, so its root is the folder named, never a parent consumer.
        var root = ConsumerRoot.Locate(parsed.Value("--root"), requireProject: false, json: false, out var rootExit);

        if (root is null)
        {
            return rootExit;
        }

        var target = Path.Combine(root, ".litespec", "project.yaml");

        if (File.Exists(target))
        {
            CliHost.Error.WriteLine($"'{target}' already exists; litespec init never overwrites it.");
            return 2;
        }

        string home;

        try
        {
            home = LiteSpecHome.RequireRoot();
        }
        catch (InvalidOperationException exception)
        {
            return CommandFailure.UsageError(exception.Message, Usage, json: false);
        }

        var release = LiteSpecHome.Version;

        if (release is null)
        {
            CliHost.Error.WriteLine("The LiteSpec release holds no readable version.");
            return 1;
        }

        // The project and the tool manifest name the package version, which a release candidate suffixes.
        // Links name the release: a candidate's site is not published, so its links keep the base version.
        var version = ReleaseVersion.PackageOf(release);

        JsonObject project;
        JsonObject overlay;
        Dictionary<string, string> scaffold;

        try
        {
            if (RecordText.Read(Path.Combine(home, "templates", "consumer", "project.yaml")) is not JsonObject template)
            {
                CliHost.Error.WriteLine("The LiteSpec release holds no readable templates/consumer/project.yaml.");
                return 1;
            }

            if (RecordText.Read(Path.Combine(home, "templates", "presets", $"{preset}.yaml")) is not JsonObject presetJson
                || presetJson["shape"] is null)
            {
                CliHost.Error.WriteLine($"The LiteSpec release holds no readable templates/presets/{preset}.yaml.");
                return 1;
            }

            // The scaffold sources are read before anything is written, so a broken
            // release fails before the project file exists without its companions.
            scaffold = [];

            foreach (var (file, _) in Scaffold)
            {
                var source = Path.Combine(home, "templates", "consumer", file);

                if (!File.Exists(source))
                {
                    CliHost.Error.WriteLine($"The LiteSpec release holds no readable templates/consumer/{file}.");
                    return 1;
                }

                scaffold[file] = File.ReadAllText(source);
            }

            project = template;
            overlay = presetJson;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonTextException)
        {
            CliHost.Error.WriteLine($"Failed to read the LiteSpec release: {exception.Message}");
            return 1;
        }

        project["shape"] = overlay["shape"]!.DeepClone();

        // A preset that names no stacks writes a project with none, so the
        // template stacks do not survive into a records-level project.
        if (overlay["stacks"] is not null)
        {
            project["stacks"] = overlay["stacks"]!.DeepClone();
        }
        else
        {
            project.Remove("stacks");
        }

        // A preset that declares its layer claims writes them; every other preset
        // keeps the template claims.
        if (overlay["layers"] is not null)
        {
            project["layers"] = overlay["layers"]!.DeepClone();
        }

        project["size"] = size;

        var paths = project["paths"] as JsonObject;

        if (paths is null)
        {
            paths = [];
            project["paths"] = paths;
        }

        // A preset that names no frontends writes a project with none, so a
        // server project with no frontend omits frontends.
        if (overlay["frontends"] is not null)
        {
            project["frontends"] = overlay["frontends"]!.DeepClone();
        }
        else
        {
            project.Remove("frontends");
        }

        // The shared UI package and its pattern catalog belong to the controlled web
        // frontends; a preset without one would declare a package no frontend uses.
        var controlled = (project["frontends"] as JsonArray)?
            .Any(frontend => (string?)frontend?["platform"] is "react" or "svelte") ?? false;

        if (!controlled)
        {
            project.Remove("ui");
            paths.Remove("uiPatterns");
        }

        var record = project["project"] as JsonObject;

        if (record is null)
        {
            record = [];
            project["project"] = record;
        }

        var name = parsed.Value("--name") ?? new DirectoryInfo(root).Name;
        record["name"] = name;
        project["reviewedLiteSpecVersion"] = version;

        // The template names the project as __PROJECT__ in paths and package names; the
        // name fills those, and every other placeholder is left for the owner to fill.
        // An npm package scope must be lowercase, so a scope takes the name in lowercase
        // kebab case: Team Expenses becomes @team-expenses/ui.
        Fill(project, "@__PROJECT__/", $"@{PackageScope(name)}/");
        Fill(project, "__PROJECT__", name);
        var unfilled = Placeholders(project).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            // The first line names the schema for YAML language servers; the body is the project
            // as YAML, written from the same tree the placeholders were filled in.
            var body = YamlWriter.Write(RecordText.TextOf(project));
            var header = $"# yaml-language-server: $schema=https://litespec.dev/{release}/schemas/litespec-project.schema.yaml\n";
            File.WriteAllText(target, header + (body.EndsWith('\n') ? body : body + "\n"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            CliHost.Error.WriteLine($"Failed to write '{target}': {exception.Message}");
            return 1;
        }

        CliHost.Out.WriteLine($"Wrote {target}.");

        var copied = 0;
        var shapeText = (string?)project["shape"] ?? "server";
        var stackNames = (project["stacks"] as JsonArray)?
            .Select(entry => (string?)entry)
            .OfType<string>()
            .ToList() ?? [];
        var extensionNames = ExtensionNames(project);
        var layerClaims = LayerClaimsOf(project);
        var filledAgents = AgentProtocol.Fill(
            scaffold["project-agents.md"],
            release,
            version,
            size,
            shapeText,
            AgentProtocol.FormatStacks(stackNames),
            AgentProtocol.FormatExtensions(extensionNames),
            AgentProtocol.FormatLayers(layerClaims),
            AgentProtocol.SelectionKey(shapeText, stackNames),
            command);

        foreach (var (file, relative) in Scaffold)
        {
            var destination = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));

            if (file == "project-agents.md" && File.Exists(destination))
            {
                // An existing agent context keeps its own text; init only inserts the region it lacks.
                var insertion = InsertAgentsRegion(destination, filledAgents);

                if (insertion != 0)
                {
                    return insertion;
                }

                continue;
            }

            if (File.Exists(destination))
            {
                CliHost.Out.WriteLine($"'{destination}' already exists; left alone.");
                continue;
            }

            var contents = file == "project-agents.md" ? filledAgents : scaffold[file];

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.WriteAllText(destination, contents);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                CliHost.Error.WriteLine($"Failed to write '{destination}': {exception.Message}");
                return 1;
            }

            CliHost.Out.WriteLine($"Wrote {destination}.");
            copied += 1;
        }

        var gitignore = Path.Combine(root, ".litespec", ".gitignore");

        if (File.Exists(gitignore))
        {
            CliHost.Out.WriteLine($"'{gitignore}' already exists; left alone.");
        }
        else
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(gitignore)!);
                File.WriteAllText(gitignore, "model/\nbook/\n");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                CliHost.Error.WriteLine($"Failed to write '{gitignore}': {exception.Message}");
                return 1;
            }

            CliHost.Out.WriteLine($"Wrote {gitignore}.");
        }

        // A standalone project pins the release in reviewedLiteSpecVersion alone, so only a tool install writes the tool manifest.
        if (install == "tool")
        {
            var toolManifest = Path.Combine(root, ".config", "dotnet-tools.json");

            if (File.Exists(toolManifest))
            {
                CliHost.Out.WriteLine($"'{toolManifest}' already exists; left alone.");
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(toolManifest)!);
                    File.WriteAllText(toolManifest, ToolManifest(version));
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    CliHost.Error.WriteLine($"Failed to write '{toolManifest}': {exception.Message}");
                    return 1;
                }

                CliHost.Out.WriteLine($"Wrote {toolManifest}.");
            }
        }

        if (wantsSkill)
        {
            var skillExit = InstallSkills(root, home, version, skillAgents, skillGlobal);

            if (skillExit != 0)
            {
                return skillExit;
            }
        }

        if (unfilled.Count > 0)
        {
            CliHost.Out.WriteLine($"Fill these placeholders before validating: {string.Join(", ", unfilled)}.");
        }

        if (copied > 0)
        {
            CliHost.Out.WriteLine("Fill every <placeholder> in the copied files before validating.");
        }

        CliHost.Out.WriteLine($"Next step: {command} model check");
        NoticeClaudeImport(root);

        if (!wantsSkill)
        {
            CliHost.Out.WriteLine($"To install the LiteSpec skill, run: {command} skill add");
        }

        return 0;
    }

    /// <summary>Installs the skill copies init requested, one generated copy per agent.</summary>
    /// <param name="root">The consumer root the project scope writes under.</param>
    /// <param name="home">The LiteSpec release root the skill templates are read from.</param>
    /// <param name="version">The carried release the project copies are stamped with.</param>
    /// <param name="agents">The selected agent names.</param>
    /// <param name="global">Whether the copies live under the home folder rather than the consumer.</param>
    /// <returns>The process exit code: 0 when every copy is written, 1 when a target is refused or a file cannot be read or written.</returns>
    static int InstallSkills(string root, string home, string version, IReadOnlyList<string> agents, bool global)
    {
        IReadOnlyList<(string RelativePath, string Text)> files;

        try
        {
            files = SkillFiles.Render(Path.Combine(home, "templates"), global ? null : version);
        }
        catch (IOException exception)
        {
            CliHost.Error.WriteLine(exception.Message);
            return 1;
        }
        catch (UnauthorizedAccessException exception)
        {
            CliHost.Error.WriteLine($"Failed to read the LiteSpec release: {exception.Message}");
            return 1;
        }

        var baseFolder = global ? SkillFiles.HomeFolder() : root;
        var folders = agents.Select(agent => global ? SkillFiles.GlobalFolder(baseFolder, agent) : SkillFiles.ProjectFolder(baseFolder, agent)).ToList();

        // Every target is checked before any is written, so a refused target leaves the rest untouched.
        var refused = folders.Where(folder => Directory.Exists(folder) && !SkillFiles.CarriesMarker(folder)).ToList();

        if (refused.Count > 0)
        {
            foreach (var folder in refused)
            {
                CliHost.Error.WriteLine($"'{folder}' holds a skill this tool did not write; left alone.");
            }

            return 1;
        }

        foreach (var folder in folders)
        {
            var update = Directory.Exists(folder);

            try
            {
                SkillFiles.WriteCopy(folder, files);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                CliHost.Error.WriteLine($"Failed to write '{folder}': {exception.Message}");
                return 1;
            }

            CliHost.Out.WriteLine(update ? $"Updated {folder}." : $"Wrote {folder}.");
        }

        return 0;
    }

    /// <summary>Prints a line when the root holds a CLAUDE.md that does not import AGENTS.md.</summary>
    /// <param name="root">The consumer root.</param>
    static void NoticeClaudeImport(string root)
    {
        var file = Path.Combine(root, AgentProtocol.ClaudeFileName);

        if (!File.Exists(file))
        {
            return;
        }

        string text;

        try
        {
            text = File.ReadAllText(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        if (AgentProtocol.ImportsAgents(text))
        {
            return;
        }

        CliHost.Out.WriteLine($"'{file}' does not import AGENTS.md; add '@AGENTS.md' so Claude Code reads the agent protocol.");
    }

    /// <summary>Inserts the agent-protocol region into an existing agent context that holds none.</summary>
    /// <param name="destination">The existing AGENTS.md path.</param>
    /// <param name="filledTemplate">The filled agent context template the region is read from.</param>
    /// <returns>0 when the region was inserted or the file already holds one, 1 when the file cannot be read or written.</returns>
    static int InsertAgentsRegion(string destination, string filledTemplate)
    {
        string existing;

        try
        {
            existing = File.ReadAllText(destination);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            CliHost.Error.WriteLine($"Failed to read '{destination}': {exception.Message}");
            return 1;
        }

        if (AgentProtocol.ContainsRegion(existing) || AgentProtocol.TemplateBody(filledTemplate) is not { } body)
        {
            CliHost.Out.WriteLine($"'{destination}' already exists; left alone.");
            return 0;
        }

        var newline = existing.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var rewritten = AgentProtocol.Insert(existing, AgentProtocol.BuildRegion(body, newline), newline);

        try
        {
            File.WriteAllText(destination, rewritten);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            CliHost.Error.WriteLine($"Failed to write '{destination}': {exception.Message}");
            return 1;
        }

        CliHost.Out.WriteLine($"Updated {destination}.");
        return 0;
    }

    /// <summary>The extension ids the project selects in selectedExtensions, in declaration order.</summary>
    /// <param name="project">The project document the preset overlay produced.</param>
    /// <returns>The selected extension ids.</returns>
    static List<string> ExtensionNames(JsonObject project)
    {
        var names = new List<string>();

        if (project["selectedExtensions"] is not JsonArray selected)
        {
            return names;
        }

        foreach (var entry in selected)
        {
            if (entry is JsonValue value && value.TryGetValue<string>(out var id) && id.Length > 0)
            {
                names.Add(id);
            }
            else if (entry is JsonObject container
                && container["id"] is JsonValue contained
                && contained.TryGetValue<string>(out var objectId)
                && objectId.Length > 0)
            {
                names.Add(objectId);
            }
        }

        return names;
    }

    /// <summary>The layer claims the project declares in layers.</summary>
    /// <param name="project">The project document the preset overlay produced.</param>
    /// <returns>The claimed layers with their practices.</returns>
    static LayerClaims LayerClaimsOf(JsonObject project)
    {
        var layers = project["layers"] as JsonObject;

        return new LayerClaims(
            layers?.ContainsKey("intent") == true,
            layers?.ContainsKey("behavior") == true,
            PracticeOf(layers, "implementation"),
            PracticeOf(layers, "proof"));
    }

    /// <summary>The practice a layer claims, or null when the layer is not claimed.</summary>
    /// <param name="layers">The layers mapping, or null when the project names none.</param>
    /// <param name="name">The layer name.</param>
    /// <returns>The practice, or null.</returns>
    static LayerPractice? PracticeOf(JsonObject? layers, string name)
    {
        if (layers is null || !layers.ContainsKey(name))
        {
            return null;
        }

        return layers[name] is JsonValue value && value.TryGetValue<string>(out var text) && text == "own"
            ? LayerPractice.Own
            : LayerPractice.LiteSpec;
    }

    /// <summary>The local tool manifest pinning the LiteSpec.Cli tool at the release.</summary>
    /// <param name="version">The LiteSpec package version to pin in the tool manifest, with a release candidate's suffix.</param>
    /// <returns>The dotnet-tools manifest JSON text pinning the LiteSpec.Cli tool at that version.</returns>
    internal static string ToolManifest(string version) => $$"""
        {
          "version": 1,
          "isRoot": true,
          "tools": {
            "LiteSpec.Cli": {
              "version": "{{version}}",
              "commands": ["litespec"]
            }
          }
        }
        """ + "\n";

    /// <summary>The project name as an npm package scope: lowercase, with runs of other characters as one hyphen.</summary>
    /// <param name="name">The project name to convert into a package scope.</param>
    /// <returns>The lowercase kebab-case scope form of the name.</returns>
    static string PackageScope(string name) =>
        System.Text.RegularExpressions.Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9._-]+", "-").Trim('-');

    /// <summary>Replaces a placeholder inside every string value of the document.</summary>
    /// <param name="node">The JSON node whose string values are rewritten in place, or null when the subtree holds no value.</param>
    /// <param name="placeholder">The placeholder text to replace in each string value.</param>
    /// <param name="value">The replacement text for the placeholder.</param>
    static void Fill(JsonNode? node, string placeholder, string value)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var key in jsonObject.Select(entry => entry.Key).ToList())
                {
                    if (jsonObject[key] is JsonValue text && text.TryGetValue<string>(out var current))
                    {
                        jsonObject[key] = current.Replace(placeholder, value, StringComparison.Ordinal);
                    }
                    else
                    {
                        Fill(jsonObject[key], placeholder, value);
                    }
                }

                break;
            case JsonArray array:
                for (var index = 0; index < array.Count; index++)
                {
                    if (array[index] is JsonValue text && text.TryGetValue<string>(out var current))
                    {
                        array[index] = current.Replace(placeholder, value, StringComparison.Ordinal);
                    }
                    else
                    {
                        Fill(array[index], placeholder, value);
                    }
                }

                break;
        }
    }

    /// <summary>Every template placeholder, such as <c>__EXAMPLE__</c>, still in the document.</summary>
    /// <param name="node">The document node to scan, or null when the document holds no value.</param>
    /// <returns>Every placeholder token still present in the document string values.</returns>
    public static IEnumerable<string> Placeholders(JsonNode? node) => node switch
    {
        JsonObject jsonObject => jsonObject.SelectMany(entry => Placeholders(entry.Value)),
        JsonArray array => array.SelectMany(Placeholders),
        JsonValue value when value.TryGetValue<string>(out var text) => PlaceholderPattern.Matches(text).Select(match => match.Value),
        _ => [],
    };

    /// <summary>The pattern matching a template placeholder token such as <c>__EXAMPLE__</c>.</summary>
    static readonly System.Text.RegularExpressions.Regex PlaceholderPattern = new(@"__[A-Z][A-Z0-9_]*?__");
}
