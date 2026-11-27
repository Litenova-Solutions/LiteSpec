using System.Text;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.CommandLine;

namespace LiteSpec.Cli.Audit;

/// <summary>The <c>litespec ui audit</c> command: audits the rendered pages in a browser.</summary>
/// <follows>litespec/core/release#report-verification-exactly</follows>
static class AuditCommand
{
    /// <summary>Runs the audit: collects the routes, visits each in a browser, and writes the report.</summary>
    /// <param name="args">The command arguments after the subcommand.</param>
    /// <returns>The exit code: 0 for no findings, 1 for findings with --fail-on blocking, 2 for a usage error.</returns>
    public static int Run(string[] args)
    {
        var parsed = CliArgs.Parse(
            args,
            [],
            ["--root", "--base-url", "--routes", "--widths", "--schemes", "--storage-state", "--out", "--fail-on", "--primary-selector", "--browser"],
            maxPositionals: 0);

        if (parsed.Help)
        {
            CliHost.Out.WriteLine(UiCommand.Usage);
            return 0;
        }

        if (parsed.Error is { } error)
        {
            return CommandFailure.UsageError(error, UiCommand.Usage, json: false);
        }

        var (options, optionsError) = AuditOptions.Parse(parsed);

        if (options is null)
        {
            return CommandFailure.UsageError(optionsError!, UiCommand.Usage, json: false);
        }

        var config = ConsumerRoot.Open(options.Root, json: false, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        var routes = ReadRoutes(config, options, out var routesExit);

        if (routes is null)
        {
            return routesExit;
        }

        if (routes.Count == 0)
        {
            CliHost.Out.WriteLine("No routes to audit.");
            return 0;
        }

        string? storageFull = null;

        if (options.StorageState is { } storageOption)
        {
            storageFull = FullPath(storageOption);

            if (!File.Exists(storageFull))
            {
                return CommandFailure.UsageError($"Storage state file '{storageOption}' does not exist.", UiCommand.Usage, json: false);
            }
        }

        string? browserFull = null;

        if (options.Browser is { } browserOption)
        {
            browserFull = FullPath(browserOption);

            if (!File.Exists(browserFull))
            {
                return CommandFailure.UsageError($"Browser '{browserOption}' does not exist.", UiCommand.Usage, json: false);
            }
        }

        var outDir = FullPath(options.Out);

        try
        {
            Directory.CreateDirectory(outDir);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return CommandFailure.Fail($"Cannot write '{options.Out}': {exception.Message}", json: false, code: 1);
        }

        string script;

        try
        {
            script = ReadScript();
        }
        catch (InvalidOperationException exception)
        {
            return CommandFailure.Fail(exception.Message, json: false, code: 1);
        }

        AuditBrowser.Run? run;

        try
        {
            run = AuditBrowser.RunAsync(options with { StorageState = storageFull, Browser = browserFull }, routes, script, outDir).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException exception)
        {
            return CommandFailure.Fail(exception.Message, json: false, code: 1);
        }

        if (run is null)
        {
            return CommandFailure.Fail(
                "No browser is available. Install Google Chrome, Microsoft Edge, or Chromium, or pass --browser <path>, and run the audit again.",
                json: false,
                code: 1);
        }

        var findings = AuditReport.Merge(run.Raw, routes);

        foreach (var finding in findings)
        {
            CliHost.Out.WriteLine(AuditReport.Line(finding));
        }

        var reportPath = Path.Combine(outDir, AuditReport.ReportFileName);

        try
        {
            File.WriteAllText(
                reportPath,
                CliJson.Serialize(AuditReport.AsJson(options.BaseUrl, routes, options.Widths, options.Schemes, findings, run.Screenshots)),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return CommandFailure.Fail($"Cannot write '{reportPath}': {exception.Message}", json: false, code: 1);
        }

        try
        {
            AuditCheckResult.Write(config, routes, findings);
        }
        catch (InvalidOperationException exception)
        {
            return CommandFailure.Fail(exception.Message, json: false, code: 1);
        }

        if (findings.Count == 0)
        {
            CliHost.Out.WriteLine($"No findings across {routes.Count} routes.");
        }
        else
        {
            CliHost.Out.WriteLine();
            CliHost.Out.WriteLine($"{findings.Count} findings across {routes.Count} routes.");
        }

        CliHost.Out.WriteLine($"Wrote {reportPath} ({run.Screenshots.Count} screenshots).");

        return findings.Count > 0 && options.FailOn == "blocking" ? 1 : 0;
    }

    /// <summary>Reads the audited routes: from the routes file, or from the screen records.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="options">The parsed audit options.</param>
    /// <param name="exitCode">The exit code for the caller to return when the routes cannot be read.</param>
    /// <returns>The routes in order, or null when they cannot be read.</returns>
    static IReadOnlyList<AuditRoute>? ReadRoutes(ConsumerConfig config, AuditOptions options, out int exitCode)
    {
        exitCode = 0;

        if (options.RoutesFile is { } routesOption)
        {
            var routesPath = FullPath(routesOption);

            if (!File.Exists(routesPath))
            {
                exitCode = CommandFailure.UsageError($"Routes file '{routesOption}' does not exist.", UiCommand.Usage, json: false);
                return null;
            }

            var (fromFile, routesError) = AuditRoutes.ReadFile(routesPath);

            if (fromFile is null)
            {
                exitCode = CommandFailure.UsageError(routesError!, UiCommand.Usage, json: false);
                return null;
            }

            return fromFile;
        }

        var read = RecordReader.Read(config);

        if (read.Problems is [var first, ..])
        {
            exitCode = CommandFailure.Fail($"Cannot read the records: {first.Message}", json: false, code: 1);
            return null;
        }

        return AuditRoutes.Collect(read.Records);
    }

    /// <summary>The absolute path of an option value: as written when absolute, else under the working directory.</summary>
    /// <param name="path">The option value.</param>
    /// <returns>The absolute path.</returns>
    static string FullPath(string path) =>
        Path.IsPathFullyQualified(path) ? path : Path.Combine(CliHost.WorkingDirectory, path);

    /// <summary>Reads the embedded audit script.</summary>
    /// <returns>The script text.</returns>
    static string ReadScript()
    {
        using var stream = typeof(AuditCommand).Assembly.GetManifestResourceStream("LiteSpec.Audit.audit.js")
            ?? throw new InvalidOperationException("Missing embedded asset 'audit.js'.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
