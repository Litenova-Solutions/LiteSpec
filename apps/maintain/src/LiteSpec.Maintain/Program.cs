using LiteSpec;
using LiteSpec.Authoring;

// Messages quote page text verbatim, so the console must write UTF-8 rather
// than the Windows code page, which turns a non-ASCII character into a control byte.
Console.OutputEncoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

const string usage = """
    Usage: litespec-maintain <command> [options]

    Commands:
      validate      Check the authoring contract of the LiteSpec repository.
      provisions    Write or check docs/reference/provisions.md.

    Run 'litespec-maintain <command> --help' for a command's options.
    'litespec-maintain --version' prints the LiteSpec release of the checkout.
    """;

if (args.Length == 0)
{
    Console.Error.WriteLine(usage);
    return 2;
}

var rest = args[1..];

return args[0] switch
{
    "--help" or "-h" or "help" => Help(),
    "--version" => Version(),
    "validate" => StandardsValidator.Run(rest),
    "provisions" => ProvisionsCommand.Run(rest),
    _ => Unknown(args[0]),
};

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
