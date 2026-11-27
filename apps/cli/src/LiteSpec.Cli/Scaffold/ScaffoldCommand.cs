using System.Text;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Scaffold;

/// <summary>
///     The <c>litespec scaffold</c> command: writes the normal, single-file, non-partial C#
///     files of one use case from its record, into the folders the .NET pack states.
/// </summary>
/// <follows>litespec/core/backend-application#organize-application-by-use-case</follows>
public static class ScaffoldCommand
{
    /// <summary>The usage text for the scaffold command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage: litespec scaffold <use-case-id> [--root <consumer>] [--diff] [--help]

        Writes the code files of one use case from its record: the command or query
        message, its handler, a validator for a command with inputs, the result, and one
        definition file each for authorization, audit, tenancy and, for a command,
        idempotency, plus the endpoint with its request and response models when a web
        entry point reaches the use case. An existing file is skipped and listed. With
        --diff it writes nothing and prints each file it would create with its full content.

          <use-case-id>  The use case to scaffold, with or without the use-case/ prefix.
          --root      The consumer repository to read. Defaults to the working directory.
          --diff      Write nothing; print each file it would create with its full content.
          --help      Print this text and exit.

        Exit codes: 0 the files are written, 1 the use case cannot scaffold or a file cannot be written, 2 the options are invalid, the project file is missing, or a record file does not parse.
        """;

    /// <summary>Runs the scaffold command.</summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code: 0 written, 1 unscaffoldable or unwritable, 2 usage error or unreadable consumer.</returns>
    public static int Run(string[] args) => Run(CliHost.Out, CliHost.Error, args);

    /// <summary>Runs the scaffold command against explicit writers, so tests capture the output.</summary>
    /// <param name="stdout">The writer receiving the command output.</param>
    /// <param name="stderr">The writer receiving the command errors.</param>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code: 0 written, 1 unscaffoldable or unwritable, 2 usage error or unreadable consumer.</returns>
    internal static int Run(TextWriter stdout, TextWriter stderr, string[] args)
    {
        if (args is ["--help"] or ["-h"] or ["help"])
        {
            stdout.WriteLine(Usage);
            return 0;
        }

        var parsed = CliArgs.Parse(
            args,
            ["--diff"],
            ["--root"],
            maxPositionals: 1);

        if (parsed.Help)
        {
            stdout.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is { } error)
        {
            return Fail(stderr, stdout, error, code: 2);
        }

        if (parsed.Positionals.Count == 0)
        {
            return Fail(stderr, stdout, "Missing use case id.", code: 2);
        }

        var config = ConsumerRoot.Open(parsed.Value("--root"), json: false, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        var model = ModelBuilder.Build(config);

        // A scaffolded file must never be quietly short: when a record file
        // does not parse, scaffold writes nothing and names each invalid file instead.
        var invalid = model.Problems
            .Where(finding => finding.Code == "record-invalid" && IsParseFailure(finding))
            .Select(finding => PathText.Relative(config.Root, finding.Path))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        if (invalid.Count > 0)
        {
            foreach (var file in invalid)
            {
                stderr.WriteLine($"{file}: does not parse.");
            }

            stdout.WriteLine("0 files written.");
            return 2;
        }

        if (!ScaffoldPlan.TryBuild(model, parsed.Positionals[0], out var plan, out var planError) || plan is null)
        {
            stderr.WriteLine(planError ?? $"Use case '{parsed.Positionals[0]}' not found.");
            return 1;
        }

        var diff = parsed.Has("--diff");
        var missing = plan.Files
            .Where(file => !File.Exists(Path.Combine(config.Root, file.RelativePath)))
            .ToList();

        if (plan.ExpectsLine is not null)
        {
            stdout.WriteLine(plan.ExpectsLine);
        }

        if (diff)
        {
            foreach (var file in missing)
            {
                stdout.WriteLine($"--- {Display(config.Root, file.RelativePath)}");
                stdout.Write(file.Content);
            }

            return 0;
        }

        var written = 0;
        var skipped = 0;

        foreach (var file in plan.Files)
        {
            var absolute = Path.Combine(config.Root, file.RelativePath);

            if (File.Exists(absolute))
            {
                stdout.WriteLine($"Skipped (exists) {Display(config.Root, file.RelativePath)}");
                skipped++;
                continue;
            }

            try
            {
                if (Path.GetDirectoryName(absolute) is { Length: > 0 } folder)
                {
                    Directory.CreateDirectory(folder);
                }

                File.WriteAllText(absolute, file.Content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                stderr.WriteLine($"Cannot write '{Display(config.Root, file.RelativePath)}': {exception.Message}");
                return 1;
            }

            stdout.WriteLine($"Created {Display(config.Root, file.RelativePath)}");
            written++;
        }

        stdout.WriteLine($"{written} files written, {skipped} files skipped.");
        return 0;
    }

    /// <summary>Whether the finding reports a file the reader could not read, rather than a record the file holds.</summary>
    /// <param name="finding">The record-invalid finding.</param>
    /// <returns>True when the file does not parse.</returns>
    private static bool IsParseFailure(Finding finding) =>
        finding.Message.Contains("could not be read", StringComparison.Ordinal)
        || finding.Message.StartsWith("The glossary is not YAML", StringComparison.Ordinal);

    /// <summary>The display path of a scaffold file, relative to the consumer root.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="relative">The file path relative to the consumer root.</param>
    /// <returns>The relative display path.</returns>
    private static string Display(string root, string relative) =>
        PathText.Relative(root, Path.Combine(root, relative));

    /// <summary>Writes a usage error: the message and the usage text, and returns the exit code.</summary>
    /// <param name="stderr">The writer receiving the command errors.</param>
    /// <param name="stdout">The writer receiving the command output.</param>
    /// <param name="message">The usage error message.</param>
    /// <param name="code">The exit code the command returns.</param>
    /// <returns>The exit code.</returns>
    private static int Fail(TextWriter stderr, TextWriter stdout, string message, int code)
    {
        stderr.WriteLine(message);
        stdout.WriteLine(Usage);
        return code;
    }
}
