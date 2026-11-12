namespace LiteSpec.Cli;

/// <summary>
///     The process facts one command reads: where it writes, the folder it runs in, and its
///     environment. A command run by the executable reads the process itself. A command run
///     through <see cref="Run"/> reads the values its <see cref="CliRun"/> gives, so the test
///     suite runs many commands at once in one process, each with its own output.
/// </summary>
/// <follows>litespec/core/release#report-verification-exactly</follows>
static class CliHost
{
    /// <summary>The run of the command on this thread of execution, or null for the process itself.</summary>
    static readonly AsyncLocal<CliRun?> Current = new();

    /// <summary>Gets the writer for the command's standard output.</summary>
    public static TextWriter Out => Current.Value?.Out ?? Console.Out;

    /// <summary>Gets the writer for the command's standard error.</summary>
    public static TextWriter Error => Current.Value?.Error ?? Console.Error;

    /// <summary>Gets the folder the command runs in.</summary>
    public static string WorkingDirectory => Current.Value?.WorkingDirectory ?? Directory.GetCurrentDirectory();

    /// <summary>Reads an environment variable of the command: the run's value when it names one, else the process's.</summary>
    /// <param name="name">The variable name.</param>
    /// <returns>The value, or null when the variable is not set.</returns>
    public static string? Variable(string name) =>
        Current.Value is { } run && run.Variables.TryGetValue(name, out var value)
            ? value
            : Environment.GetEnvironmentVariable(name);

    /// <summary>Runs one command with the run's output, folder and environment.</summary>
    /// <param name="run">The output writers, working folder and variables of this command.</param>
    /// <param name="args">The command line, as the executable receives it.</param>
    /// <returns>The command's exit code.</returns>
    public static int Run(CliRun run, string[] args)
    {
        var previous = Current.Value;
        Current.Value = run;

        try
        {
            return CliEntry.Run(args);
        }
        finally
        {
            Current.Value = previous;
        }
    }
}
