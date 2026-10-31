using System.Text.Json.Nodes;
using LiteSpec;
using LiteSpec.Authoring;
using LiteSpec.Book;
using LiteSpec.Consumer;
using LiteSpec.Init;
using LiteSpec.Model;
using LiteSpec.Ui;

// Messages quote page text verbatim, so the console must write UTF-8 rather
// than the Windows code page, which turns a non-ASCII character into a control byte.
Console.OutputEncoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

const string usage = """
    Usage: litespec <command> [options]

    Commands in a consumer repository:
      init          Write litespec.project.json from a preset.
      new           Scaffold a record page and reserve its identifier.
      rename        Rename a record identifier and rewrite every link.
      consumer      Validate the consumer's project file, records and language.
      ui            Validate the controlled web UI contract.
      model         Build, check, render or query the project model.
      book          Generate the project book from model.json.

    Run 'litespec <command> --help' for a command's options.
    'litespec --version' prints the LiteSpec release this tool ships in.
    """;

if (args.Length == 0)
{
    Console.Error.WriteLine(usage);
    return 2;
}

var rest = args[1..];

return LastResort.Run(
    () => args[0] switch
    {
        "--help" or "-h" or "help" => Help(),
        "--version" => Version(),
        "init" => InitCommand.Run(rest),
        "new" => NewCommand.Run(rest),
        "rename" => RenameCommand.Run(rest),
        "consumer" => ConsumerValidator.Run(rest),
        "ui" => UiValidator.Run(rest),
        "model" => ModelCommand.Run(rest),
        "book" => BookCommand.Run(rest),
        _ => Unknown(args[0]),
    },
    args);

int Help()
{
    Console.WriteLine(usage);
    return 0;
}

int Version()
{
    Console.WriteLine(LiteSpecHome.Version ?? "unknown");
    return 0;
}

int Unknown(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'.");
    Console.Error.WriteLine(usage);
    return 2;
}

namespace LiteSpec
{
    /// <summary>
    ///     The last-resort handler for an unexpected exception in a command:
    ///     one line and exit code 1, never a stack trace.
    /// </summary>
    /// <follows>litespec/core/release#report-verification-exactly</follows>
    internal static class LastResort
    {
        /// <summary>Runs <paramref name="command"/> and converts an unexpected exception.</summary>
        public static int Run(Func<int> command, string[] args) =>
            Run(command, WantsJson(args), Console.Out, Console.Error);

        /// <summary>Runs <paramref name="command"/> and converts an unexpected exception.</summary>
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

        /// <summary>Writes the failure for <paramref name="exception"/> and returns exit code 1.</summary>
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
}
