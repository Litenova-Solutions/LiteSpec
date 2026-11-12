using LiteSpec.Cli.Model.Configuration;
using LiteSpec.CommandLine;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Skill;

/// <summary>
///     The <c>litespec skill</c> command: installs or removes the litespec skill for the
///     project's coding agents, one generated copy per selected agent.
/// </summary>
/// <follows>litespec/core/agent#keep-the-agent-protocol-current</follows>
static class SkillCommand
{
    /// <summary>The command's usage text.</summary>
    public const string Usage = """
        Usage: litespec skill add [--agent <name>]... [--global] [--root <path>] [--help]
               litespec skill remove [--agent <name>]... [--global] [--root <path>] [--help]
               litespec skill --help

        Installs or removes the litespec skill, the folder of guidance a coding
        agent loads when its description matches the task. Each selected agent
        gets its own copy from the carried release: claude writes
        .claude/skills/litespec/, opencode writes .opencode/skills/litespec/,
        and agents writes .agents/skills/litespec/. With no --agent, the command
        selects agents and claude, which together reach Claude Code and OpenCode.
        With --global, the command writes under the user's home instead of the
        consumer and needs no .litespec/project.yaml; a project copy is stamped
        with the carried release, and a global copy names no release. Add writes
        every selected copy and replaces a copy this tool wrote; a folder without
        its marker is left alone. Remove deletes only a folder carrying the marker.

          --agent   One of claude, opencode, or agents. Repeat the option for more than one.
          --global  Write under the user's home instead of the consumer. Cannot be combined with --root.
          --root    The consumer repository to change. Defaults to the working directory.
          --help    Print this text and exit.

        Exit codes: 0 installed or removed, 1 a target holds a folder this tool did not write or a file cannot be read or written, 2 the subcommand or an agent is unknown, the project file is missing, or an option is invalid.
        """;

