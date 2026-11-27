using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Consumer;
using LiteSpec.Cli.Rules;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Hook;

/// <summary>
///     The <c>litespec hook</c> command: the small surface the agent harness calls.
///     <c>post-edit</c> reads the Claude Code PostToolUse JSON on standard input and
///     prints the judgment provisions and the open findings for the edited file.
///     <c>pre-stop</c> runs the model check scoped to the files changed since HEAD
///     and prints the blocking findings.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
static partial class HookCommand
{
    /// <summary>The usage text for the hook command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage: litespec hook post-edit [--root <consumer>]
               litespec hook pre-stop [--root <consumer>]

        Post-edit reads the Claude Code PostToolUse JSON on standard input, takes the edited
        file from tool_input.file_path, and prints at most 40 lines: the judgment provisions
        rules returns for that file and the open findings for it. Pre-stop runs the model
        check scoped to the files changed since HEAD and prints the blocking findings.

          --root      The consumer repository to read. Defaults to the working directory.
          --help      Print this text and exit.

        Exit codes: 0 the hook printed its context, or pre-stop found no blocking finding,
        1 a file cannot be read, 2 the options are invalid or pre-stop found a blocking finding.
        """;

    /// <summary>The most lines post-edit prints, so the added context stays short.</summary>
    internal const int MaxLines = 40;

    /// <summary>Runs the hook command.</summary>
    /// <param name="args">The command arguments, the subcommand first.</param>
    /// <returns>The exit code: 0 printed, 1 an unreadable file, 2 a usage error or a blocking finding at pre-stop.</returns>
    public static int Run(string[] args) => Run(args, null);

    /// <summary>Runs the hook command with the given standard input.</summary>
    /// <param name="args">The command arguments, the subcommand first.</param>
    /// <param name="input">The standard input text for post-edit, or null to read the process input.</param>
    /// <returns>The exit code: 0 printed, 1 an unreadable file, 2 a usage error or a blocking finding at pre-stop.</returns>
    internal static int Run(string[] args, TextReader? input)
    {
        if (args.Length == 0 || args is ["--help"] or ["-h"] or ["help"])
        {
            CliHost.Out.WriteLine(Usage);
            return args.Length == 0 ? 2 : 0;
        }

        var command = args[0];

        if (command is not ("post-edit" or "pre-stop"))
        {
            return CommandFailure.UsageError($"Unknown hook '{command}'. Use post-edit or pre-stop.", Usage, json: false);
        }

        var parsed = CliArgs.Parse(args[1..], [], ["--root"], maxPositionals: 0);

        if (parsed.Help)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is { } error)
        {
            return CommandFailure.UsageError(error, Usage, json: false);
        }

        return command == "post-edit"
            ? PostEdit(parsed.Value("--root"), input ?? Console.In)
            : PreStop(parsed.Value("--root"));
    }

    /// <summary>Runs post-edit: the judgment provisions and the open findings for the edited file, at most 40 lines.</summary>
    /// <param name="rootOption">The --root value, or null when it was not passed.</param>
    /// <param name="input">The standard input holding the hook JSON.</param>
    /// <returns>The exit code: 0 printed or nothing applied, 1 when a file cannot be read.</returns>
    static int PostEdit(string? rootOption, TextReader input)
    {
        string payload;

        try
        {
            payload = input.ReadToEnd();
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            return CommandFailure.Fail($"Cannot read the hook input: {exception.Message}", json: false, code: 1);
        }

        if (EditedFile(payload) is not { Length: > 0 } edited)
        {
            return 0;
        }

        var config = ConsumerFor(rootOption, edited);

        if (config is null)
        {
            return 0;
        }

        string home;

        try
        {
            home = LiteSpecHome.RequireRoot();
        }
        catch (InvalidOperationException)
        {
            return 0;
        }

        JsonObject manifest;

        try
        {
            manifest = LiteSpecHome.ReadManifest(home);
        }
        catch (Exception exception) when (exception is IOException or JsonTextException or UnauthorizedAccessException)
        {
            return 0;
        }

        var backendStack = StackSelection.Stacks(manifest)
            .FirstOrDefault(stack => stack.Kind == "backend" && config.Stacks.Contains(stack.Id, StringComparer.Ordinal))
            ?.Id ?? "";
        var context = PathContext.Resolve(config, backendStack, edited);

        if (context is null)
        {
            return 0;
        }

        var catalog = ProvisionCatalog.Load(home, manifest, config.Shape, config.Stacks, config.Extensions);
        var conventions = ConventionFiles.Load(config);
        var waivers = WaiverFiles.Load(config, DateOnly.FromDateTime(DateTime.UtcNow));
        var index = ProvisionRecordIndex.Load(config);
        var matches = ProvisionRetriever.Retrieve(catalog, conventions, waivers, config, context, index);

        var lines = new List<string>();
        var judgment = matches
            .Where(match => !match.IsChecked && !match.IsConcernWide && !match.MatchesOnlyThroughMarkedRecord)
            .ToList();

        if (judgment.Count > 0)
        {
            lines.Add($"Judgment provisions for {context.Display}:");

            foreach (var match in judgment)
            {
                lines.Add($"- {match.Entry.Id}: {OneLine(match.Statement)}");
            }
        }

        foreach (var collapsed in CollapsedRecordLines(matches))
        {
            lines.Add(collapsed);
        }

        foreach (var finding in OpenFindingsFor(config, edited))
        {
            lines.Add(finding);
        }

        if (lines.Count == 0)
        {
            return 0;
        }

        // The hook output is added context beside the edit, so it stays short: the cap
        // counts every printed line, and the last line says what was withheld.
        if (lines.Count > MaxLines)
        {
            var withheld = lines.Count - (MaxLines - 1);
            lines = lines.Take(MaxLines - 1).ToList();
            lines.Add($"... and {withheld} more lines withheld (40-line cap).");
        }

        foreach (var line in lines)
        {
            CliHost.Out.WriteLine(line);
        }

        return 0;
    }

    /// <summary>Runs pre-stop: the blocking findings on the files changed since HEAD.</summary>
    /// <param name="rootOption">The --root value, or null when it was not passed.</param>
    /// <returns>The exit code: 0 with no blocking finding, 2 when a blocking finding stands.</returns>
    static int PreStop(string? rootOption)
    {
        var config = ConsumerRoot.Open(rootOption, json: false, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        var (files, error) = RulesSince.ChangedFiles(config.Root, "HEAD");

        if (files is null)
        {
            CliHost.Out.WriteLine($"Pre-stop check skipped: {error}");
            return 0;
        }

        var scoped = files
            .Where(file => IsUnder(config.Root, file))
            .ToList();

        if (scoped.Count == 0)
        {
            CliHost.Out.WriteLine("Pre-stop check: no changed files since HEAD.");
            return 0;
        }

        var model = ModelBuilder.Build(config);
        List<Finding> findings;
        IReadOnlyList<Finding> covered;

        try
        {
            (findings, covered) = ModelCommand.Collect(model, config.Root);
        }
        catch (FailFast fail)
        {
            CliHost.Out.WriteLine($"Pre-stop check failed: {fail.Message}");
            return 2;
        }

        var roots = scoped.Select(Path.GetFullPath).ToList();
        var coveredSet = covered.Select(CoveredKey).ToHashSet();
        var blocking = findings
            .Where(finding => ModelOptions.InOnlyPaths(model, config, finding, roots))
            .Where(finding => !coveredSet.Contains(CoveredKey(finding)))
            .Select(finding => (Finding: finding, Severity: ModelCommand.Decide(model, config, finding, release: false)))
            .Where(entry => entry.Severity.Outcome == Outcome.Blocking)
            .OrderBy(entry => PathText.Relative(config.Root, entry.Finding.Path), StringComparer.Ordinal)
            .ThenBy(entry => entry.Finding.Line)
            .ThenBy(entry => entry.Finding.Code, StringComparer.Ordinal)
            .ToList();

        if (blocking.Count == 0)
        {
            CliHost.Out.WriteLine("Pre-stop check: no blocking findings in the change set.");
            return 0;
        }

        foreach (var entry in blocking)
        {
            CliHost.Out.WriteLine(
                $"{PathText.Relative(config.Root, entry.Finding.Path)}:{entry.Finding.Line} [{entry.Finding.Tier}] [{entry.Finding.Code}] {entry.Finding.Message}");
        }

        CliHost.Out.WriteLine($"{blocking.Count} blocking {(blocking.Count == 1 ? "finding" : "findings")} in the change set.");
        return 2;
    }

    /// <summary>The edited file the hook JSON names, or null when it names none.</summary>
    /// <param name="payload">The hook JSON text.</param>
    /// <returns>The absolute or root-relative edited file, or null.</returns>
    internal static string? EditedFile(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(payload);
        }
        catch (JsonException)
        {
            return null;
        }

        using (document)
        {
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (root.TryGetProperty("tool_input", out var input) && input.ValueKind == JsonValueKind.Object)
            {
                foreach (var name in new[] { "file_path", "filePath", "path", "file_pathname" })
                {
                    if (input.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                        && value.GetString() is { Length: > 0 } named)
                    {
                        return named;
                    }
                }
            }

            foreach (var name in new[] { "file_path", "filePath", "path" })
            {
                if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                    && value.GetString() is { Length: > 0 } named)
                {
                    return named;
                }
            }

            return null;
        }
    }

    /// <summary>The consumer the edited file belongs to: the project found from the file's folder, else the project found from the start folder.</summary>
    /// <param name="rootOption">The --root value, or null when it was not passed.</param>
    /// <param name="edited">The edited file the hook JSON named.</param>
    /// <returns>The loaded declaration, or null when no consumer holds the file.</returns>
    static ConsumerConfig? ConsumerFor(string? rootOption, string edited)
    {
        if (rootOption is not null)
        {
            return ConsumerRoot.Open(rootOption, json: false, out _) is { } explicitConfig ? explicitConfig : null;
        }

        string? folder = null;

        try
        {
            var absolute = Path.IsPathFullyQualified(edited)
                ? Path.GetFullPath(edited)
                : Path.GetFullPath(Path.Combine(CliHost.WorkingDirectory, edited));
            folder = Path.GetDirectoryName(absolute);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            folder = null;
        }

        if (folder is not null && ConsumerRoot.Open(folder, json: false, out _) is { } near)
        {
            return near;
        }

        return ConsumerRoot.Open(null, json: false, out _);
    }

    /// <summary>One collapsed line per marked record holding provisions about the edited file.</summary>
    /// <param name="matches">The retrieved provisions.</param>
    /// <returns>The collapsed lines in record order.</returns>
    static List<string> CollapsedRecordLines(IReadOnlyList<ProvisionMatch> matches)
    {
        var records = matches
            .Where(match => match.MatchesOnlyThroughMarkedRecord)
            .SelectMany(match => match.RecordIds)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        var lines = new List<string>();

        foreach (var record in records)
        {
            var about = matches.Count(match => match.RecordIds.Contains(record, StringComparer.Ordinal));
            lines.Add($"{record}: {about} provisions about the record; model check checks them; run rules {record} for the rest");
        }

        return lines;
    }

    /// <summary>The open findings on the edited file: each standing finding on the file or on its subject record, blocking first.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="edited">The edited file the hook JSON named.</param>
    /// <returns>The finding lines for the file.</returns>
    static List<string> OpenFindingsFor(ConsumerConfig config, string edited)
    {
        string absolute;

        try
        {
            absolute = Path.IsPathFullyQualified(edited)
                ? Path.GetFullPath(edited)
                : Path.GetFullPath(Path.Combine(config.Root, edited.Replace('/', Path.DirectorySeparatorChar)));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return [];
        }

        var model = ModelBuilder.Build(config);
        List<Finding> findings;
        IReadOnlyList<Finding> covered;

        try
        {
            (findings, covered) = ModelCommand.Collect(model, config.Root);
        }
        catch (FailFast)
        {
            return [];
        }

        var coveredSet = covered.Select(CoveredKey).ToHashSet();
        var lines = findings
            .Where(finding => !coveredSet.Contains(CoveredKey(finding)) && FindingApplies(model, finding, absolute))
            .Select(finding => (Finding: finding, Severity: ModelCommand.Decide(model, config, finding, release: false)))
            .OrderByDescending(entry => entry.Severity.Outcome == Outcome.Blocking)
            .ThenBy(entry => PathText.Relative(config.Root, entry.Finding.Path), StringComparer.Ordinal)
            .ThenBy(entry => entry.Finding.Line)
            .ThenBy(entry => entry.Finding.Code, StringComparer.Ordinal)
            .Select(entry => $"{PathText.Relative(config.Root, entry.Finding.Path)}:{entry.Finding.Line} [{entry.Finding.Tier}] [{entry.Finding.Code}] {entry.Finding.Message}")
            .ToList();

        if (lines.Count == 0)
        {
            return [];
        }

        var headed = new List<string> { "Open findings for the edited file:" };
        headed.AddRange(lines);
        return headed;
    }

    /// <summary>Whether the finding belongs to the edited file: its file is the edited file, or its subject record's file is.</summary>
    /// <param name="model">The built project model resolving the finding subject.</param>
    /// <param name="finding">The finding to test.</param>
    /// <param name="absolute">The absolute edited file.</param>
    /// <returns>True when the finding sits on the edited file or its record.</returns>
    static bool FindingApplies(ProjectModel model, Finding finding, string absolute)
    {
        if (string.Equals(Path.GetFullPath(finding.Path), absolute, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return finding.Subject is not null
            && model.ById.TryGetValue(Identifiers.BaseId(finding.Subject), out var record)
            && record.File.Length > 0
            && string.Equals(Path.GetFullPath(record.File), absolute, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Whether the folder holds the path.</summary>
    /// <param name="folder">The absolute folder.</param>
    /// <param name="path">The absolute path.</param>
    /// <returns>True when the folder holds the path.</returns>
    static bool IsUnder(string folder, string path)
    {
        try
        {
            var full = Path.GetFullPath(path);
            var basePath = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return full.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)
                || string.Equals(full.TrimEnd(Path.DirectorySeparatorChar), basePath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    /// <summary>The identity that pairs a standing finding with its covered entry: code, path, line and message.</summary>
    /// <param name="finding">The finding whose identity is wanted.</param>
    /// <returns>The code, path, line and message of the finding.</returns>
    static (string Code, string Path, int Line, string Message) CoveredKey(Finding finding) =>
        (finding.Code, finding.Path, finding.Line, finding.Message);

    /// <summary>One statement as one line: inner newlines read as spaces.</summary>
    /// <param name="statement">The provision statement.</param>
    /// <returns>The statement with inner newlines replaced by spaces.</returns>
    static string OneLine(string statement) =>
        SingleSpace().Replace(statement, " ").Trim();

    /// <summary>A run of whitespace the one-line form collapses.</summary>
    /// <returns>The whitespace expression.</returns>
    [GeneratedRegex(@"\s+")]
    private static partial Regex SingleSpace();
}
