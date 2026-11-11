using LiteSpec.Cli;
using LiteSpec.Cli.Book;
using LiteSpec.Cli.Init;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.RecordEditing;
using LiteSpec.Cli.Rules;
using LiteSpec.Standards;

// Messages quote page text verbatim, so the console must write UTF-8 rather
// than the Windows code page, which turns a non-ASCII character into a control byte.
Console.OutputEncoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

const string usage = """
    Usage: litespec <command> [options]

    Commands in a consumer repository:
      init          Write .litespec/project.yaml from a preset.
      new           Scaffold a record page and reserve its identifier.
      rename        Rename a record identifier and rewrite every link.
      model         Check, format, render or query the project model.
      book          Generate the project book from the records.
      rules         List the provisions that govern a path or record.

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
        "model" => ModelCommand.Run(rest),
        "book" => BookCommand.Run(rest),
        "rules" => RulesCommand.Run(rest),
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

int Unknown(string command) =>
    LastResort.UsageError($"Unknown command '{command}'.", usage, json: false);
