namespace LiteSpec.Ui;

/// <summary>
///     The controlled web UI validator: the <c>litespec ui</c> command and the
///     in-process <see cref="Validate"/> the consumer validator calls.
/// </summary>
/// <follows>litespec/frontend/ui#select-one-visual-authority</follows>
/// <follows>litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package</follows>
/// <follows>litespec/frontend/web/ui#track-source-changes</follows>
static class UiValidator
{
    /// <summary>The <c>litespec ui</c> usage text.</summary>
    public const string Usage = """
        Usage: litespec ui [--root <path>] [--format json] [--help]

        Validates the controlled web UI contract for the consumer at root,
        which defaults to the current directory.

        Checks the shared package configuration, source lock, design contract,
        token stylesheet, shared stylesheets, class strings, application imports,
        the dependency boundary, the pattern catalog, and the route suite
        coverage. Nothing else: no environment, naming, secret, storage, or HTML
        sink scan.

          --root <path>   The consumer root. Defaults to the current directory.
          --format json   Write one JSON object on stdout instead of human-readable lines.
          --help          Print this text and exit.

        Exit codes: 0 no problem, 1 at least one problem, 2 usage error.
        """;

    /// <summary>Runs the <c>litespec ui</c> command; returns the exit code.</summary>
    /// <param name="args">The command arguments after <c>ui</c>.</param>
    /// <returns>The exit code: 0 no problem, 1 at least one problem, 2 usage error.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Run is the ui command handler: every failure of the validation run goes through the last-resort reporter as an error and an exit code instead of crashing.")]
    public static int Run(string[] args)
    {
        var parsed = CliArgs.Parse(args, Array.Empty<string>(), ["--root", "--format"]);
        if (parsed.Help)
        {
            Console.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is not null)
        {
            Console.Error.WriteLine(parsed.Error);
            Console.Error.WriteLine(Usage);
            return 2;
        }

        if (parsed.FormatError is not null)
        {
            Console.Error.WriteLine(parsed.FormatError);
            Console.Error.WriteLine(Usage);
            return 2;
        }

        string root;
        try
        {
            root = parsed.Root();
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Console.Error.WriteLine($"Invalid root '{parsed.Value("--root")}': {exception.Message}");
            return 2;
        }

        if (LiteSpecHome.Root is null)
        {
            Console.Error.WriteLine($"No LiteSpec release was found above '{AppContext.BaseDirectory}'. Set LITESPEC_HOME to the folder that holds {LiteSpecHome.ManifestFile}.");
            return 2;
        }

        if (!File.Exists(Path.Combine(root, "litespec.project.json")))
        {
            Console.Error.WriteLine($"No litespec.project.json at {root}");
            return 2;
        }

        try
        {
            var report = Validate(root);
            if (parsed.Json)
            {
                CliJson.Write(report.ToJson());
                return report.ExitCode;
            }

            Console.WriteLine($"Consumer: {report.Consumer}");
            Console.WriteLine(report.SharedPackage is null
                ? "Shared UI package: none declared"
                : $"Shared UI package: {JsonRead.Show(report.SharedPackage.NameFound, report.SharedPackage.Name)} at {JsonRead.Show(report.SharedPackage.PathFound, report.SharedPackage.Path)}");
            Console.WriteLine($"Controlled frontends: {report.ControlledFrontends}");
            if (report.SkippedFrontends.Count > 0)
            {
                Console.WriteLine($"Skipped frontends: {string.Join(", ", report.SkippedFrontends.Select(SkippedText))}");
            }

            if (report.Problems.Count > 0)
            {
                Console.WriteLine($"\nFAIL ({report.Problems.Count} problem(s)):");
                foreach (var item in report.Problems)
                {
                    Console.WriteLine($"  - {item}");
                }

                return report.ExitCode;
            }

            if (!report.HasPackage)
            {
                // A silent skip reads as conformance. Naming the frontends and the platform
                // each one declared makes the skipped scope visible in the output that a
                // reviewer reads. (litespec/frontend/ui#record-a-frontend-outside-the-controlled-contract)
                Console.WriteLine("PASS: no controlled web frontend is present.");
                if (report.Frontends.Count > 0)
                {
                    Console.WriteLine($"Skipped frontends: {string.Join(", ", report.Frontends.Select(DeclaredText))}");
                }
                else
                {
                    Console.WriteLine("Skipped frontends: none; the project declares no frontend.");
                }

                return report.ExitCode;
            }

            Console.WriteLine("\nPASS: the shared package, its source lock, its design contract, its stylesheets, each frontend boundary, and each route suite are valid.");
            return report.ExitCode;
        }
        catch (Exception exception)
        {
            return LastResort.Report(exception, parsed.Json, Console.Out, Console.Error);
        }
    }

    /// <summary>
    ///     Validates the consumer at <paramref name="consumerRoot"/> in-process.
    ///     Never writes to the console and never exits the process.
    /// </summary>
    /// <param name="consumerRoot">The consumer root to validate.</param>
    /// <returns>The collected report.</returns>
    public static UiReport Validate(string consumerRoot) => new UiSession(consumerRoot).Execute();

    /// <summary>
    ///     How a skipped frontend reads in the report.
    /// </summary>
    /// <param name="frontend">The declared frontend.</param>
    /// <returns>The text the frontend prints as.</returns>
    static string SkippedText(UiFrontend frontend) =>
        $"{JsonRead.Show(frontend.NameFound, frontend.Name)} (platform: {JsonRead.Show(frontend.PlatformFound, frontend.Platform)})";

    /// <summary>
    ///     How a declared frontend reads in the skipped scope line.
    /// </summary>
    /// <param name="frontend">The declared frontend.</param>
    /// <returns>The text the frontend prints as.</returns>
    static string DeclaredText(UiFrontend frontend) =>
        $"{JsonRead.Show(frontend.NameFound, frontend.Name)} (platform: {(frontend.Platform is null ? "undeclared" : JsonRead.ShowNode(frontend.Platform))})";
}
