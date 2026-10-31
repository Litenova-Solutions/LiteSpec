using System.Diagnostics;

namespace LiteSpec.Tests.Support;

/// <summary>
///     Runs the built <c>litespec-maintain</c> tool as its own process, the way a
///     maintainer runs it, so a case proves the exit code and the printed text together.
/// </summary>
static class Cli
{
    /// <summary>Gets the LiteSpec repository root, found above the test binary.</summary>
    public static string Repository { get; } = FindRepository();

    /// <summary>Runs <c>litespec-maintain</c> with the arguments, in the working directory when one is given.</summary>
    /// <param name="args">The command arguments.</param>
    /// <param name="workingDirectory">The working directory, or null for the repository root.</param>
    /// <param name="environment">Extra environment variables, or null for none.</param>
    /// <returns>The exit code and the two output streams of the run.</returns>
    public static CliResult Run(IEnumerable<string> args, string? workingDirectory = null, IDictionary<string, string>? environment = null)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory ?? Repository,
        };

        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "litespec-maintain.dll"));

        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        start.Environment["LITESPEC_HOME"] = Repository;

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
            throw new TimeoutException($"litespec-maintain {string.Join(' ', args)} did not finish in three minutes.");
        }

        return new CliResult(process.ExitCode, stdout.Result, stderr.Result);
    }

    /// <summary>Runs <c>litespec-maintain</c> with the arguments.</summary>
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
            if (File.Exists(Path.Combine(folder.FullName, "litespec.manifest.json")))
            {
                return folder.FullName;
            }
        }

        throw new InvalidOperationException("The test binary sits outside a LiteSpec checkout.");
    }
}