    /// <summary>Runs the command and returns its exit code.</summary>
    /// <param name="args">The command arguments after the command name, holding the subcommand and the options.</param>
    /// <returns>The process exit code: 0 when the skill is installed or removed, 1 when a target holds a folder this tool did not write or a file cannot be read or written, and 2 on an unknown subcommand or agent, a missing project file, or an invalid option.</returns>
    public static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            CliHost.Out.WriteLine(Usage);
            return 2;
        }

        if (args is ["--help"] or ["-h"] or ["help"])
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        var (agents, rest, splitError) = SplitAgents(args);

        if (splitError is not null)
        {
            return CommandFailure.UsageError(splitError, Usage, json: false);
        }

        var parsed = CliArgs.Parse(rest.ToArray(), ["--global"], ["--root"], maxPositionals: 1);

        if (parsed.Help)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is not null)
        {
            return CommandFailure.UsageError(parsed.Error, Usage, json: false);
        }

        if (parsed.Positionals is not [var subcommand])
        {
            return CommandFailure.UsageError("Skill needs a subcommand: add or remove.", Usage, json: false);
        }

        if (subcommand is not ("add" or "remove"))
        {
            return CommandFailure.UsageError($"Unknown command '{subcommand}'.", Usage, json: false);
        }

        IReadOnlyList<string> selected = agents.Count == 0
            ? SkillFiles.DefaultAgents
            : agents.Distinct(StringComparer.Ordinal).ToList();

        foreach (var agent in selected)
        {
            if (agent.Length == 0)
            {
                return CommandFailure.UsageError("Option --agent needs a value.", Usage, json: false);
            }

            if (!SkillFiles.IsAgent(agent))
            {
                return CommandFailure.UsageError($"Unknown agent '{agent}'; use agents, claude, or opencode.", Usage, json: false);
            }
        }

        var global = parsed.Has("--global");
        var rootOption = parsed.Value("--root");

        if (global && rootOption is not null)
        {
            return CommandFailure.UsageError("--global and --root cannot be combined.", Usage, json: false);
        }

        string baseFolder;

        if (global)
        {
            baseFolder = SkillFiles.HomeFolder();
        }
        else
        {
            var config = ConsumerRoot.Open(rootOption, json: false, out var openExit);

            if (config is null)
            {
                return openExit;
            }

            baseFolder = config.Root;
        }

        string releaseRoot;

        try
        {
            releaseRoot = LiteSpecHome.RequireRoot();
        }
        catch (InvalidOperationException exception)
        {
            return CommandFailure.UsageError(exception.Message, Usage, json: false);
        }

        string? release = null;

        if (!global)
        {
            release = LiteSpecHome.Version;

            if (release is null)
            {
                CliHost.Error.WriteLine("The LiteSpec release holds no readable version.");
                return 1;
            }
        }

        IReadOnlyList<(string RelativePath, string Text)> files;

        try
        {
            files = SkillFiles.Render(Path.Combine(releaseRoot, "templates"), release);
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

        return subcommand == "add"
            ? Add(selected, global, baseFolder, files)
            : Remove(selected, global, baseFolder);
    }

    /// <summary>Installs the skill at every selected target, replacing a copy this tool wrote.</summary>
    /// <param name="agents">The selected agent names.</param>
    /// <param name="global">Whether the targets live under the home folder rather than the consumer.</param>
    /// <param name="baseFolder">The consumer root or the home folder the targets live under.</param>
    /// <param name="files">Every file of the copy: its forward-slash path under the skill folder and its text.</param>
    /// <returns>The process exit code: 0 when every copy is written, 1 when a target is refused or a file cannot be written.</returns>
    static int Add(IReadOnlyList<string> agents, bool global, string baseFolder, IReadOnlyList<(string RelativePath, string Text)> files)
    {
        var folders = agents.Select(agent => FolderFor(global, baseFolder, agent)).ToList();

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
                // A marked copy is removed first, so a re-add holds exactly the carried files.
                if (update)
                {
                    Directory.Delete(folder, recursive: true);
                }

                foreach (var (relative, text) in files)
                {
                    var target = Path.Combine(folder, relative.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.WriteAllText(target, text);
                }
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

    /// <summary>Removes the skill at every selected target written by this tool.</summary>
    /// <param name="agents">The selected agent names.</param>
    /// <param name="global">Whether the targets live under the home folder rather than the consumer.</param>
    /// <param name="baseFolder">The consumer root or the home folder the targets live under.</param>
    /// <returns>The process exit code: 0 when every target is removed or already absent, 1 when a target holds a folder this tool did not write or a folder cannot be removed.</returns>
    static int Remove(IReadOnlyList<string> agents, bool global, string baseFolder)
    {
        var refused = false;

        foreach (var agent in agents)
        {
            var folder = FolderFor(global, baseFolder, agent);

            if (!Directory.Exists(folder))
            {
                CliHost.Out.WriteLine($"No litespec skill at '{folder}'; nothing to remove.");
                continue;
            }

            if (!SkillFiles.CarriesMarker(folder))
            {
                CliHost.Error.WriteLine($"'{folder}' holds a skill this tool did not write; left alone.");
                refused = true;
                continue;
            }

            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                CliHost.Error.WriteLine($"Failed to remove '{folder}': {exception.Message}");
                return 1;
            }

            CliHost.Out.WriteLine($"Removed {folder}.");
        }

        return refused ? 1 : 0;
    }

    /// <summary>The skill folder of the agent under the consumer root or the home folder.</summary>
    /// <param name="global">Whether the folder lives under the home folder rather than the consumer.</param>
    /// <param name="baseFolder">The consumer root or the home folder.</param>
    /// <param name="agent">The agent name: claude, opencode, or agents.</param>
    /// <returns>The absolute skill folder.</returns>
    static string FolderFor(bool global, string baseFolder, string agent) =>
        global ? SkillFiles.GlobalFolder(baseFolder, agent) : SkillFiles.ProjectFolder(baseFolder, agent);

    /// <summary>Splits the repeatable --agent options from the arguments, so the remaining options parse once.</summary>
    /// <param name="args">The raw arguments after the command name.</param>
    /// <returns>The agent values in order, the remaining arguments, and the usage error when --agent names no value.</returns>
    static (List<string> Agents, List<string> Others, string? Error) SplitAgents(string[] args)
    {
        var agents = new List<string>();
        var rest = new List<string>(args.Length);

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];

            if (arg == "--agent")
            {
                // Like every other value option, --agent takes the next argument unless it opens another option.
                if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    return (agents, rest, "Option --agent needs a value.");
                }

                agents.Add(args[++index]);
            }
            else if (arg.StartsWith("--agent=", StringComparison.Ordinal))
            {
                var value = arg["--agent=".Length..];

                if (value.Length == 0)
                {
                    return (agents, rest, "Option --agent needs a value.");
                }

                agents.Add(value);
            }
            else
            {
                rest.Add(arg);
            }
        }

        return (agents, rest, null);
    }
}
