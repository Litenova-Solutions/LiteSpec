using System.Text;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Generate;

/// <summary>
///     The <c>litespec generate</c> command: writes the code the records fully determine,
///     from the <c>generate</c> targets of the consumer declaration.
/// </summary>
/// <follows>litespec/core/model#generate-record-determined-code-from-the-records</follows>
static class GenerateCommand
{
    /// <summary>The usage text for the generate command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage: litespec generate [--root <consumer>] [--check] [--help]

        Writes the code the records fully determine: the refusal codes, permission ids,
        domain event decisions and closed value sets the generate targets name. With
        --check it writes nothing and exits 1 when any file would change.

          --root      The consumer repository to read. Defaults to the working directory.
          --check     Write nothing; list each file that would change and exit 1 when any would.
          --help      Print this text and exit.

        Exit codes: 0 the files are written, or --check found every file current, 1 a file would change with --check, the records cannot be read, or a file cannot be written, 2 the options are invalid or the project file is missing.
        """;

    /// <summary>Runs the generate command.</summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code: 0 written or current, 1 drift with --check or an unreadable record, 2 usage error or unreadable consumer.</returns>
    public static int Run(string[] args)
    {
        if (args is ["--help"] or ["-h"] or ["help"])
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        var parsed = CliArgs.Parse(
            args,
            ["--check"],
            ["--root"],
            maxPositionals: 0);

        if (parsed.Help)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is { } error)
        {
            return CommandFailure.UsageError(error, Usage, json: false);
        }

        var config = ConsumerRoot.Open(parsed.Value("--root"), json: false, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        var model = ModelBuilder.Build(config);

        // A generated file must never be quietly short: when a record file
        // does not parse, generate writes nothing and names each invalid file instead.
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
                CliHost.Error.WriteLine($"{file}: does not parse.");
            }

            CliHost.Out.WriteLine("0 files rewritten.");
            return 2;
        }

        var plan = GeneratePlan.Build(model);

        if (plan.Errors.Count > 0)
        {
            foreach (var targetError in plan.Errors)
            {
                CliHost.Error.WriteLine(targetError);
            }

            return 1;
        }

        var changed = plan.Files
            .Where(file => !File.Exists(file.AbsolutePath) || File.ReadAllText(file.AbsolutePath) != file.Content)
            .ToList();
        var check = parsed.Has("--check");

        if (!check)
        {
            foreach (var file in changed)
            {
                try
                {
                    if (Path.GetDirectoryName(file.AbsolutePath) is { Length: > 0 } folder)
                    {
                        Directory.CreateDirectory(folder);
                    }

                    File.WriteAllText(file.AbsolutePath, file.Content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
                {
                    return CommandFailure.Fail($"Cannot write '{PathText.Relative(config.Root, file.AbsolutePath)}': {exception.Message}", json: false, code: 1);
                }
            }
        }

        foreach (var file in changed)
        {
            CliHost.Out.WriteLine(PathText.Relative(config.Root, file.AbsolutePath));
        }

        CliHost.Out.WriteLine(check ? $"{changed.Count} files would change." : $"{changed.Count} files rewritten.");
        return check && changed.Count > 0 ? 1 : 0;
    }

    /// <summary>Whether the finding reports a file the reader could not read, rather than a record the file holds.</summary>
    /// <param name="finding">The record-invalid finding.</param>
    /// <returns>True when the file does not parse.</returns>
    private static bool IsParseFailure(Finding finding) =>
        finding.Message.Contains("could not be read", StringComparison.Ordinal)
        || finding.Message.StartsWith("The glossary is not YAML", StringComparison.Ordinal);
}
