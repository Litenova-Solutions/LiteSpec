using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Consumer;
using LiteSpec.Cli.Model.Diff;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Model.Screens;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model;

/// <summary>
///     The <c>litespec model</c> command: check, review, format, render, query and edges a consumer's project model.
/// </summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
static class ModelCommand
{
    /// <summary>The usage text for the model command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage:
          litespec model check --root <consumer> [--format json] [--release] [--since <ref>] [--module <module>] [--only-paths <paths...>] [--record-proof] [--verbose]
          litespec model review --root <consumer> [--format json]
          litespec model format --root <consumer> [--check]
          litespec model render --root <consumer>
          litespec model query --root <consumer> <id>
          litespec model query --root <consumer> --search <text>
          litespec model query --root <consumer> --list <type> [--module <module>]
          litespec model edges --root <consumer> [--module <module>]
          litespec model --help

        Exit codes: 0 no open blocking finding, 1 an open blocking finding, no record with the id, or a file that would change with format --check, 2 usage error or a record file that does not parse, 3 no open blocking finding but the inspection is incomplete.
        """;

    /// <summary>Runs the model command: check, review, format, render, query or edges.</summary>
    /// <param name="args">The command arguments, the subcommand first.</param>
    /// <returns>The exit code: 0 for success, 1 for a blocking finding or a missing record, 2 for a usage error, 3 for success with an incomplete inspection.</returns>
    public static int Run(string[] args)
    {
        if (args.Length == 0 || args is ["--help"] or ["-h"] or ["help"])
        {
            CliHost.Out.WriteLine(Usage);
            return args.Length == 0 ? 2 : 0;
        }

        var command = args[0];

        // --only-paths takes any number of paths, which the option parser reads as one
        // value, so the check arguments split them out before parsing.
        var onlyPaths = (Paths: (IReadOnlyList<string>)[], Error: (string?)null);
        var checkArgs = args[1..];

        if (command == "check")
        {
            var split = ModelOptions.SplitOnlyPaths(args[1..]);
            onlyPaths = (split.Paths, split.Error);
            checkArgs = split.Remaining;
        }

        var parsed = CliArgs.Parse(
            checkArgs,
            command switch
            {
                "check" => ["--release", "--record-proof", "--verbose"],
                "format" => ["--check"],
                _ => [],
            },
            command switch
            {
                "check" => ["--root", "--format", "--since", "--module"],
                "review" => ["--root", "--format"],
                "query" => ["--root", "--search", "--list", "--module"],
                "edges" => ["--root", "--module"],
                _ => ["--root"],
            },
            maxPositionals: command switch
            {
                "query" => 1,
                _ => 0,
            });

        // Only check and review take --format, so every other subcommand fails in text.
        var json = parsed.Json;

        // A usage error is reported before --help is honoured, so an unsupported --format value fails here too.
        var usageError = onlyPaths.Error ?? parsed.Error ?? parsed.FormatError;

        if (parsed.Help && usageError is null)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (usageError is { } error)
        {
            return CommandFailure.UsageError(error, Usage, json);
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
                // A module check reads only that module's backend projects, so a sibling
                // edit that left another module unparsable does not fail the run.
                var model = ModelBuilder.Build(config, parsed.Value("--module") is { Length: > 0 } readModule ? readModule : null);
                List<Finding> findings;
                IReadOnlyList<Finding> covered;

                try
                {
                    (findings, covered) = Collect(model, root, parsed.Has("--verbose"));
                }
                catch (FailFast fail)
                {
                    return CommandFailure.Fail(fail.Message, json, fail.Code);
                }

                // Level R findings are prose quality for a person: model review reports them, never the check.
                findings.RemoveAll(finding => finding.Tier == "R");
                covered = covered.Where(finding => finding.Tier != "R").ToList();

                var release = parsed.Has("--release");
                var recordProof = parsed.Has("--record-proof");
                var sinceRef = parsed.Value("--since");
                HashSet<(string Code, string Subject, string Message)>? baseline = null;
                string? baseError = null;
                var newFindings = 0;
                IReadOnlyList<Finding> breaks = [];
                IReadOnlyList<Finding> proofDrift = [];

                if (sinceRef is not null)
                {
                    if (sinceRef.Length == 0)
                    {
                        return CommandFailure.UsageError("Option --since needs a value.", Usage, json);
                    }

                    // The base is checked like the working tree, so every finding the change
                    // introduced is one the base lacks, however far its subject sits from the
                    // changed files. When the base cannot be built or checked, every finding
                    // blocks and the output says why.
                    try
                    {
                        using var checkout = DiffBase.Load(config, sinceRef);
                        var (baseStanding, _) = Collect(checkout.Model, checkout.Model.Config.Root);
                        baseStanding.RemoveAll(finding => finding.Tier == "R");
                        baseline = baseStanding.Select(SinceKey).ToHashSet();
                        newFindings = findings.Count(finding => !baseline.Contains(SinceKey(finding)));
                        breaks = ContractBreaks.Find(checkout.Model, model);
                        proofDrift = ProofChanges.Find(checkout.Model, model);
                    }
                    catch (InvalidOperationException exception)
                    {
                        baseError = exception.Message;
                    }
                    catch (FailFast fail)
                    {
                        baseError = fail.Message;
                    }
                }

                // A finding the base already holds is existing debt: it reports with the
                // unchanged mark instead of blocking. Without a base every finding blocks.
                var shown = findings
                    .Select(finding => (Finding: finding, Severity: Decide(model, config, finding, release)))
                    .Select(entry => baseline is not null
                        && entry.Severity.Outcome == Outcome.Blocking
                        && baseline.Contains(SinceKey(entry.Finding))
                            ? (entry.Finding, Severity: new Severity(Outcome.Reported), Demoted: true)
                            : (entry.Finding, entry.Severity, Demoted: false))
                    .ToList();

                // A breaking contract change is the point of --since, so it
                // always stands: the base comparison never demotes it to reported. It
                // runs only when the layer claims cover its code, like every finding.
                foreach (var breaking in breaks.Where(finding => config.Layers.Covers(finding.Code, config.Extensions)))
                {
                    shown.Add((breaking, Decide(model, config, breaking, release), false));
                }

                // A proving test changed without its scenario blocks: the scenario must change with its proof.
                foreach (var drift in proofDrift.Where(finding => config.Layers.Covers(finding.Code, config.Extensions)))
                {
                    shown.Add((drift, Decide(model, config, drift, release), false));
                }

                // A module check prints only the findings on that module's records and files,
                // and the exit code below follows those findings alone.
                if (parsed.Value("--module") is { } onlyModule)
                {
                    if (onlyModule.Length == 0)
                    {
                        return CommandFailure.UsageError("Option --module needs a value.", Usage, json);
                    }

                    var known = KnownModules(model);

                    if (!known.Contains(onlyModule))
                    {
                        return CommandFailure.UsageError(UnknownModule(onlyModule, known), Usage, json);
                    }

                    shown = shown.Where(entry => InModule(model, config, entry.Finding, onlyModule)).ToList();
                    covered = covered.Where(finding => InModule(model, config, finding, onlyModule)).ToList();
                }

                // A scoped check prints only the findings on the named paths and their
                // records, and the exit code below follows those findings alone.
                if (onlyPaths.Paths.Count > 0)
                {
                    var roots = onlyPaths.Paths.Select(path => ModelOptions.Rooted(config, path)).ToList();
                    shown = shown.Where(entry => ModelOptions.InOnlyPaths(model, config, entry.Finding, roots)).ToList();
                    covered = covered.Where(finding => ModelOptions.InOnlyPaths(model, config, finding, roots)).ToList();
                }

                // A release drift makes every other finding suspect: the installed tool reads the project
                // against a release the project never reviewed. The drift finding prints first, so no one
                // starts fixing findings that may not apply.
                shown = shown
                    .OrderByDescending(entry => entry.Finding.Code == "release-drift")
                    .ToList();

                var incomplete = IncompleteLine(MissingReaders(config, shown), NotBlocking(shown));

                if (json)
                {
                    var payload = new JsonObject();

                    if (sinceRef is not null)
                    {
                        payload["scope"] = "since";
                        payload["since"] = sinceRef;

                        if (baseError is not null)
                        {
                            payload["baseError"] = baseError;
                        }
                        else
                        {
                            payload["newFindings"] = newFindings;
                        }
                    }

                    payload["findings"] = new JsonArray([.. shown.Select(entry => (JsonNode?)AsJson(config, entry.Finding, entry.Severity, entry.Demoted))]);
                    payload["covered"] = new JsonArray([.. covered.Select(finding => (JsonNode?)AsCoveredJson(config, finding))]);
                    payload["backendReader"] = model.BackendReaderMode;

                    if (model.SemanticUnavailableReason is { Length: > 0 } unavailable)
                    {
                        payload["backendReaderNote"] = unavailable;
                    }

                    CliHost.Out.WriteLine(CliJson.Serialize(payload));
                }
                else
                {
                    if (sinceRef is not null)
                    {
                        CliHost.Out.WriteLine(baseError is not null
                            ? $"Since {sinceRef}: {Stopped(baseError)} Every finding blocks."
                            : $"Since {sinceRef}: {newFindings} new findings.");
                    }

                    // Each finding prints once. The open blocking findings come first, so the lines
                    // that fail the run lead the output however many reported findings follow.
                    var blocking = shown.Where(entry => entry.Severity.Outcome == Outcome.Blocking).ToList();

                    foreach (var entry in shown.Where(entry => entry.Severity.Outcome == Outcome.Blocking)
                                 .Concat(shown.Where(entry => entry.Severity.Outcome != Outcome.Blocking)))
                    {
                        CliHost.Out.WriteLine(FindingLine(config, entry.Finding, entry.Demoted));
                    }

                    foreach (var finding in covered)
                    {
                        CliHost.Out.WriteLine($"{PathText.Relative(config.Root, finding.Path)}:{finding.Line} [covered {finding.Tier}] [{finding.Code}] {finding.Message}");
                    }

                    if (shown.Count > 0)
                    {
                        CliHost.Out.WriteLine();
                        CliHost.Out.WriteLine("Findings by code:");

                        foreach (var group in shown
                            .GroupBy(entry => (entry.Finding.Code, entry.Finding.Tier))
                            .Select(group => (group.Key.Code, group.Key.Tier, Count: group.Count()))
                            .OrderBy(group => LevelRank(group.Tier))
                            .ThenByDescending(group => group.Count)
                            .ThenBy(group => group.Code, StringComparer.Ordinal))
                        {
                            CliHost.Out.WriteLine($"  {group.Count,6}  {group.Code} (level {group.Tier})");
                        }
                    }

                    CliHost.Out.WriteLine(model.BackendSemantics is { } semantics
                        ? $"Backend reader: {model.BackendReaderMode} ({semantics.ReferenceSummary})."
                        : model.SemanticUnavailableReason is { Length: > 0 } reason
                            ? $"Backend reader: {model.BackendReaderMode} (semantic unavailable: {reason})."
                            : $"Backend reader: {model.BackendReaderMode}.");
                    CliHost.Out.WriteLine($"{shown.Count} findings, {blocking.Count} blocking, {covered.Count} covered.");

                    if (incomplete is not null)
                    {
                        CliHost.Out.WriteLine(incomplete);
                    }
                }

                // The check writes the proof ledger only on --record-proof, after the
                // findings are printed: a plain check reads the project and writes nothing.
                if (recordProof)
                {
                    model.Ledger.Save(config);
                }

                if (shown.Any(entry => entry.Severity.Outcome == Outcome.Blocking))
                {
                    return 1;
                }

                return incomplete is not null ? 3 : 0;
            }

            case "review":
            {
                var model = ModelBuilder.Build(config);
                List<Finding> findings;
                IReadOnlyList<Finding> covered;

                try
                {
                    (findings, covered) = Collect(model, root);
                }
                catch (FailFast fail)
                {
                    return CommandFailure.Fail(fail.Message, json, fail.Code);
                }

                // Review reports the level R findings the check sets aside: prose quality only a person can weigh.
                var shown = findings
                    .Where(finding => finding.Tier == "R")
                    .Select(finding => (Finding: finding, Severity: Decide(model, config, finding, release: false)))
                    .ToList();
                covered = covered.Where(finding => finding.Tier == "R").ToList();

                if (json)
                {
                    var payload = new JsonObject
                    {
                        ["findings"] = new JsonArray([.. shown.Select(entry => (JsonNode?)AsJson(config, entry.Finding, entry.Severity))]),
                        ["covered"] = new JsonArray([.. covered.Select(finding => (JsonNode?)AsCoveredJson(config, finding))]),
                    };

                    CliHost.Out.WriteLine(CliJson.Serialize(payload));
                }
                else
                {
                    foreach (var entry in shown)
                    {
                        CliHost.Out.WriteLine(FindingLine(config, entry.Finding, false));
                    }

                    foreach (var finding in covered)
                    {
                        CliHost.Out.WriteLine($"{PathText.Relative(config.Root, finding.Path)}:{finding.Line} [covered {finding.Tier}] [{finding.Code}] {finding.Message}");
                    }

                    if (shown.Count > 0)
                    {
                        CliHost.Out.WriteLine();
                        CliHost.Out.WriteLine("Findings by code:");

                        foreach (var group in shown
                            .GroupBy(entry => (entry.Finding.Code, entry.Finding.Tier))
                            .Select(group => (group.Key.Code, group.Key.Tier, Count: group.Count()))
                            .OrderBy(group => LevelRank(group.Tier))
                            .ThenByDescending(group => group.Count)
                            .ThenBy(group => group.Code, StringComparer.Ordinal))
                        {
                            CliHost.Out.WriteLine($"  {group.Count,6}  {group.Code} (level {group.Tier})");
                        }
                    }

                    CliHost.Out.WriteLine($"{shown.Count} findings, 0 blocking, {covered.Count} covered.");
                }

                // Review never blocks: its findings are prose quality for a person, not defects.
                return 0;
            }

            case "format":
            {
                var check = parsed.Has("--check");
                var (changed, invalid) = RecordFormatter.Format(config, check);

                foreach (var file in invalid)
                {
                    CliHost.Error.WriteLine($"{file}: does not parse.");
                }

                foreach (var file in changed)
                {
                    CliHost.Out.WriteLine(file);
                }

                if (check)
                {
                    CliHost.Out.WriteLine($"{changed.Count} files would change.");
                }
                else
                {
                    CliHost.Out.WriteLine($"{changed.Count} files rewritten.");
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

                // A rendered identifier list must never be quietly short: when a record file
                // does not parse, render writes nothing and names each invalid file instead.
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

                // Render writes every generated output: the fenced regions in
                // explanation files and documents, the agent-protocol region in
                // AGENTS.md, the marked project-scope skill copies, the records
                // file each web frontend imports from its frame, and the UI source
                // lock beside the shared package.
                var changed = GeneratedRegions.Render(model)
                    .Concat(AgentProtocol.Write(model))
                    .Concat(AgentProtocol.WriteSkills(model))
                    .Concat(FrontendRecords.Write(model))
                    .Concat(UiSourceLock.Write(model))
                    .ToList();

                foreach (var file in changed)
                {
                    CliHost.Out.WriteLine(file);
                }

                CliHost.Out.WriteLine($"{changed.Count} files rewritten.");
                return 0;
            }

            case "query":
            {
                var search = parsed.Value("--search");
                var list = parsed.Value("--list");
                var queryModule = parsed.Value("--module");
                var modes = parsed.Positionals.Count + (search is null ? 0 : 1) + (list is null ? 0 : 1);

                if (modes == 0)
                {
                    return CommandFailure.UsageError("Query needs an id, --search <text> or --list <type>.", Usage, json);
                }

                if (modes > 1)
                {
                    return CommandFailure.UsageError("Query takes one of <id>, --search <text> or --list <type>.", Usage, json);
                }

                if (search is not null && search.Length == 0)
                {
                    return CommandFailure.UsageError("Option --search needs a value.", Usage, json);
                }

                if (list is not null && list.Length == 0)
                {
                    return CommandFailure.UsageError("Option --list needs a value.", Usage, json);
                }

                if (queryModule is not null && list is null)
                {
                    return CommandFailure.UsageError("Option --module needs --list.", Usage, json);
                }

                if (queryModule is not null && queryModule.Length == 0)
                {
                    return CommandFailure.UsageError("Option --module needs a value.", Usage, json);
                }

                var model = ModelBuilder.Build(config);

                if (search is not null)
                {
                    foreach (var record in RecordQuery.Search(model, search))
                    {
                        CliHost.Out.WriteLine(RecordQuery.Line(model, record));
                    }

                    return 0;
                }

                if (list is not null)
                {
                    if (!RecordTypes.All.Contains(list))
                    {
                        return CommandFailure.UsageError(
                            $"Unknown record type '{list}'. Use one of: {string.Join(", ", RecordTypes.All.OrderBy(name => name, StringComparer.Ordinal))}.",
                            Usage,
                            json);
                    }

                    if (queryModule is not null)
                    {
                        var known = KnownModules(model);

                        if (!known.Contains(queryModule))
                        {
                            return CommandFailure.UsageError(UnknownModule(queryModule, known), Usage, json);
                        }
                    }

                    foreach (var record in RecordQuery.List(model, list, queryModule))
                    {
                        CliHost.Out.WriteLine(RecordQuery.Line(model, record));
                    }

                    return 0;
                }

                var id = parsed.Positionals[0];
                var answer = RecordQuery.Query(model, id);

                if (answer is null)
                {
                    return CommandFailure.Fail($"No record holds '{id}'.", json, 1);
                }

                CliHost.Out.WriteLine(answer);
                return 0;
            }

            case "edges":
            {
                var edgeModule = parsed.Value("--module");

                if (edgeModule is not null && edgeModule.Length == 0)
                {
                    return CommandFailure.UsageError("Option --module needs a value.", Usage, json);
                }

                var model = ModelBuilder.Build(config);

                if (edgeModule is not null)
                {
                    var known = KnownModules(model);

                    if (!known.Contains(edgeModule))
                    {
                        return CommandFailure.UsageError(UnknownModule(edgeModule, known), Usage, json);
                    }
                }

                foreach (var line in ModelEdges.Lines(model, edgeModule))
                {
                    CliHost.Out.WriteLine(line);
                }

                return 0;
            }

            default:
                return CommandFailure.UsageError($"Unknown command '{command}'.", Usage, json);
        }
    }

    /// <summary>
    ///     Collects a project's findings that its layer claims cover: the model checks, then the consumer
    ///     pass, which reads the project file, the record pages, the glossary and the controlled UI into the same
    ///     finding shape. <c>model check</c>, <c>model review</c> and the book all take their findings here, so they hold the same
    ///     claim filter and the same severity rule.
    /// </summary>
    /// <param name="model">The built project model to check.</param>
    /// <param name="root">The absolute consumer root the consumer pass reads.</param>
    /// <param name="verbose">Whether schema errors list every rejected shape instead of the selected one.</param>
    /// <returns>The standing findings, consumer findings included, and the covered findings.</returns>
    /// <exception cref="FailFast">The consumer pass stopped the run with its own exit code.</exception>
    internal static (List<Finding> Findings, IReadOnlyList<Finding> Covered) Collect(ProjectModel model, string root, bool verbose = false)
    {
        // The consumer pass reads the project from disk and not the model, so it runs beside the checks.
        var consumerRun = new ConsumerRun(root);
        consumerRun.Verbose = verbose;
        var consumerPass = Task.Run(consumerRun.Execute);

        var (modelFindings, covered) = Checker.Check(model);
        var findings = modelFindings.ToList();

        consumerPass.GetAwaiter().GetResult();
        findings.AddRange(consumerRun.Errors);

        // Consumer findings take the same claim filter as the model findings, so a project
        // claims the same part of the contract in both passes.
        findings.RemoveAll(finding => !model.Config.Layers.Covers(finding.Code, model.Config.Extensions));

        return (findings, covered);
    }

    /// <summary>Every module the model holds: each declared module and each module folder holding records.</summary>
    /// <param name="model">The built project model holding the module records.</param>
    /// <returns>The declared and folder module names.</returns>
    static HashSet<string> KnownModules(ProjectModel model)
    {
        var known = new HashSet<string>(StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            if (record is { Type: RecordTypes.Module, Derived: false }
                && Identifiers.TrySplit(record.Id, out _, out _, out var name)
                && name.Length > 0)
            {
                known.Add(name);
            }

            if (!record.Derived && record.Module is { Length: > 0 } folder)
            {
                known.Add(folder);
            }
        }

        return known;
    }

    /// <summary>The usage error for a module name the project declares no module for.</summary>
    /// <param name="module">The unknown module name.</param>
    /// <param name="known">The declared module names.</param>
    /// <returns>The usage error message.</returns>
    static string UnknownModule(string module, HashSet<string> known) =>
        known.Count == 0
            ? $"Unknown module '{module}': this project declares no modules."
            : $"Unknown module '{module}'. Use one of: {string.Join(", ", known.OrderBy(name => name, StringComparer.Ordinal))}.";

    /// <summary>Whether the finding reports a file that does not parse, rather than one that parses but holds no valid record.</summary>
    /// <param name="finding">The finding to test.</param>
    /// <returns>True when the finding reports a record, document, or glossary file that does not parse.</returns>
    static bool IsParseFailure(Finding finding) =>
        finding.Message.Contains("could not be read", StringComparison.Ordinal)
        || finding.Message.StartsWith("The glossary is not YAML", StringComparison.Ordinal);

    /// <summary>Whether the finding belongs to the module: its subject record's module, else its record file's module folder, else its code file's module folder.</summary>
    /// <param name="model">The built project model resolving the finding subject.</param>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <param name="finding">The finding to test.</param>
    /// <param name="module">The module name to keep.</param>
    /// <returns>True when the finding sits on the module's records or files.</returns>
    static bool InModule(ProjectModel model, ConsumerConfig config, Finding finding, string module)
    {
        if (finding.Subject is not null
            && model.ById.TryGetValue(Identifiers.BaseId(finding.Subject), out var record)
            && record.Module is not null)
        {
            return module.Equals(record.Module, StringComparison.Ordinal);
        }

        if (RecordPlacement.ModuleOf(finding.Path, config) is { } recordModule)
        {
            return module.Equals(recordModule, StringComparison.Ordinal);
        }

        // A code finding carries no record subject: the module layout names its module folder.
        if (SourceRoots.ModulePath(config, finding.Path) is { } segments && segments.Length > 0)
        {
            return SameModuleName(segments[0], module);
        }

        return false;
    }

    /// <summary>Whether a code module folder and a record module name spell one module: case, hyphens and underscores ignored.</summary>
    /// <param name="folder">The module folder name as written on disk.</param>
    /// <param name="name">The module name the check keeps.</param>
    /// <returns>True when both names agree with case, hyphens and underscores ignored; otherwise false.</returns>
    static bool SameModuleName(string folder, string name) =>
        ModelOptions.ModuleMatches(folder, name);

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
            });

        return release && severity.Outcome != Outcome.Covered && IsReleaseReadiness(finding)
            ? new Severity(Outcome.Blocking)
            : severity;
    }

    /// <summary>
    ///     Whether the finding gates a release: a reader missing for a claimed layer. A
    ///     waiver with an expiry parks it before it reaches this decision, so a covered finding never gates.
    /// </summary>
    /// <param name="finding">The finding to test.</param>
    /// <returns>True when the finding gates a release.</returns>
    static bool IsReleaseReadiness(Finding finding) =>
        finding.Code is "reader-missing";

    /// <summary>The identity a <c>--since</c> run matches a finding on: its code, subject and message, never its line.</summary>
    /// <param name="finding">The finding whose identity is wanted.</param>
    /// <returns>The code, subject and message the base comparison matches.</returns>
    internal static (string Code, string Subject, string Message) SinceKey(Finding finding) =>
        (finding.Code, finding.Subject ?? "", finding.Message);

    /// <summary>The frontends the run could not read: each distinct frontend a <c>reader-missing</c> finding names by path, in name order.</summary>
    /// <param name="config">The consumer configuration holding the declared frontends.</param>
    /// <param name="shown">The findings the run shows, with their decided severities.</param>
    /// <returns>The distinct unread frontend names in name order.</returns>
    static IReadOnlyList<string> MissingReaders(ConsumerConfig config, IReadOnlyList<(Finding Finding, Severity Severity, bool Demoted)> shown)
    {
        var missing = shown
            .Select(entry => entry.Finding)
            .Where(finding => finding.Code == "reader-missing")
            .Select(finding => finding.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return [.. config.Frontends
            .Where(frontend => missing.Contains(frontend.Path))
            .Select(frontend => frontend.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)];
    }

    /// <summary>How many shown level S findings report instead of blocking, setting the <c>--since</c> unchanged findings aside.</summary>
    /// <param name="shown">The findings the run shows, with their decided severities.</param>
    /// <returns>The level S reported findings the missing readers held back.</returns>
    static int NotBlocking(IReadOnlyList<(Finding Finding, Severity Severity, bool Demoted)> shown) =>
        shown.Count(entry => entry.Finding.Tier == "S"
            && entry.Severity.Outcome == Outcome.Reported
            && !entry.Demoted);

    /// <summary>The last line a check prints when a frontend reader was missing, or null when every reader ran.</summary>
    /// <param name="frontends">The unread frontend names in name order.</param>
    /// <param name="waiting">How many level S findings report instead of blocking.</param>
    /// <returns>The incomplete line with one clause per frontend, or null when no frontend is unread.</returns>
    internal static string? IncompleteLine(IReadOnlyList<string> frontends, int waiting) =>
        frontends.Count == 0
            ? null
            : "inspection incomplete: " + string.Join("; ", frontends.Select(name => $"reader for {name} missing, {waiting} findings not blocking"));

    /// <summary>The reason as one trimmed sentence ending with a stop.</summary>
    /// <param name="reason">The base failure reason.</param>
    /// <returns>The trimmed reason ending with a stop.</returns>
    static string Stopped(string reason)
    {
        var trimmed = reason.Trim();

        if (trimmed.Length == 0)
        {
            return "The base could not be checked.";
        }

        return trimmed.EndsWith('.') || trimmed.EndsWith('!') || trimmed.EndsWith('?') ? trimmed : trimmed + ".";
    }

    /// <summary>One finding's printed line: its path, line, level, code and message.</summary>
    /// <param name="config">The consumer configuration, for the relative path.</param>
    /// <param name="finding">The finding to print.</param>
    /// <param name="demoted">Whether <c>--since</c> demoted the finding as unchanged.</param>
    /// <returns>The printed line for the finding.</returns>
    static string FindingLine(ConsumerConfig config, Finding finding, bool demoted) =>
        $"{PathText.Relative(config.Root, finding.Path)}:{finding.Line} [{LevelLabel(finding, demoted)}] [{finding.Code}] {finding.Message}";

    /// <summary>The sort rank of a level: G first, then S, then R.</summary>
    /// <param name="level">The level letter: G, S or R.</param>
    /// <returns>The sort rank, 0 for G, 1 for S, 2 for R and 3 for anything else.</returns>
    static int LevelRank(string level) => level switch
    {
        "G" => 0,
        "S" => 1,
        "R" => 2,
        _ => 3,
    };

    /// <summary>The printed level: the level, plus unchanged when <c>--since</c> demoted the finding.</summary>
    /// <param name="finding">The finding whose level is printed.</param>
    /// <param name="demoted">Whether <c>--since</c> demoted the finding as unchanged.</param>
    /// <returns>The level, with unchanged appended when the finding is demoted.</returns>
    static string LevelLabel(Finding finding, bool demoted) =>
        demoted ? finding.Tier + " unchanged" : finding.Tier;

    /// <summary>The JSON name of a finding outcome: blocking, reported or covered.</summary>
    /// <param name="outcome">The outcome to name.</param>
    /// <returns>The lower-case outcome name.</returns>
    static string OutcomeName(Outcome outcome) => outcome.ToString().ToLowerInvariant();

    /// <summary>One standing finding as its JSON object, the shape every JSON finding list writes.</summary>
    /// <param name="config">The consumer configuration, for the relative path.</param>
    /// <param name="finding">The finding to project.</param>
    /// <param name="severity">The decided outcome of the finding.</param>
    /// <param name="demoted">Whether <c>--since</c> demoted the finding as unchanged.</param>
    /// <returns>The JSON object for the finding.</returns>
    internal static JsonObject AsJson(ConsumerConfig config, Finding finding, Severity severity, bool demoted = false) => new()
    {
        ["code"] = finding.Code,
        ["level"] = finding.Tier,
        ["outcome"] = OutcomeName(severity.Outcome),
        ["reason"] = demoted ? "unchanged" : null,
        ["message"] = finding.Message,
        ["path"] = PathText.Relative(config.Root, finding.Path),
        ["line"] = finding.Line,
        ["subject"] = finding.Subject,
    };

    /// <summary>One covered finding as its JSON object, the shape every JSON covered list writes.</summary>
    /// <param name="config">The consumer configuration, for the relative path.</param>
    /// <param name="finding">The covered finding to project.</param>
    /// <returns>The JSON object for the covered finding.</returns>
    internal static JsonObject AsCoveredJson(ConsumerConfig config, Finding finding) => new()
    {
        ["code"] = finding.Code,
        ["level"] = finding.Tier,
        ["outcome"] = OutcomeName(Outcome.Covered),
        ["reason"] = null,
        ["message"] = finding.Message,
        ["path"] = PathText.Relative(config.Root, finding.Path),
        ["line"] = finding.Line,
        ["subject"] = finding.Subject,
    };
}
