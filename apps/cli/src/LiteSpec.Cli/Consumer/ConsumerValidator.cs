namespace LiteSpec.Consumer;

/// <summary>
///     The <c>litespec consumer</c> command: holds a consumer repository to the release it pinned.
///     It checks the project record, the manifest version and the named stacks and extensions,
///     every page's frontmatter and required sections, the glossary with
///     the rejected words, and the controlled UI through the UI validator.
/// </summary>
/// <follows>litespec/core/authoring#declare-structured-specification-metadata</follows>
/// <follows>litespec/core/authoring#record-the-reviewed-litespec-release</follows>
/// <follows>litespec/core/authoring#reject-a-recorded-synonym-inside-its-scope</follows>
static class ConsumerValidator
{
    /// <summary>
    ///     The help text printed by <c>--help</c> and by every usage error.
    /// </summary>
    public const string Usage = """
        Usage: litespec consumer [--root <path>] [--format json] [--help]

        Validates the consumer repository at --root, which defaults to the
        current directory. The consumer must contain litespec.project.json.

          --root <path>   The consumer repository. Defaults to the current directory.
          --format json   Write one JSON object on stdout instead of human-readable lines.
          --help          Print this text and exit.

        Exit codes: 0 no problem, 1 at least one problem, 2 usage error.
        """;

    /// <summary>
    ///     Validates the consumer repository and returns the exit code.
    /// </summary>
    /// <param name="args">The command arguments after <c>consumer</c>.</param>
    /// <returns>The exit code: 0 no problem, 1 at least one problem, 2 usage error.</returns>
    public static int Run(string[] args)
    {
        var parsed = CliArgs.Parse(args, [], ["--root", "--format"]);

        if (parsed.Help)
        {
            Console.WriteLine(Usage);
            return 0;
        }

        var error = parsed.Error ?? parsed.FormatError;

        if (error is not null)
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(Usage);
            return 2;
        }

        var rootOption = parsed.Value("--root");
        ConsumerRun run;

        try
        {
            run = new ConsumerRun(
                Path.GetFullPath(string.IsNullOrEmpty(rootOption) ? Directory.GetCurrentDirectory() : rootOption),
                parsed.Json);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Console.Error.WriteLine($"Invalid root '{rootOption}': {exception.Message}");
            return 2;
        }

        try
        {
            run.Execute();
            return run.Report();
        }
        catch (FailFast fail)
        {
            return fail.Code;
        }
        catch (InvalidOperationException missing) when (LiteSpecHome.Root is null)
        {
            Console.Error.WriteLine(missing.Message);
            return 2;
        }
        catch (Exception exception)
        {
            return LastResort.Report(exception, parsed.Json, Console.Out, Console.Error);
        }
    }
}
