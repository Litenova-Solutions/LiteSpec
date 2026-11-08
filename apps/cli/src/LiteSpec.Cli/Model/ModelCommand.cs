using System.Text.Json;
using LiteSpec.Consumer;
using LiteSpec.Model.Diff;

namespace LiteSpec.Model;

/// <summary>
///     The <c>litespec model</c> command: check, format, render and query a consumer's project model.
/// </summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
static class ModelCommand
{
    /// <summary>The usage text for the model command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage:
          litespec model check --root <consumer> [--format json] [--release] [--since <ref>]
          litespec model format --root <consumer> [--check]
          litespec model render --root <consumer>
          litespec model query --root <consumer> <id>
          litespec model --help

        Exit codes: 0 no open blocking finding, 1 an open blocking finding, no record with the id, or a file that would change with format --check, 2 usage error or a record file that does not parse.
        """;

    /// <summary>Runs the model command: check, format, render or query.</summary>
    /// <param name="args">The command arguments, the subcommand first.</param>
    /// <returns>The exit code: 0 for success, 1 for a blocking finding or a missing record, 2 for a usage error.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Run is the last-resort command handler: every failure becomes a Failed message with exit code 1, so no exception type is excluded.")]
    public static int Run(string[] args)
    {
        if (args.Length == 0 || args is ["--help"] or ["-h"] or ["help"])
        {
            Console.WriteLine(Usage);
            return args.Length == 0 ? 2 : 0;
        }

        var command = args[0];
        var parsed = CliArgs.Parse(
            args[1..],
            command switch
            {
                "check" => ["--release"],
                "format" => ["--check"],
                _ => [],
            },
            command switch
            {
                "check" => ["--root", "--format", "--since"],
                _ => ["--root"],
            },
            maxPositionals: command switch
            {
                "query" => 1,
                _ => 0,
            });

        if (parsed.Help)
        {
            Console.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is { } error)
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(Usage);
            return 2;
        }

        string root;

        try
        {
            root = ConsumerRoot.Resolve(parsed.Value("--root"));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Console.Error.WriteLine($"Invalid root '{parsed.Value("--root")}': {exception.Message}");
            return 2;
        }

        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine($"Root '{root}' does not exist.");
            return 2;
        }

        // Without a project file every path falls back to a default, and the
        // check would pass a folder that is not a consumer at all.
        if (!File.Exists(ConsumerRoot.ProjectFile(root)))
        {
            Console.Error.WriteLine($"Root '{root}' holds no .litespec/project.json.");
            return 2;
        }

        ConsumerConfig config;

        try
        {
            config = ConsumerConfig.Load(root);
        }
        catch (JsonException exception)
        {
            Console.Error.WriteLine($".litespec/project.json is not valid JSON: {exception.Message}");
            return 2;
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }

        try
        {
            switch (command)
            {
                case "check":
                {
                    var model = ModelBuilder.Build(config);
                    var (modelFindings, known) = Checker.Check(model);
                    var findings = modelFindings.ToList();

                    // The consumer pass reads the project file, the record pages,
                    // the glossary and the controlled UI into the same finding
                    // shape, so one command and one CI step hold the whole consumer.
                    try
                    {
                        var consumerRun = new ConsumerRun(root);
                        consumerRun.Execute();
                        findings.AddRange(consumerRun.Errors);
                    }
                    catch (FailFast fail)
                    {
                        Console.Error.WriteLine(fail.Message);
                        return fail.Code;
                    }

                    model.Ledger.Save(config);
                    var json = parsed.Json;
                    var release = parsed.Has("--release");
                    var sinceRef = parsed.Value("--since");
                    StagedScope.Scope? since = null;
                    IReadOnlyList<Finding> breaks = [];

                    if (sinceRef is not null)
                    {
                        var (scope, sinceError) = StagedScope.Since(model, sinceRef);

                        if (scope is null)
                        {
                            Console.Error.WriteLine(sinceError);
                            return 2;
                        }

                        since = scope;

                        try
                        {
                            using var checkout = DiffBase.Load(config, sinceRef);
                            breaks = ContractBreaks.Find(checkout.Model, model);
                        }
                        catch (InvalidOperationException exception)
                        {
                            Console.Error.WriteLine(exception.Message);
                            return 2;
                        }
                    }

                    var shown = findings
                        .Select(finding => (Finding: finding, Severity: Decide(model, config, finding, release)))
                        .Select(entry => since is not null
                            && entry.Severity.Outcome == Outcome.Blocking
                            && !StagedScope.InScope(since, entry.Finding)
                                ? (entry.Finding, Severity: new Severity(Outcome.Reported), Demoted: true)
                                : (entry.Finding, entry.Severity, Demoted: false))
                        .ToList();

                    // A breaking contract change is the point of --since, so it
                    // always stands: the scope never demotes it to reported.
                    foreach (var breaking in breaks)
                    {
                        shown.Add((breaking, Decide(model, config, breaking, release), false));
                    }

                    if (json)
                    {
                        var payload = new Dictionary<string, object?>();

                        if (since is not null)
                        {
                            payload["scope"] = "since";
                            payload["since"] = sinceRef;
                            payload["changedFiles"] = since.ChangedFiles;
                            payload["changedRecords"] = since.RecordIds.Count;
                        }

                        payload["findings"] = shown.Select(entry => AsJson(config, entry.Finding, entry.Severity, entry.Demoted)).ToList();
                        payload["known"] = known.Select(gap => AsKnownJson(config, gap)).ToList();

                        Console.WriteLine(JsonSerializer.Serialize(payload, CliJson.Options));
                    }
                    else
                    {
                        if (since is not null)
                        {
                            Console.WriteLine($"Since {sinceRef}: {since.ChangedFiles} files, {since.RecordIds.Count} records.");
                        }

                        // Each finding prints once. The open blocking findings come first, so the lines
                        // that fail the run lead the output however many advisory findings follow.
                        var blocking = shown.Where(entry => entry.Severity.Outcome == Outcome.Blocking).ToList();

                        foreach (var entry in blocking.Concat(shown.Where(entry => entry.Severity.Outcome != Outcome.Blocking)))
                        {
                            Console.WriteLine(FindingLine(config, entry.Finding, entry.Demoted));
                        }

                        foreach (var gap in known)
                        {
                            Console.WriteLine($"{config.Relative(gap.Path)}:{gap.Line} [known {gap.Tier}] [{gap.Code}] {gap.Message}");
                        }

                        if (shown.Count > 0)
                        {
                            Console.WriteLine();
                            Console.WriteLine("Findings by code:");

                            foreach (var group in shown
                                .GroupBy(entry => (entry.Finding.Code, entry.Finding.Tier))
                                .Select(group => (group.Key.Code, group.Key.Tier, Count: group.Count()))
                                .OrderBy(group => LevelRank(group.Tier))
                                .ThenByDescending(group => group.Count)
                                .ThenBy(group => group.Code, StringComparer.Ordinal))
                            {
                                Console.WriteLine($"  {group.Count,6}  {group.Code} (level {group.Tier})");
                            }
                        }

                        Console.WriteLine($"{shown.Count} findings, {blocking.Count} blocking, {known.Count} known.");
                    }

                    return shown.Any(entry => entry.Severity.Outcome == Outcome.Blocking) ? 1 : 0;
                }

                case "format":
                {
                    var check = parsed.Has("--check");
                    var (changed, invalid) = RecordFormatter.Format(config, check);

                    foreach (var file in invalid)
                    {
                        Console.Error.WriteLine($"{file}: does not parse.");
                    }

                    foreach (var file in changed)
                    {
                        Console.WriteLine(file);
                    }

                    if (check)
                    {
                        Console.WriteLine($"{changed.Count} files would change.");
                    }
                    else
                    {
                        Console.WriteLine($"{changed.Count} files rewritten.");
                    }

                    if (invalid.Count > 0)
                    {
                        return 2;
                    }

                    return check && changed.Count > 0 ? 1 : 0;
                }

                case "render":
                {
                    var model = ModelBuilder.Build(config);

                    // Render writes every generated output: the fenced regions in
                    // explanation files and documents, and the records file each
                    // web frontend imports from its frame.
                    var changed = Renderer.Render(model).Concat(FrontendRecords.Write(model)).ToList();

                    foreach (var file in changed)
                    {
                        Console.WriteLine(file);
                    }

                    Console.WriteLine($"{changed.Count} files rewritten.");
                    return 0;
                }

                case "query":
                {
                    if (parsed.Positionals is not [var id])
                    {
                        Console.Error.WriteLine("Query needs an id.");
                        return 2;
                    }

                    var model = ModelBuilder.Build(config);
                    var answer = Querier.Query(model, id);

                    if (answer is null)
                    {
                        Console.Error.WriteLine($"No record holds '{id}'.");
                        return 1;
                    }

                    Console.WriteLine(answer);
                    return 0;
                }

                default:
                    Console.Error.WriteLine($"Unknown command '{command}'.");
                    return 2;
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Failed: {exception.Message}");
            return 1;
        }
    }

    /// <summary>
    ///     Decides one finding's severity from its tier and the qualification
    ///     of the reader that produced it: TypeScript findings take the
    ///     handshake qualification, Roslyn findings stay qualified, and a finding
    ///     with no reader is qualified. At release the release-readiness findings
    ///     block instead of reporting.
    /// </summary>
    /// <param name="model">The built project model the finding belongs to.</param>
    /// <param name="config">The consumer configuration of the project.</param>
    /// <param name="finding">The finding to decide.</param>
    /// <param name="release">Whether release readiness makes reported findings block.</param>
    /// <returns>The severity of the finding under the release flag.</returns>
    static Severity Decide(ProjectModel model, ConsumerConfig config, Finding finding, bool release)
    {
        var severity = Severity.Of(
            finding,
            (finding.Code == "reader-missing" && config.IsContinuousIntegration) || finding.Reader switch
            {
                Finding.TypeScriptReader => model.TypeScriptQualified,
                Finding.RoslynReader => true,
                _ => true,
            },
            config.IsContinuousIntegration);

        return release && severity.Outcome == Outcome.Reported && IsReleaseReadiness(finding)
            ? new Severity(Outcome.Blocking)
            : severity;
    }

    /// <summary>Whether the finding gates a release: an unverified use case.</summary>
    /// <param name="finding">The finding to test.</param>
    /// <returns>True when the finding gates a release.</returns>
    static bool IsReleaseReadiness(Finding finding) =>
        finding.Code == "use-case-unverified";

    /// <summary>One finding's printed line: its path, line, level, code and message.</summary>
    /// <param name="config">The consumer configuration, for the relative path.</param>
    /// <param name="finding">The finding to print.</param>
    /// <param name="demoted">Whether <c>--since</c> demoted the finding as unchanged.</param>
    /// <returns>The printed line for the finding.</returns>
    static string FindingLine(ConsumerConfig config, Finding finding, bool demoted) =>
        $"{config.Relative(finding.Path)}:{finding.Line} [{LevelLabel(finding, demoted)}] [{finding.Code}] {finding.Message}";

    /// <summary>The sort rank of a level: G first, then S, then A.</summary>
    /// <param name="level">The level letter: G, S or A.</param>
    /// <returns>The sort rank, 0 for G, 1 for S, 2 for A and 3 for anything else.</returns>
    static int LevelRank(string level) => level switch
    {
        "G" => 0,
        "S" => 1,
        "A" => 2,
        _ => 3,
    };

    /// <summary>The printed level: the level, plus unchanged when <c>--since</c> demoted the finding.</summary>
    /// <param name="finding">The finding whose level is printed.</param>
    /// <param name="demoted">Whether <c>--since</c> demoted the finding as unchanged.</param>
    /// <returns>The level, with unchanged appended when the finding is demoted.</returns>
    static string LevelLabel(Finding finding, bool demoted) =>
        demoted ? finding.Tier + " unchanged" : finding.Tier;

    /// <summary>One standing finding as its JSON object.</summary>
    /// <param name="config">The consumer configuration, for the relative path.</param>
    /// <param name="finding">The finding to project.</param>
    /// <param name="severity">The decided severity of the finding.</param>
    /// <param name="demoted">Whether <c>--since</c> demoted the finding as unchanged.</param>
    /// <returns>The JSON object for the finding.</returns>
    static object AsJson(ConsumerConfig config, Finding finding, Severity severity, bool demoted = false) => new
    {
        code = finding.Code,
        level = finding.Tier,
        outcome = severity.Outcome == Outcome.Blocking ? "blocking" : "reported",
        reason = demoted ? "unchanged" : null,
        message = finding.Message,
        path = config.Relative(finding.Path),
        line = finding.Line,
        subject = finding.Subject,
    };

    /// <summary>One known-gap finding as its JSON object.</summary>
    /// <param name="config">The consumer configuration, for the relative path.</param>
    /// <param name="finding">The known-gap finding to project.</param>
    /// <returns>The JSON object for the known-gap finding.</returns>
    static object AsKnownJson(ConsumerConfig config, Finding finding) => new
    {
        code = finding.Code,
        level = finding.Tier,
        outcome = "known",
        reason = (string?)null,
        message = finding.Message,
        path = config.Relative(finding.Path),
        line = finding.Line,
        subject = finding.Subject,
    };
}
