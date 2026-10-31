using System.Text.Json.Nodes;

namespace LiteSpec;

/// <summary>
///     The last-resort handler for an unexpected exception in a command:
///     one line and exit code 1, never a stack trace.
/// </summary>
/// <follows>litespec/core/release#report-verification-exactly</follows>
internal static class LastResort
{
    /// <summary>Runs a command under the last-resort handler, choosing the output format from the arguments.</summary>
    /// <param name="command">The command body to run, returning its exit code.</param>
    /// <param name="args">The original command arguments, inspected for the JSON format request.</param>
    /// <returns>The command exit code, or 1 when the command throws an unexpected exception.</returns>
    public static int Run(Func<int> command, string[] args) =>
        Run(command, WantsJson(args), Console.Out, Console.Error);

    /// <summary>Runs a command and converts an unexpected exception into a reported failure.</summary>
    /// <param name="command">The command delegate to execute and shield from unhandled exceptions.</param>
    /// <param name="json">True when the caller requested JSON output.</param>
    /// <param name="stdout">The writer that receives the JSON error object.</param>
    /// <param name="stderr">The writer that receives the plain failure line.</param>
    /// <returns>The command exit code, or 1 when the command throws an unexpected exception.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "LastResort is the last-resort command handler: every unexpected exception becomes a one-line failure with exit code 1, so no exception type is excluded.")]
    public static int Run(Func<int> command, bool json, TextWriter stdout, TextWriter stderr)
    {
        try
        {
            return command();
        }
        catch (Exception exception)
        {
            return Report(exception, json, stdout, stderr);
        }
    }

    /// <summary>Writes the failure for an unexpected exception and returns exit code 1.</summary>
    /// <param name="exception">The unexpected exception the command raised.</param>
    /// <param name="json">True when the caller requested JSON output.</param>
    /// <param name="stdout">The writer that receives the JSON error object.</param>
    /// <param name="stderr">The writer that receives the plain failure line.</param>
    /// <returns>Exit code 1 in all cases.</returns>
    public static int Report(Exception exception, bool json, TextWriter stdout, TextWriter stderr)
    {
        if (json)
        {
            stdout.WriteLine(new JsonObject { ["error"] = exception.Message }.ToJsonString());
        }
        else
        {
            stderr.WriteLine($"Failed: {exception.Message}");
        }

        return 1;
    }

    /// <summary>True when the arguments ask for <c>--format json</c>.</summary>
    /// <param name="args">The command arguments to scan for a JSON format request.</param>
    /// <returns>True when an argument asks for the json format; otherwise false.</returns>
    public static bool WantsJson(string[] args)
    {
        for (var index = 0; index < args.Length; index++)
        {
            if (string.Equals(args[index], "--format=json", StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(args[index], "--format", StringComparison.Ordinal)
                && index + 1 < args.Length
                && string.Equals(args[index + 1], "json", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
