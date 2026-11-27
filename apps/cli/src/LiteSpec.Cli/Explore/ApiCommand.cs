namespace LiteSpec.Cli.Explore;

/// <summary>The <c>litespec api</c> command: the running-API walk.</summary>
/// <follows>litespec/core/release#report-verification-exactly</follows>
static class ApiCommand
{
    /// <summary>The usage text for the api command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage: litespec api explore --base-url <url> [--openapi <path-or-url>] [--steps <n>] [--seed <n>] [--header "Name: value"]... [--cookie-file <file>] [--deny <method> <path-pattern>]... [--out <dir>] [--allow-delete] [--allow-remote] [--help]
               litespec api --help

        Walks a running API from its OpenAPI document: makes valid-looking
        calls from the listed operations, reuses ids from earlier responses,
        and reports every 5xx, every 4xx outside problem details, and every
        timeout, with a Markdown and a JSON report.

          --base-url          The running API to walk, such as http://localhost:5000.
          --openapi           The OpenAPI document: a file, an absolute URL, or a path under the base URL. Defaults to /openapi/v1.json.
          --steps             How many calls the walk makes. Defaults to 200.
          --seed              The seed that replays the same calls. Defaults to 1.
          --header            A request header as Name: value. Repeat for each header.
          --cookie-file       A file with one name=value cookie per line, sent as the Cookie header.
          --deny              A method and a path pattern the walk never calls, such as --deny POST sign-out. Repeat for each entry.
          --out               The folder receiving explore.md and explore.json. Defaults to .litespec/explore in the working directory.
          --allow-delete      Walk DELETE operations and paths holding delete.
          --allow-remote      Walk a base URL that is not loopback.
          --help              Print this text and exit.

        Exit codes: 0 no 5xx was found, 1 a 5xx was found or the walk could not run, 2 the options are invalid or a named file is missing.
        """;

    /// <summary>Runs the api command's subcommand.</summary>
    /// <param name="args">The command arguments, the subcommand first.</param>
    /// <returns>The exit code: 0 for a clean walk, 1 for a 5xx or a failed run, 2 for a usage error.</returns>
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

        if (args[0] != "explore")
        {
            return CommandFailure.UsageError($"Unknown command '{args[0]}'.", Usage, json: false);
        }

        return ExploreCommand.Run(args[1..]);
    }
}
