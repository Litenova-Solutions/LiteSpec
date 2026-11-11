using System.Diagnostics;

namespace LiteSpec.Cli.Tests.Support;

/// <summary>
///     Runs the <c>litespec</c> CLI so a case proves the exit code and the printed text
///     together: in this process through the CLI's own dispatch by default, or as its own
///     process for a native executable, for <c>LITESPEC_TEST_PROCESS</c>, and for a command
///     that sets its own <c>LITESPEC_HOME</c>.
/// </summary>
static class TestCli
{
    /// <summary>Gets the LiteSpec repository root, found above the test binary.</summary>
    public static string Repository { get; } = FindRepository();

    /// <summary>A folder under the test fixtures, from forward-slash segments.</summary>
    /// <param name="relative">The fixture path with forward-slash segments.</param>
    /// <returns>The full path to the fixture folder.</returns>
    public static string Fixture(string relative) =>
        Path.Combine(Repository, "apps", "cli", "tests", "LiteSpec.Cli.Tests", "Fixtures", relative.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>
    ///     Gets the native <c>litespec</c> executable the suite runs instead of the built
    ///     <c>litespec.dll</c>, from <c>LITESPEC_TEST_EXECUTABLE</c>, or null to run the build
    ///     output on the .NET runtime. CI sets it on each platform, so the one suite proves
    ///     the native binary a consumer installs.
    /// </summary>
    public static string? Executable { get; } = Environment.GetEnvironmentVariable("LITESPEC_TEST_EXECUTABLE") is { Length: > 0 } executable
        ? Path.GetFullPath(executable)
        : null;

    /// <summary>Runs <c>litespec</c> with the arguments, in the working directory when one is given.</summary>
    /// <param name="args">The command arguments.</param>
    /// <param name="workingDirectory">The working directory, or null for the repository root.</param>
    /// <param name="environment">Extra environment variables, or null for none.</param>
    /// <returns>The exit code and the two output streams of the run.</returns>
    public static CliResult Run(IEnumerable<string> args, string? workingDirectory = null, IDictionary<string, string>? environment = null)
    {
        var argList = args.ToArray();
        return RunsInProcess(environment)
            ? RunInProcess(argList, workingDirectory, environment)
            : RunProcess(argList, workingDirectory, environment);
    }

    /// <summary>
    ///     Gets a value indicating whether commands run inside the test process. They do unless the suite runs a
    ///     native executable, or <c>LITESPEC_TEST_PROCESS</c> asks for one process per command.
    /// </summary>
    public static bool InProcess { get; } =
        Executable is null && Environment.GetEnvironmentVariable("LITESPEC_TEST_PROCESS") is not { Length: > 0 };

    /// <summary>
    ///     Whether this command can run in-process. A command that sets its own
    ///     <c>LITESPEC_HOME</c> runs as a process: the release root is read once per process.
    /// </summary>
    /// <param name="environment">The variables the command sets, or null for none.</param>
    /// <returns>True when the command runs in this process.</returns>
    static bool RunsInProcess(IDictionary<string, string>? environment) =>
        InProcess
        && (environment is null
            || !environment.TryGetValue("LITESPEC_HOME", out var home)
            || Path.GetFullPath(home) == Path.GetFullPath(Repository));

    /// <summary>
    ///     Runs the command through the CLI's own dispatch in this process, with its own output,
    ///     folder and variables, so a command costs no process start or JIT warm-up. The native
    ///     CI jobs run the same cases against the installed executable.
    /// </summary>
    /// <param name="args">The command arguments.</param>
    /// <param name="workingDirectory">The working directory, or null for the repository root.</param>
    /// <param name="environment">Extra environment variables, or null for none.</param>
    /// <returns>The exit code and the two output streams of the run.</returns>
    static CliResult RunInProcess(string[] args, string? workingDirectory, IDictionary<string, string>? environment)
    {
        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["LITESPEC_HOME"] = Repository,
            ["CI"] = "",
        };

        foreach (var (key, value) in environment ?? new Dictionary<string, string>())
        {
            variables[key] = value;
        }

        using var output = new StringWriter();
        using var error = new StringWriter();
        var stdout = TextWriter.Synchronized(output);
        var stderr = TextWriter.Synchronized(error);
        var status = CliHost.Run(new CliRun(stdout, stderr, workingDirectory ?? Repository, variables), args);
        stdout.Flush();
        stderr.Flush();
        return new CliResult(status, output.ToString(), error.ToString());
    }

    /// <summary>Runs the command as its own process: the native executable, or the built CLI on the .NET runtime.</summary>
    /// <param name="args">The command arguments.</param>
    /// <param name="workingDirectory">The working directory, or null for the repository root.</param>
    /// <param name="environment">Extra environment variables, or null for none.</param>
    /// <returns>The exit code and the two output streams of the run.</returns>
    static CliResult RunProcess(string[] args, string? workingDirectory, IDictionary<string, string>? environment)
    {
        var start = new ProcessStartInfo(Executable ?? "dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory ?? Repository,
        };

        if (Executable is null)
        {
            start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "litespec.dll"));
        }

        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        start.Environment["LITESPEC_HOME"] = Repository;

        // The check blocks on planned Intent and on a missing reader only in continuous
        // integration, so every run starts outside it; a test that needs CI sets it.
        start.Environment["CI"] = "";

        foreach (var (key, value) in environment ?? new Dictionary<string, string>())
        {
            start.Environment[key] = value;
        }

        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(TimeSpan.FromMinutes(3)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"litespec {string.Join(' ', args)} did not finish in three minutes.");
        }

        return new CliResult(process.ExitCode, stdout.Result, stderr.Result);
    }

    /// <summary>Runs <c>litespec</c> with the arguments.</summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code and the two output streams of the run.</returns>
    public static CliResult Run(params string[] args) => Run((IEnumerable<string>)args);

    /// <summary>A fresh, empty temporary folder that the caller deletes.</summary>
    /// <param name="prefix">The prefix in the temporary folder name.</param>
    /// <returns>The fresh temporary folder.</returns>
    public static TempFolder Temp(string prefix) => new(prefix);

    static string FindRepository()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "litespec.manifest.yaml")))
            {
                return folder.FullName;
            }
        }

        throw new InvalidOperationException("The test binary sits outside a LiteSpec checkout.");
    }
}
