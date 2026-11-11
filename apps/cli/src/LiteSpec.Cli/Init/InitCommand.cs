using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Init;

/// <summary>
///     The <c>litespec init</c> command: writes a consumer's <c>.litespec/project.yaml</c>
///     from one of the ready-made presets in <c>templates/presets/</c>, and copies the
///     starting agent context, glossary, and example cast from <c>templates/consumer/</c>.
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
        Usage: litespec init --preset <name> --size <small|large> [--install <tool|standalone>] [--root <path>] [--name <project>] [--help]

        Writes .litespec/project.yaml into root from the named preset, stamped with the
        LiteSpec release this tool carries, and copies the starting agent context,
        glossary, and example cast into .litespec/ beside it. A fresh AGENTS.md carries the LiteSpec
        block filled with the release, size, shape, stacks, and selection index of the
        new project, and names the command that runs the CLI. With --install tool, the
        command also writes .config/dotnet-tools.json pinning the LiteSpec.Cli tool at
        the release, and the agent context runs the CLI as dotnet litespec. With
        --install standalone, the project runs the native binary as litespec, and
        reviewedLiteSpecVersion pins the release. The preset selects the shape, the stacks
        and the frontends; a preset that declares layers writes that claim
        instead of the template claim, and a preset that names no stacks or
        no frontends writes a project with none. --size and --name complete the
        project record. The command never overwrites an existing file: it writes
        each missing file and leaves existing ones alone.

          --preset   One of: android-client, blazor-client, dotnet-react, dotnet-svelte, records.
          --size     The project size: small or large.
          --install  How the project installs the CLI: tool, a local .NET tool (the default), or standalone, the native binary.
          --root     The consumer repository to write into. Defaults to the working directory.
          --name     The project name. Defaults to the root folder's name.
          --help     Print this text and exit.

        Exit codes: 0 written, 1 the release's template, preset, or scaffold file is unreadable, 2 the project file exists or an option is invalid.
        """;

    /// <summary>Runs the command and returns its exit code.</summary>
    /// <param name="args">The arguments after the init command name, holding the preset, size, root and project name options.</param>
    /// <returns>The process exit code: 0 when the project is written, 1 when a release file cannot be read or written, and 2 when the project file exists or an option is invalid.</returns>
    public static int Run(string[] args)
    {
        var parsed = CliArgs.Parse(args, [], ["--preset", "--size", "--install", "--root", "--name"]);

        if (parsed.Help)
        {
            Console.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is not null)
        {
            return LastResort.UsageError(parsed.Error, Usage, json: false);
        }

        var size = parsed.Value("--size");

        if (size is not ("small" or "large"))
        {
            return LastResort.UsageError(size is null ? "Missing --size; use small or large." : $"Unknown size '{size}'; use small or large.", Usage, json: false);
        }

        var preset = parsed.Value("--preset");

        if (preset is null || !Presets.Contains(preset))
        {
            return LastResort.UsageError(preset is null ? $"Missing --preset. Known presets: {string.Join(", ", Presets)}." : $"Unknown preset '{preset}'. Known presets: {string.Join(", ", Presets)}.", Usage, json: false);
        }

        var install = parsed.Value("--install") ?? "tool";

        if (!Installs.TryGetValue(install, out var command))
        {
            return LastResort.UsageError($"Unknown install '{install}'; use tool or standalone.", Usage, json: false);
        }

        // init creates the project, so its root is the folder named, never a parent consumer.
        var root = ConsumerRoot.Locate(parsed.Value("--root"), requireProject: false, json: false, out var rootExit);

        if (root is null)
        {
            return rootExit;
        }

        var target = Path.Combine(root, ".litespec", "project.yaml");

        if (File.Exists(target))
        {
            Console.Error.WriteLine($"'{target}' already exists; litespec init never overwrites it.");
            return 2;
        }

        string home;

        try
        {
            home = LiteSpecHome.RequireRoot();
        }
        catch (InvalidOperationException exception)
        {
            return LastResort.UsageError(exception.Message, Usage, json: false);
        }

        var version = LiteSpecHome.Version;

        if (version is null)
        {
            Console.Error.WriteLine("The LiteSpec release holds no readable version.");
            return 1;
        }

        JsonObject project;
        JsonObject overlay;
        Dictionary<string, string> scaffold;

        try
        {
            if (RecordText.Read(Path.Combine(home, "templates", "consumer", "project.yaml")) is not JsonObject template)
            {
                Console.Error.WriteLine("The LiteSpec release holds no readable templates/consumer/project.yaml.");
                return 1;
            }

            if (RecordText.Read(Path.Combine(home, "templates", "presets", $"{preset}.yaml")) is not JsonObject presetJson
                || presetJson["shape"] is null)
            {
                Console.Error.WriteLine($"The LiteSpec release holds no readable templates/presets/{preset}.yaml.");
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
                    Console.Error.WriteLine($"The LiteSpec release holds no readable templates/consumer/{file}.");
                    return 1;
                }

                scaffold[file] = File.ReadAllText(source);
            }

            project = template;
            overlay = presetJson;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonTextException)
        {
            Console.Error.WriteLine($"Failed to read the LiteSpec release: {exception.Message}");
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
            var header = $"# yaml-language-server: $schema=https://litespec.dev/{version}/schemas/litespec-project.schema.yaml\n";
            File.WriteAllText(target, header + (body.EndsWith('\n') ? body : body + "\n"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Failed to write '{target}': {exception.Message}");
            return 1;
        }

        Console.WriteLine($"Wrote {target}.");

        var copied = 0;
        var shapeText = (string?)project["shape"] ?? "server";
        var stackNames = (project["stacks"] as JsonArray)?
            .Select(entry => (string?)entry)
            .OfType<string>()
            .ToList() ?? [];

        foreach (var (file, relative) in Scaffold)
        {
            var destination = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));

            if (File.Exists(destination))
            {
                Console.WriteLine($"'{destination}' already exists; left alone.");
                continue;
            }

            var contents = file == "project-agents.md"
                ? FillAgents(scaffold[file], version, size, shapeText, stackNames, command)
                : scaffold[file];

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.WriteAllText(destination, contents);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Failed to write '{destination}': {exception.Message}");
                return 1;
            }

            Console.WriteLine($"Wrote {destination}.");
            copied += 1;
        }

        var gitignore = Path.Combine(root, ".litespec", ".gitignore");

        if (File.Exists(gitignore))
        {
            Console.WriteLine($"'{gitignore}' already exists; left alone.");
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
                Console.Error.WriteLine($"Failed to write '{gitignore}': {exception.Message}");
                return 1;
            }

            Console.WriteLine($"Wrote {gitignore}.");
        }

        // A standalone project pins the release in reviewedLiteSpecVersion alone, so only a tool install writes the tool manifest.
        if (install == "tool")
        {
            var toolManifest = Path.Combine(root, ".config", "dotnet-tools.json");

            if (File.Exists(toolManifest))
            {
                Console.WriteLine($"'{toolManifest}' already exists; left alone.");
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
                    Console.Error.WriteLine($"Failed to write '{toolManifest}': {exception.Message}");
                    return 1;
                }

                Console.WriteLine($"Wrote {toolManifest}.");
            }
        }

        if (unfilled.Count > 0)
        {
            Console.WriteLine($"Fill these placeholders before validating: {string.Join(", ", unfilled)}.");
        }

        if (copied > 0)
        {
            Console.WriteLine("Fill every <placeholder> in the copied files before validating.");
        }

        Console.WriteLine($"Next step: {command} model check");
        return 0;
    }

    /// <summary>Fills the LiteSpec block of a fresh agent context with the new project's selection.</summary>
    /// <param name="text">The agent context template text with placeholders for the project selection.</param>
    /// <param name="version">The LiteSpec release version the new project records.</param>
    /// <param name="size">The project size, small or large.</param>
    /// <param name="shape">The project shape, server or client-only.</param>
    /// <param name="stacks">The stack identifiers the preset selects, in list order.</param>
    /// <param name="command">The command that runs the CLI in the project: <c>dotnet litespec</c> or <c>litespec</c>.</param>
    /// <returns>The template text with each selection placeholder replaced by its value.</returns>
    static string FillAgents(string text, string version, string size, string shape, List<string> stacks, string command) =>
        text.Replace("__LITESPEC_COMMAND__", command, StringComparison.Ordinal)
            .Replace("__LITESPEC_VERSION__", version, StringComparison.Ordinal)
            .Replace("__SIZE__", size, StringComparison.Ordinal)
            .Replace("__SHAPE__", shape, StringComparison.Ordinal)
            .Replace("__STACKS__", JoinList(stacks), StringComparison.Ordinal)
            .Replace("__SELECTION_KEY__", stacks.Count == 0 ? shape : $"{shape}.{string.Join(".", stacks)}", StringComparison.Ordinal);

    /// <summary>The stacks as prose, the way the selection page joins them: none, one, or joined with commas and and.</summary>
    /// <param name="items">The stack names to join as prose.</param>
    /// <returns>The prose form of the list: none for an empty list, the single name alone, two names joined with and, or a comma list ending with and.</returns>
    static string JoinList(List<string> items) => items.Count switch
    {
        0 => "none",
        1 => items[0],
        2 => $"{items[0]} and {items[1]}",
        _ => $"{string.Join(", ", items.Take(items.Count - 1))} and {items[^1]}",
    };

    /// <summary>The local tool manifest pinning the LiteSpec.Cli tool at the release.</summary>
    /// <param name="version">The LiteSpec release version to pin in the tool manifest.</param>
    /// <returns>The dotnet-tools manifest JSON text pinning the LiteSpec.Cli tool at that version.</returns>
    static string ToolManifest(string version) => $$"""
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
