using System.Text.Json;
using System.Text.Json.Nodes;
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
          litespec model check --root <consumer> [--format json] [--release] [--since <ref>] [--all]
          litespec model format --root <consumer> [--check]
          litespec model render --root <consumer>
          litespec model query --root <consumer> <id>
          litespec model --help

        Exit codes: 0 no open blocking finding, 1 an open blocking finding, no record with the id, or a file that would change with format --check, 2 usage error or a record file that does not parse.
        """;

    /// <summary>Runs the model command: check, format, render or query.</summary>
    /// <param name="args">The command arguments, the subcommand first.</param>
    /// <returns>The exit code: 0 for success, 1 for a blocking finding or a missing record, 2 for a usage error.</returns>
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
                "check" => ["--release", "--all"],
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

        // Only check takes --format, so every other subcommand fails in text.
        var json = parsed.Json;

        // A usage error is reported before --help is honoured, so an unsupported --format value fails here too.
        var usageError = parsed.Error ?? parsed.FormatError;

        if (parsed.Help && usageError is null)
        {
            Console.WriteLine(Usage);
            return 0;
        }

        if (usageError is { } error)
        {
            return LastResort.UsageError(error, Usage, json);
        }

        var config = ConsumerRoot.Open(parsed.Value("--root"), json, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        var root = config.Root;

        switch (command)
        {
            case "check":
            {
                var model = ModelBuilder.Build(config);
                List<Finding> findings;
                IReadOnlyList<Finding> known;

                try
                {
                    (findings, known) = Collect(model, root);
                }
                catch (FailFast fail)
                {
                    return LastResort.Fail(fail.Message, json, fail.Code);
                }

                model.Ledger.Save(config);
                var release = parsed.Has("--release");
                var all = parsed.Has("--all");
                var sinceRef = parsed.Value("--since");
                StagedScope.Scope? since = null;
                IReadOnlyList<Finding> breaks = [];

                if (sinceRef is not null)
                {
                    var (scope, sinceError) = StagedScope.Since(model, sinceRef);

                    if (scope is null)
                    {
                        return LastResort.Fail(sinceError, json, 2);
                    }

                    since = scope;

                    try
                    {
                        using var checkout = DiffBase.Load(config, sinceRef);
                        breaks = ContractBreaks.Find(checkout.Model, model);
                    }
                    catch (InvalidOperationException exception)
                    {
                        return LastResort.Fail(exception.Message, json, 2);
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
                // always stands: the scope never demotes it to reported. It
                // runs only at the level its code claims, like every finding.
                foreach (var breaking in breaks.Where(finding => ConformanceLevels.Runs(finding.Code, config.Conformance)))
                {
                    shown.Add((breaking, Decide(model, config, breaking, release), false));
                }

                if (json)
                {
                    var payload = new JsonObject();

                    if (since is not null)
                    {
                        payload["scope"] = "since";
                        payload["since"] = sinceRef;
                        payload["changedFiles"] = since.ChangedFiles;
                        payload["changedRecords"] = since.RecordIds.Count;
                    }

                    payload["findings"] = new JsonArray([.. shown.Select(entry => (JsonNode?)AsJson(config, entry.Finding, entry.Severity, entry.Demoted))]);
                    payload["known"] = new JsonArray([.. known.Select(gap => (JsonNode?)AsKnownJson(config, gap))]);

                    CliJson.Write(payload);
                }
                else
                {
                    if (since is not null)
                    {
                        Console.WriteLine($"Since {sinceRef}: {since.ChangedFiles} files, {since.RecordIds.Count} records.");
                    }

                    // Each listed finding prints once. The open blocking findings come first, so the lines
                    // that fail the run lead the output however many advisory findings follow. By default
                    // the level A findings are summarized by code in the counts below and not listed one
                    // by one, so a passing check with many advisories stays readable; --all lists them.
                    var blocking = shown.Where(entry => entry.Severity.Outcome == Outcome.Blocking).ToList();
                    var advisoryCount = shown.Count(entry => entry.Finding.Tier == "A");
                    var listed = all
                        ? shown
                        : shown.Where(entry => entry.Finding.Tier != "A").ToList();

                    foreach (var entry in listed.Where(entry => entry.Severity.Outcome == Outcome.Blocking)
                                 .Concat(listed.Where(entry => entry.Severity.Outcome != Outcome.Blocking)))
                    {
                        Console.WriteLine(FindingLine(config, entry.Finding, entry.Demoted));
                    }

                    foreach (var gap in known)
                    {
                        Console.WriteLine($"{PathText.Relative(config.Root, gap.Path)}:{gap.Line} [known {gap.Tier}] [{gap.Code}] {gap.Message}");
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

                    if (!all && advisoryCount > 0)
                    {
                        Console.WriteLine($"{advisoryCount} advisory findings summarized by code; run with --all to list them.");
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
                    return LastResort.UsageError("Query needs an id.", Usage, json);
                }

                var model = ModelBuilder.Build(config);
                var answer = Querier.Query(model, id);

                if (answer is null)
                {
                    return LastResort.Fail($"No record holds '{id}'.", json, 1);
                }

                Console.WriteLine(answer);
                return 0;
            }

            default:
                return LastResort.UsageError($"Unknown command '{command}'.", Usage, json);
        }
    }

    /// <summary>
    ///     Collects a project's findings at its claimed conformance level: the model checks, then the consumer
    ///     pass, which reads the project file, the record pages, the glossary and the controlled UI into the same
    ///     finding shape. <c>model check</c> and the book both take their findings here, so they hold the same
    ///     level filter and the same severity rule.
    /// </summary>
    /// <param name="model">The built project model to check.</param>
    /// <param name="root">The absolute consumer root the consumer pass reads.</param>
    /// <returns>The standing findings, consumer findings included, and the known gaps.</returns>
    /// <exception cref="FailFast">The consumer pass stopped the run with its own exit code.</exception>
    internal static (List<Finding> Findings, IReadOnlyList<Finding> Known) Collect(ProjectModel model, string root)
    {
        var (modelFindings, known) = Checker.Check(model);
        var findings = modelFindings.ToList();

        var consumerRun = new ConsumerRun(root);
        consumerRun.Execute();
        findings.AddRange(consumerRun.Errors);

        // Consumer findings take the same level filter as the model findings, so a project
        // claims the same part of the contract in both passes.
        findings.RemoveAll(finding => !ConformanceLevels.Runs(finding.Code, model.Config.Conformance));

        return (findings, known);
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
    internal static Severity Decide(ProjectModel model, ConsumerConfig config, Finding finding, bool release)
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
        $"{PathText.Relative(config.Root, finding.Path)}:{finding.Line} [{LevelLabel(finding, demoted)}] [{finding.Code}] {finding.Message}";

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

    /// <summary>The JSON severity name of a finding tier, for annotation tools that read error, warning and note.</summary>
    /// <param name="finding">The finding whose tier is named.</param>
    /// <returns>error for level G, warning for level S and note for level A.</returns>
    static string SeverityName(Finding finding) => finding.Tier switch
    {
        "G" => "error",
        "S" => "warning",
        "A" => "note",
        _ => "error",
    };

    /// <summary>One standing finding as its JSON object, the shape every JSON finding list writes.</summary>
    /// <param name="config">The consumer configuration, for the relative path.</param>
    /// <param name="finding">The finding to project.</param>
    /// <param name="severity">The decided severity of the finding.</param>
    /// <param name="demoted">Whether <c>--since</c> demoted the finding as unchanged.</param>
    /// <returns>The JSON object for the finding.</returns>
    internal static JsonObject AsJson(ConsumerConfig config, Finding finding, Severity severity, bool demoted = false) => new()
    {
        ["code"] = finding.Code,
        ["level"] = finding.Tier,
        ["severity"] = SeverityName(finding),
        ["outcome"] = severity.Outcome == Outcome.Blocking ? "blocking" : "reported",
        ["reason"] = demoted ? "unchanged" : null,
        ["message"] = finding.Message,
        ["path"] = PathText.Relative(config.Root, finding.Path),
        ["line"] = finding.Line,
        ["subject"] = finding.Subject,
    };

    /// <summary>One known-gap finding as its JSON object, the shape every JSON known list writes.</summary>
    /// <param name="config">The consumer configuration, for the relative path.</param>
    /// <param name="finding">The known-gap finding to project.</param>
    /// <returns>The JSON object for the known-gap finding.</returns>
    internal static JsonObject AsKnownJson(ConsumerConfig config, Finding finding) => new()
    {
        ["code"] = finding.Code,
        ["level"] = finding.Tier,
        ["severity"] = SeverityName(finding),
        ["outcome"] = "known",
        ["reason"] = null,
        ["message"] = finding.Message,
        ["path"] = PathText.Relative(config.Root, finding.Path),
        ["line"] = finding.Line,
        ["subject"] = finding.Subject,
    };
}
