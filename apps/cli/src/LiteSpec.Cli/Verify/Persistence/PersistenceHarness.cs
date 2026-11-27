using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model;

namespace LiteSpec.Cli.Verify.Persistence;

/// <summary>
///     Runs the consumer persistence harness through the platform shell and reads the
///     snapshot lines it prints, one JSON object per line.
/// </summary>
/// <follows>litespec/core/backend-persistence#evolve-stored-document-contracts-explicitly</follows>
internal static class PersistenceHarness
{
    /// <summary>The longest time the harness may run, the limit every harness call in LiteSpec uses.</summary>
    internal static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    /// <summary>One snapshot line the harness prints: the snapshot file, whether it reads, and why not.</summary>
    /// <param name="File">The snapshot path the harness names, or empty when the line names none.</param>
    /// <param name="Status">Whether the snapshot reads: <c>ok</c> or <c>failed</c>.</param>
    /// <param name="Message">Why the snapshot does not read, or empty when it reads.</param>
    internal sealed record Line(string File, string Status, string Message);

    /// <summary>Runs the harness from the consumer root with a mode and a snapshot folder.</summary>
    /// <param name="command">The harness command the project file names.</param>
    /// <param name="mode">The harness mode: <c>record</c> or <c>check</c>.</param>
    /// <param name="folder">The snapshot folder, relative to the consumer root.</param>
    /// <param name="root">The consumer root the harness runs in.</param>
    /// <returns>The captured harness run.</returns>
    internal static ProcessRunner.Result Run(string command, string mode, string folder, string root)
    {
        var script = $"{command} {mode} \"{folder}\"";

        if (OperatingSystem.IsWindows())
        {
            return RunWindows(script, root);
        }

        return ProcessRunner.Capture("/bin/sh", ["-c", script], root, Timeout);
    }

    /// <summary>Runs the harness through the Windows shell with a verbatim command line.</summary>
    /// <param name="script">The harness command with its mode and snapshot folder.</param>
    /// <param name="root">The consumer root the harness runs in.</param>
    /// <returns>The captured harness run.</returns>
    private static ProcessRunner.Result RunWindows(string script, string root)
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

            if (!process.WaitForExit(Timeout) || !stdout.Wait(Timeout))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // The harness exited between the wait and the kill.
                }

                return new ProcessRunner.Result(1, [], "cmd.exe did not finish in 300 seconds.", TimedOut: true);
            }

            return new ProcessRunner.Result(process.ExitCode, output.ToArray(), stderr.GetAwaiter().GetResult());
        }
        catch (Win32Exception)
        {
            return new ProcessRunner.Result(1, [], "cmd.exe is not available.");
        }
    }

    /// <summary>Reads every snapshot line the harness prints, in order, skipping blank lines.</summary>
    /// <param name="output">The standard output the harness run captured.</param>
    /// <returns>The parsed lines; a line that is not a snapshot result reads as failed.</returns>
    internal static List<Line> Parse(string output)
    {
        var lines = new List<Line>();

        foreach (var raw in output.Split('\n'))
        {
            var text = raw.Trim();

            if (text.Length == 0)
            {
                continue;
            }

            lines.Add(Read(text));
        }

        return lines;
    }

    /// <summary>Reads one harness line: a snapshot result, or a failed result when the line is not one.</summary>
    /// <param name="text">The trimmed output line.</param>
    /// <returns>The snapshot line the text states.</returns>
    private static Line Read(string text)
    {
        JsonNode? node;

        try
        {
            node = JsonNode.Parse(text);
        }
        catch (JsonException)
        {
            return new Line(string.Empty, "failed", $"Harness line is not a snapshot result: {text}.");
        }

        if (node is JsonObject item
            && item["file"] is JsonValue file
            && file.TryGetValue<string>(out var name)
            && item["status"] is JsonValue status
            && status.TryGetValue<string>(out var state)
            && state is "ok" or "failed"
            && item["message"] is JsonValue message
            && message.TryGetValue<string>(out var reason))
        {
            return new Line(name ?? string.Empty, state, reason ?? string.Empty);
        }

        return new Line(string.Empty, "failed", $"Harness line is not a snapshot result: {text}.");
    }
}
