using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace LiteSpec;

/// <summary>
///     Runs an external program without a shell and captures what it writes. Every
///     caller that starts a process (git, Node) goes through this type, so a timeout,
///     a missing program and a killed process read the same way everywhere.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
public static class ProcessRunner
{
    /// <summary>Runs git in a folder with five minutes allowed, the limit every git call in LiteSpec uses.</summary>
    /// <param name="workdir">The folder git runs in.</param>
    /// <param name="args">The git arguments, in order.</param>
    /// <returns>The captured result of the git run.</returns>
    public static Result Git(string workdir, params string[] args) =>
        Capture("git", args, workdir, TimeSpan.FromMinutes(5));

    /// <summary>Runs a program and waits for it, killing it when the timeout passes.</summary>
    /// <param name="file">The program to start, found on the PATH when it is not a full path.</param>
    /// <param name="args">The arguments, passed one by one without a shell.</param>
    /// <param name="workdir">The folder the program runs in.</param>
    /// <param name="timeout">The longest time the program may run.</param>
    /// <param name="stdin">The text written to the program's standard input, or null for none.</param>
    /// <returns>The exit status with the captured output bytes and error text. A timeout and a missing program report status 1.</returns>
    public static Result Capture(
        string file, IReadOnlyList<string> args, string workdir, TimeSpan timeout, string? stdin = null)
    {
        var start = new ProcessStartInfo(file)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workdir,
        };

        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        try
        {
            using var process = Process.Start(start)!;
            using var output = new MemoryStream();
            var stdout = Task.Run(() => process.StandardOutput.BaseStream.CopyTo(output));
            var stderr = process.StandardError.ReadToEndAsync();

            try
            {
                process.StandardInput.Write(stdin ?? "");
                process.StandardInput.Close();
            }
            catch (IOException)
            {
                // The process exited before reading its input; its exit code and output report why.
            }

            if (!process.WaitForExit(timeout) || !stdout.Wait(timeout))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // The process exited between the wait and the kill.
                }

                return new Result(1, [], $"{file} did not finish in {timeout.TotalSeconds:0} seconds.", TimedOut: true);
            }

            return new Result(process.ExitCode, output.ToArray(), stderr.GetAwaiter().GetResult());
        }
        catch (Win32Exception)
        {
            return new Result(1, [], $"{file} is not available.");
        }
    }

    /// <summary>
    ///     What a captured program left behind: its exit status, its standard output as bytes,
    ///     and its standard error as text.
    /// </summary>
    /// <param name="Status">The exit status; 1 when the program timed out or could not start.</param>
    /// <param name="Output">The bytes the program wrote to standard output.</param>
    /// <param name="Error">The text the program wrote to standard error, or the runner's own message.</param>
    /// <param name="TimedOut">Whether the runner killed the program for running past its timeout.</param>
    public sealed record Result(int Status, byte[] Output, string Error, bool TimedOut = false)
    {
        /// <summary>Gets the standard output decoded as UTF-8 text.</summary>
        public string Stdout => Encoding.UTF8.GetString(Output);
    }
}
