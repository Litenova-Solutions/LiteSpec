using LiteSpec.Cli.Book;
using LiteSpec.Cli.Init;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.RecordEditing;
using LiteSpec.Cli.Rules;
using LiteSpec.Cli.Skill;
using LiteSpec.Standards;

namespace LiteSpec.Cli;

/// <summary>The command dispatch: one command line in, one exit code out, for the executable and for in-process runs.</summary>
/// <follows>litespec/core/release#report-verification-exactly</follows>
static class CliEntry
{
    /// <summary>The usage text the CLI prints for help and for a missing command.</summary>
    const string Usage = """
        Usage: litespec <command> [options]

        Commands in a consumer repository:
          init          Write .litespec/project.yaml from a preset.
          new           Scaffold a record page and reserve its identifier.
          rename        Rename a record identifier and rewrite every link.
          model         Check, format, render or query the project model.
          book          Generate the project book from the records.
          rules         List the provisions that govern a path or record.
          skill         Install or remove the litespec skill.

        Run 'litespec <command> --help' for a command's options.
        'litespec --version' prints the LiteSpec release this tool ships in.
        """;

    /// <summary>Runs the command the first argument names.</summary>
    /// <param name="args">The command line.</param>
    /// <returns>The command's exit code.</returns>
    public static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            CliHost.Error.WriteLine(Usage);
            return 2;
        }

        var rest = args[1..];

        return CommandFailure.Run(
            () => args[0] switch
            {
                "--help" or "-h" or "help" => Help(),
                "--version" => Version(),
                "init" => InitCommand.Run(rest),
                "new" => NewCommand.Run(rest),
                "rename" => RenameCommand.Run(rest),
                "model" => ModelCommand.Run(rest),
                "book" => BookCommand.Run(rest),
                "rules" => RulesCommand.Run(rest),
                "skill" => SkillCommand.Run(rest),
                _ => Unknown(args[0]),
            },
            args);
    }

    /// <summary>Prints the usage text.</summary>
    /// <returns>Exit code 0.</returns>
    static int Help()
    {
        CliHost.Out.WriteLine(Usage);
        return 0;
    }

    /// <summary>Prints the LiteSpec package version this tool ships as, with a release candidate's suffix.</summary>
    /// <returns>Exit code 0.</returns>
    static int Version()
    {
        CliHost.Out.WriteLine(LiteSpecHome.Version is { } release ? ReleaseVersion.PackageOf(release) : "unknown");
        return 0;
    }

    /// <summary>Reports a command the CLI does not have.</summary>
    /// <param name="command">The unknown command name.</param>
    /// <returns>The usage error exit code.</returns>
    static int Unknown(string command) =>
        CommandFailure.UsageError($"Unknown command '{command}'.", Usage, json: false);
}
