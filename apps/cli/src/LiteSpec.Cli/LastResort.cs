using System.Text.Json.Nodes;
using LiteSpec.CommandLine;

namespace LiteSpec;

/// <summary>
///     The last-resort handler for an unexpected exception in a command, and the one
///     way a command reports a failure: one line and exit code 1 for an exception, never
///     a stack trace. Under <c>--format json</c> a failure is one object with one
///     <c>error</c> string on standard output; otherwise it is the message on standard
///     error, with the usage text after a usage error.
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
            stdout.WriteLine(Json(exception.Message));
        }
        else
        {
            stderr.WriteLine($"Failed: {exception.Message}");
        }

        return 1;
    }

    /// <summary>Writes one failure in the requested format and returns its exit code.</summary>
    /// <param name="message">The failure message.</param>
    /// <param name="json">True when the caller requested JSON output.</param>
    /// <param name="code">The exit code the command returns.</param>
    /// <returns>The exit code, <paramref name="code"/>.</returns>
    public static int Fail(string message, bool json, int code)
    {
        if (json)
        {
            Console.Out.WriteLine(Json(message));
        }
        else
        {
            Console.Error.WriteLine(message);
        }

        return code;
    }

    /// <summary>Writes a usage error: the message and the usage text in text, the failure object in JSON. Returns exit code 2.</summary>
    /// <param name="message">The usage error message.</param>
    /// <param name="usage">The command's usage text, written after the message in text output.</param>
    /// <param name="json">True when the caller requested JSON output.</param>
    /// <returns>Exit code 2.</returns>
    public static int UsageError(string message, string usage, bool json)
    {
        if (json)
        {
            return Fail(message, json, 2);
        }

        Console.Error.WriteLine(message);
        Console.Error.WriteLine(usage);
        return 2;
    }

    /// <summary>The JSON text of one failure: an object with one <c>error</c> string, the shape every JSON failure takes.</summary>
    /// <param name="message">The failure message.</param>
    /// <returns>The JSON text written with the shared serializer options.</returns>
    public static string Json(string message) =>
        CliJson.Serialize(new JsonObject { ["error"] = message });

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
