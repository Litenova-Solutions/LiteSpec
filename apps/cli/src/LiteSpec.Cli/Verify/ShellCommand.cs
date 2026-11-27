using System.ComponentModel;
using System.Diagnostics;
using LiteSpec.Cli.Model;

namespace LiteSpec.Cli.Verify;

/// <summary>Runs one consumer command line through the platform shell from the consumer root and captures its output.</summary>
/// <follows>litespec/quality/testing#prove-each-scenario-through-the-public-boundary</follows>
internal static class ShellCommand
{
    /// <summary>Runs the command line through the platform shell.</summary>
    /// <param name="script">The command line.</param>
    /// <param name="root">The consumer root the command runs in.</param>
    /// <param name="timeout">The longest time the command may run.</param>
    /// <param name="environment">The variables set in the command's environment, or null for none.</param>
    /// <returns>The captured run.</returns>
    internal static ProcessRunner.Result Run(
        string script, string root, TimeSpan timeout, IReadOnlyDictionary<string, string>? environment = null) =>
        OperatingSystem.IsWindows()
            ? RunWindows(script, root, timeout, environment)
            : ProcessRunner.Capture("/bin/sh", ["-c", script], root, timeout, environment: environment);

    /// <summary>Runs the command through the Windows shell with a verbatim command line.</summary>
    /// <param name="script">The command line.</param>
    /// <param name="root">The consumer root the command runs in.</param>
    /// <param name="timeout">The longest time the command may run.</param>
    /// <param name="environment">The variables set in the command's environment, or null for none.</param>
    /// <returns>The captured run.</returns>
    private static ProcessRunner.Result RunWindows(
        string script, string root, TimeSpan timeout, IReadOnlyDictionary<string, string>? environment)
    {
        // The argument list escapes inner quotes with a backslash, which the Windows shell
        // does not read, so the shell takes its command line verbatim here instead. The one
        // outer pair with /S keeps every inner quote the harness command carries intact.
        var start = new ProcessStartInfo("cmd.exe")
        {
            Arguments = "/D /S /C \"" + script + "\"",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = root,
        };

        foreach (var (name, value) in environment ?? new Dictionary<string, string>())
        {
            start.Environment[name] = value;
        }

        try
        {
            using var process = Process.Start(start)!;
            using var output = new MemoryStream();
            var stdout = Task.Run(() => process.StandardOutput.BaseStream.CopyTo(output));
            var stderr = process.StandardError.ReadToEndAsync();

            try
            {
                process.StandardInput.Write(string.Empty);
                process.StandardInput.Close();
            }
            catch (IOException)
            {
                // The harness exited before reading its input; its exit code and output report why.
            }

            if (!process.WaitForExit(timeout) || !stdout.Wait(timeout))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // The harness exited between the wait and the kill.
                }

                return new ProcessRunner.Result(1, [], $"cmd.exe did not finish in {timeout.TotalSeconds:0} seconds.", TimedOut: true);
            }

            return new ProcessRunner.Result(process.ExitCode, output.ToArray(), stderr.GetAwaiter().GetResult());
        }
        catch (Win32Exception)
        {
            return new ProcessRunner.Result(1, [], "cmd.exe is not available.");
        }
    }

}
