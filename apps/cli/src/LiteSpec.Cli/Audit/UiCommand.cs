using LiteSpec.Cli.Review;

namespace LiteSpec.Cli.Audit;

/// <summary>The <c>litespec ui</c> command: the rendered-page audit and the review packet of changed routes.</summary>
/// <follows>litespec/core/release#report-verification-exactly</follows>
static class UiCommand
{
    /// <summary>The usage text for the ui command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage: litespec ui audit --base-url <url> [--root <folder>] [--routes <file>] [--widths <list>] [--schemes <list>] [--storage-state <file>] [--out <dir>] [--fail-on blocking|none] [--primary-selector <css>] [--browser <path>] [--help]
               litespec ui review --since <base> [--root <folder>]
               litespec ui --help

        Audits the rendered pages in a browser: visits each route at each
        width in each scheme, runs one audit script after load, and writes a
        screenshot per page with a JSON report. Without --routes, the routes
        come from the screen records, skipping routes with parameters.

          --root              The consumer repository to read. Defaults to the working directory.
          --base-url          The running application to visit, such as http://localhost:3000.
          --routes            A YAML or JSON file listing the routes: entries with a path and an optional name.
          --widths            Comma list of viewport widths in pixels. Defaults to 390,1440,2560.
          --schemes           Comma list of light and dark. Defaults to light,dark.
          --storage-state     A Playwright storage state file for signed-in pages.
          --out               The folder receiving the screenshots and audit.json. Defaults to ui-audit in the working directory.
          --fail-on           One of blocking or none. Defaults to blocking: exit 1 on any finding.
          --primary-selector  The CSS selector marking the primary action. Defaults to [data-variant=primary].
          --browser           The browser to audit with. Defaults to the installed Chrome, Edge, or Chromium, in that order.
          --help              Print this text and exit.

        Exit codes: 0 no finding, or --fail-on none, 1 a finding with --fail-on blocking, no browser, or an unreadable page, 2 the options are invalid or the project file is missing.
        """;

    /// <summary>Runs the ui command's subcommand.</summary>
    /// <param name="args">The command arguments, the subcommand first.</param>
    /// <returns>The exit code: 0 for a clean audit, 1 for findings or a failed run, 2 for a usage error.</returns>
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

        if (args[0] == "review")
        {
            return UiReviewCommand.Run(args[1..]);
        }

        if (args[0] != "audit")
        {
            return CommandFailure.UsageError($"Unknown command '{args[0]}'.", Usage, json: false);
        }

        return AuditCommand.Run(args[1..]);
    }
}
