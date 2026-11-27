namespace LiteSpec.Cli.Review;

/// <summary>
///     The <c>litespec docs</c> command: reviews the changed prose or re-renders
///     the changed records. The subcommands read and write files only, so they
///     call no model and no network.
/// </summary>
/// <follows>litespec/core/writing#write-human-docs-for-the-cold-reader</follows>
static class DocsCommand
{
    /// <summary>The usage text for the docs command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage: litespec docs <command> [options]

        Reviews the changed documentation or re-renders the changed records.
        Every subcommand reads and writes files only.

          review        Run the prose lints on the files changed since a revision and write a review packet.
          render        Re-render the book pages of only the records whose content hash changed.

        Run 'litespec docs <command> --help' for a command's options.
        """;

    /// <summary>Runs the docs command's subcommand.</summary>
    /// <param name="args">The command arguments, the subcommand first.</param>
    /// <returns>The subcommand's exit code, or 2 for a usage error.</returns>
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

        return args[0] switch
        {
            "review" => DocsReviewCommand.Run(args[1..]),
            "render" => DocsRenderCommand.Run(args[1..]),
            _ => CommandFailure.UsageError($"Unknown command '{args[0]}'.", Usage, json: false),
        };
    }
}
