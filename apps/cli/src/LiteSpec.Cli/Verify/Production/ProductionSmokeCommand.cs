using System.Text;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.CommandLine;
using LiteSpec.Model;

namespace LiteSpec.Cli.Verify.Production;

/// <summary>The <c>litespec verify production-smoke</c> command: boots the production containers and probes them.</summary>
/// <follows>litespec/core/release#boot-the-production-configuration-before-release</follows>
static class ProductionSmokeCommand
{
    /// <summary>The usage text for the production smoke command, printed on help and on usage errors.</summary>
    internal const string Usage = """
        Usage: litespec verify production-smoke [--root <consumer>] [--base-url <url>] [--help]

        Boots the production containers from the verify block, waits for the readiness path,
        requests each smoke path behind a forwarded https scheme, and reports an unhealthy host,
        a failing path, or a stub adapter serving production traffic. The down command always runs.

          --root        The consumer repository to read. Defaults to the working directory.
          --base-url    The production address to probe. Defaults to the verify block address.
          --help        Print this text and exit.

        Exit codes: 0 the smoke raised no finding, 1 the smoke raised a finding or could not run, 2 the options are invalid or the verify block names no production.
        """;

    /// <summary>Runs the production smoke command.</summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code: 0 for a clean smoke, 1 for a finding or a failed run, 2 for a usage error.</returns>
    internal static int Run(string[] args) =>
        Run(CliHost.Out, CliHost.Error, CliHost.WorkingDirectory, args, new ProductionProcessRunner(), null, null, null);

    /// <summary>Runs the production smoke with the given writers, folder, and process runner.</summary>
    /// <param name="output">The writer for standard output.</param>
    /// <param name="error">The writer for standard error.</param>
    /// <param name="workingDirectory">The folder the command runs in when --root is absent.</param>
    /// <param name="args">The command arguments.</param>
    /// <param name="processes">The runner that starts the container commands.</param>
    /// <param name="healthTimeout">How long the health wait polls, or null for the 120 second default.</param>
    /// <param name="pollInterval">How long the health wait rests between polls, or null for the default.</param>
    /// <param name="requestTimeout">How long one probe request may take, or null for the default.</param>
    /// <returns>The exit code: 0 for a clean smoke, 1 for a finding or a failed run, 2 for a usage error.</returns>
    internal static int Run(
        TextWriter output,
        TextWriter error,
        string workingDirectory,
        string[] args,
        IProductionProcessRunner processes,
        TimeSpan? healthTimeout,
        TimeSpan? pollInterval,
        TimeSpan? requestTimeout)
    {
        var parsed = CliArgs.Parse(
            args,
            [],
            ["--root", "--base-url"],
            maxPositionals: 0);

        if (parsed.Help)
        {
            output.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is { } parseError)
        {
            error.WriteLine(parseError);
            error.WriteLine(Usage);
            return 2;
        }

        if (parsed.Value("--base-url") is { Length: 0 })
        {
            error.WriteLine("Option --base-url needs a value.");
            error.WriteLine(Usage);
            return 2;
        }

        var config = OpenConsumer(parsed.Value("--root"), workingDirectory, error, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        if (config.Verify.Production is not { } production)
        {
            error.WriteLine($"The verify block of {ConsumerRoot.ProjectFileDisplay} names no production; add the compose file, the env file, the base address, and the readiness path.");
            error.WriteLine(Usage);
            return 2;
        }

        var baseUrl = parsed.Value("--base-url") ?? production.BaseUrl;
        IReadOnlyList<ProductionSmokeFinding> findings;

        try
        {
            findings = ProductionSmokeRunner.Run(
                production,
                baseUrl,
                processes,
                config.Root,
                healthTimeout ?? TimeSpan.FromSeconds(120),
                pollInterval ?? TimeSpan.FromSeconds(2),
                requestTimeout ?? TimeSpan.FromSeconds(10));
        }
        catch (Exception exception) when (exception is UriFormatException or InvalidOperationException or HttpRequestException)
        {
            error.WriteLine($"Cannot run the production smoke: {exception.Message}");
            return 1;
        }

        foreach (var finding in findings)
        {
            output.WriteLine(finding.Line(ConsumerRoot.ProjectFileDisplay));
        }

        var reportPath = Path.Combine(config.Root, ConsumerRoot.FolderName, "verify", ProductionSmokeReport.ReportFileName);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            File.WriteAllText(reportPath, CliJson.Serialize(ProductionSmokeReport.AsJson(baseUrl, production, findings)), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            error.WriteLine($"Cannot write '{reportPath}': {exception.Message}");
            return 1;
        }

        output.WriteLine($"Wrote {reportPath}.");

        return findings.Count > 0 ? 1 : 0;
    }

    /// <summary>Finds the consumer the command reads, writing a usage error to the given writer when it cannot.</summary>
    /// <param name="rootOption">The --root value, or null when it was not passed.</param>
    /// <param name="workingDirectory">The folder the command runs in when --root is absent.</param>
    /// <param name="error">The writer for the usage error.</param>
    /// <param name="exitCode">The exit code for the caller to return when the consumer is refused; 0 otherwise.</param>
    /// <returns>The loaded declaration, or null when the consumer is refused.</returns>
    static ConsumerConfig? OpenConsumer(string? rootOption, string workingDirectory, TextWriter error, out int exitCode)
    {
        exitCode = 0;
        string root;

        try
        {
            var start = new DirectoryInfo(Path.GetFullPath(string.IsNullOrEmpty(rootOption) ? workingDirectory : rootOption));

            root = start.FullName;

            for (var current = start; current is not null; current = current.Parent)
            {
                if (File.Exists(Path.Combine(current.FullName, ConsumerRoot.FolderName, ConsumerRoot.ProjectFileName)))
                {
                    root = current.FullName;
                    break;
                }
            }
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            error.WriteLine($"Invalid root '{rootOption}': {exception.Message}");
            exitCode = 2;
            return null;
        }

        if (!Directory.Exists(root))
        {
            error.WriteLine($"Root '{root}' does not exist.");
            exitCode = 2;
            return null;
        }

        if (!File.Exists(ConsumerRoot.ProjectFile(root)))
        {
            error.WriteLine(ConsumerRoot.NoProjectMessage(root));
            exitCode = 2;
            return null;
        }

        try
        {
            return ConsumerConfig.Load(root);
        }
        catch (JsonTextException exception)
        {
            error.WriteLine(ConsumerRoot.InvalidProjectMessage(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            error.WriteLine(exception.Message);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"Cannot read {ConsumerRoot.ProjectFileDisplay}: {exception.Message}");
        }

        exitCode = 2;
        return null;
    }
}
