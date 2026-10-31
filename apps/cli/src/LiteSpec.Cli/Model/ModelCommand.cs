using System.Text.Json;
using LiteSpec.Model.Diff;

namespace LiteSpec.Model;

/// <summary>
///     The <c>litespec model</c> command: build, check, render and query a consumer's project model.
/// </summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
static class ModelCommand
{
    /// <summary>The usage text for the model command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage:
          litespec model build --root <consumer> [--out model.json]
          litespec model check --root <consumer> [--format json] [--staged] [--release] [--since <ref>] [--level <levels>]
          litespec model render --root <consumer>
          litespec model query --root <consumer> <id>
          litespec model names [--root <consumer>] [--format text|json]
          litespec model review --root <consumer> [--decider <owner>] [--tier 1|3] [--module <module>] [--type <type>] [--format json]
          litespec model approve --root <consumer> (<id>... | --all) [--by <owner>] [--on <YYYY-MM-DD>] [--dry-run]
          litespec model approve --root <consumer> --batch <selector> [--batch <selector>...] [--batches <file>] --by <owner> [--second-reviewer <owner>|agent] [--review-report <file>] [--on <YYYY-MM-DD>] [--dry-run]
          litespec model diff <base> --root <consumer> [--format text|markdown|json] [--intent|--behavior] [--fail-on breaking] [--breaking-only]
          litespec model --help

        Exit codes: 0 no open blocking finding, 1 an open blocking finding, no record with the id, a refused batch, or a breaking intent change with --fail-on breaking, 2 usage error.
        """;

    /// <summary>Runs the model command: build, check, render, query, names, review, approve or diff.</summary>
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
                "diff" => ["--breaking-only", "--intent", "--behavior"],
                "approve" => ["--all", "--dry-run"],
                "check" => ["--staged", "--release"],
                _ => [],
            },
            command switch
            {
                "diff" => ["--root", "--out", "--format", "--fail-on"],
                "approve" => ["--root", "--by", "--on", "--batch", "--batches", "--second-reviewer", "--review-report"],
                "review" => ["--root", "--out", "--format", "--decider", "--tier", "--module", "--type"],
                "check" => ["--root", "--out", "--format", "--since", "--level"],
                _ => ["--root", "--out", "--format"],
            },
            maxPositionals: command switch
            {
                "query" or "diff" => 1,
                "approve" => int.MaxValue,
                _ => 0,
            });

        if (parsed.Help)
        {
            Console.WriteLine(Usage);
            return 0;
        }

        var formatError = command == "diff" ? DiffFormatError(parsed.Value("--format")) : parsed.FormatError;

        if ((parsed.Error ?? formatError) is { } error)
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(Usage);
            return 2;
        }

        string root;

        try
        {
            root = parsed.Root();
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
        if (!File.Exists(Path.Combine(root, "litespec.project.json")))
        {
            Console.Error.WriteLine($"Root '{root}' holds no litespec.project.json.");
            return 2;
        }

        ConsumerConfig config;

        try
        {
            config = ConsumerConfig.Load(root);
        }
        catch (JsonException exception)
        {
            Console.Error.WriteLine($"litespec.project.json is not valid JSON: {exception.Message}");
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
                case "build":
                {
                    var model = ModelBuilder.Build(config);

                    // The build writes the check result into the model, so the book
                    // needs only model.json: one check run, on the model just built,
                    // with the same severity decision and JSON shape as check --format json.
                    var (checkFindings, checkKnown) = Checker.Check(model);
                    model.FindingsJson = checkFindings
                        .Select(finding => (Finding: finding, Severity: Decide(model, config, finding, false)))
                        .Where(entry => entry.Severity.Outcome != Outcome.Silent)
                        .Select(entry => AsJson(config, entry.Finding, entry.Severity))
                        .Cast<object>()
                        .ToList();
                    model.KnownJson = checkKnown
                        .Where(gap => Decide(model, config, gap, false).Outcome != Outcome.Silent)
                        .Select(gap => AsKnownJson(config, gap))
                        .Cast<object>()
                        .ToList();
                    var output = parsed.Value("--out") ?? Path.Combine(root, ".model", "model.json");

                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
                    File.WriteAllText(output, model.ToJson() + "\n");
                    model.Ledger.Save(config);

                    foreach (var file in FrontendRecords.Write(model))
                    {
                        Console.WriteLine(file);
                    }

                    Console.WriteLine($"{Relative(root, output)}: {model.Records.Count} records, {model.Code.Handlers.Count} handlers, {model.Screens.Count} screens, {model.Tests.Count} scenarios.");
                    return 0;
                }

                case "check":
                {
                    var (levelFilter, levelError) = ParseLevels(parsed.Value("--level"));

                    if (levelError is not null)
                    {
                        Console.Error.WriteLine(levelError);
                        Console.Error.WriteLine(Usage);
                        return 2;
                    }

                    var model = ModelBuilder.Build(config);
                    var (findings, known) = Checker.Check(model);

                    model.Ledger.Save(config);
                    var json = parsed.Json;
                    var release = parsed.Has("--release");
                    var sinceRef = parsed.Value("--since");
                    StagedScope.Scope? staged = null;
                    StagedScope.Scope? since = null;

                    if (parsed.Has("--staged"))
                    {
                        var (scope, stagedError) = StagedScope.For(model);

                        if (scope is null)
                        {
                            Console.Error.WriteLine(stagedError);
                            return 2;
                        }

                        staged = scope;
                        findings = findings.Where(finding => StagedScope.InScope(scope, finding)).ToList();
                        known = known.Where(gap => StagedScope.InScope(scope, gap)).ToList();
                    }

                    if (sinceRef is not null)
                    {
                        var (scope, sinceError) = StagedScope.Since(model, sinceRef);

                        if (scope is null)
                        {
                            Console.Error.WriteLine(sinceError);
                            return 2;
                        }

                        since = scope;
                    }

                    var shown = findings
                        .Select(finding => (Finding: finding, Severity: Decide(model, config, finding, release)))
                        .Select(entry => since is not null
                            && entry.Severity.Outcome == Outcome.Blocking
                            && !StagedScope.InScope(since, entry.Finding)
                                ? (entry.Finding, Severity: new Severity(Outcome.Reported, "unchanged"))
                                : entry)
                        .Where(entry => entry.Severity.Outcome != Outcome.Silent)
                        .ToList();
                    var shownKnown = known
                        .Where(gap => Decide(model, config, gap, release).Outcome != Outcome.Silent)
                        .ToList();
                    var displayed = levelFilter is null
                        ? shown
                        : shown.Where(entry => levelFilter.Contains(entry.Finding.Tier)).ToList();

                    if (json)
                    {
                        var payload = new Dictionary<string, object?>();

                        if (staged is not null && since is not null)
                        {
                            payload["scope"] = "staged+since";
                        }
                        else if (staged is not null)
                        {
                            payload["scope"] = "staged";
                        }
                        else if (since is not null)
                        {
                            payload["scope"] = "since";
                        }

                        if (staged is not null)
                        {
                            payload["stagedFiles"] = staged.ChangedFiles;
                            payload["stagedRecords"] = staged.RecordIds.Count;
                        }

                        if (since is not null)
                        {
                            payload["since"] = sinceRef;
                            payload["changedFiles"] = since.ChangedFiles;
                            payload["changedRecords"] = since.RecordIds.Count;
                        }

                        payload["findings"] = displayed.Select(entry => AsJson(config, entry.Finding, entry.Severity)).ToList();
                        payload["known"] = shownKnown.Select(gap => AsKnownJson(config, gap)).ToList();

                        Console.WriteLine(JsonSerializer.Serialize(payload, CliJson.Options));
                    }
                    else
                    {
                        if (staged is not null)
                        {
                            Console.WriteLine($"Staged scope: {staged.ChangedFiles} files, {staged.RecordIds.Count} records.");
                        }

                        if (since is not null)
                        {
                            Console.WriteLine($"Since {sinceRef}: {since.ChangedFiles} files, {since.RecordIds.Count} records.");
                        }

                        // Each finding prints once. The open blocking findings come first, so the lines
                        // that fail the run lead the output however many advisory findings follow.
                        var blocking = displayed.Where(entry => entry.Severity.Outcome == Outcome.Blocking).ToList();

                        foreach (var entry in blocking.Concat(displayed.Where(entry => entry.Severity.Outcome != Outcome.Blocking)))
                        {
                            Console.WriteLine(FindingLine(config, entry.Finding, entry.Severity));
                        }

                        foreach (var gap in shownKnown)
                        {
                            Console.WriteLine($"{config.Relative(gap.Path)}:{gap.Line} [known {gap.Tier}] [{gap.Code}] {gap.Message}");
                        }

                        if (displayed.Count > 0)
                        {
                            Console.WriteLine();
                            Console.WriteLine("Findings by code:");

                            foreach (var group in displayed
                                .GroupBy(entry => (entry.Finding.Code, entry.Finding.Tier))
                                .Select(group => (group.Key.Code, group.Key.Tier, Count: group.Count()))
                                .OrderBy(group => LevelRank(group.Tier))
                                .ThenByDescending(group => group.Count)
                                .ThenBy(group => group.Code, StringComparer.Ordinal))
                            {
                                Console.WriteLine($"  {group.Count,6}  {group.Code} (level {group.Tier})");
                            }
                        }

                        Console.WriteLine($"{displayed.Count} findings, {blocking.Count} blocking, {shownKnown.Count} known.");
                    }

                    return shown.Any(entry => entry.Severity.Outcome == Outcome.Blocking) ? 1 : 0;
                }

                case "render":
                {
                    var model = ModelBuilder.Build(config);
                    var changed = Renderer.Render(model);

                    foreach (var file in changed)
                    {
                        Console.WriteLine(file);
                    }

                    Console.WriteLine($"{changed.Count} files rewritten.");
                    return 0;
                }

                case "review":
                {
                    int? tier = null;

                    if (parsed.Value("--tier") is { } tierText)
                    {
                        if (tierText is not ("1" or "3"))
                        {
                            Console.Error.WriteLine($"Unknown tier '{tierText}'; use 1 or 3.");
                            return 2;
                        }

                        tier = tierText == "1" ? 1 : 3;
                    }

                    var model = ModelBuilder.Build(config);
                    var batches = ReviewQueue.Filter(
                        ReviewQueue.Compute(model),
                        parsed.Value("--decider"),
                        tier,
                        parsed.Value("--module"),
                        parsed.Value("--type"));

                    model.Ledger.Save(config);
                    Console.WriteLine(parsed.Json ? ReviewQueue.Json(batches) : ReviewQueue.Human(batches));
                    return 0;
                }

                case "approve":
                {
                    var model = ModelBuilder.Build(config);
                    var all = parsed.Has("--all");
                    var ids = parsed.Positionals;

                    var batchRequests = BatchRequests(args[1..], root, out var batchError);

                    if (batchError is not null)
                    {
                        Console.Error.WriteLine(batchError);
                        return 2;
                    }

                    if (batchRequests.Count == 0 && parsed.Value("--batches") is not null)
                    {
                        Console.Error.WriteLine("The batches file names no batch.");
                        return 2;
                    }

                    if (batchRequests.Count > 0)
                    {
                        if (batchRequests.Count == 1
                            && parsed.Value("--batches") is null
                            && batchRequests[0].ReviewReport is null)
                        {
                            return ApproveBatch(config, model, parsed, batchRequests[0].Selector, all, ids, root);
                        }

                        return ApproveBatches(model, parsed, batchRequests, all, ids, root);
                    }

                    if (parsed.Value("--second-reviewer") is not null
                        || parsed.Value("--review-report") is not null)
                    {
                        Console.Error.WriteLine("Only approve --batch reads --second-reviewer and --review-report.");
                        return 2;
                    }

                    if (!all && ids.Count == 0)
                    {
                        Console.Error.WriteLine("Approve needs record ids, or --all for every approved or reviewed record with no approvedHash.");
                        return 2;
                    }

                    var written = model.Records.Where(record => !record.Derived).ToList();
                    var targets = all
                        ? written.Where(record => record.Status is RecordStatuses.Approved or RecordStatuses.Reviewed && string.IsNullOrEmpty(record.ApprovedHash)).ToList()
                        : written.Where(record => ids.Contains(record.Id)).ToList();
                    var unknown = all ? [] : ids.Where(id => written.All(record => record.Id != id)).ToList();

                    if (parsed.Has("--dry-run"))
                    {
                        var dry = Approver.DryRun(targets);

                        foreach (var (record, reason) in dry)
                        {
                            var action = reason == "no approvedHash" ? "stamped" : "reopened";

                            Console.WriteLine($"{config.Relative(record.File)}: {record.Id} would be {action} ({reason})");
                        }

                        var stampedCount = dry.Count(entry => entry.Reason == "no approvedHash");

                        Console.WriteLine($"{Counted(stampedCount, "record")} would be stamped, {dry.Count - stampedCount} would be reopened.");

                        foreach (var id in unknown)
                        {
                            Console.Error.WriteLine($"No written record holds '{id}'.");
                        }

                        return unknown.Count > 0 ? 1 : 0;
                    }

                    var on = parsed.Value("--on") ?? DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    var (stamped, missing) = Approver.Approve(model, targets, parsed.Value("--by"), on);

                    foreach (var record in stamped)
                    {
                        Console.WriteLine($"{config.Relative(record.File)}: {record.Id} {Checker.SemanticHash(record)}");
                    }

                    foreach (var id in unknown.Concat(missing))
                    {
                        Console.Error.WriteLine($"No written record holds '{id}'.");
                    }

                    Console.WriteLine($"{stamped.Count} records approved.");
                    return unknown.Count + missing.Count > 0 ? 1 : 0;
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

                case "names":
                {
                    var model = ModelBuilder.Build(config);

                    Console.WriteLine(ModelNames.Report(model, parsed.Value("--format") ?? "text"));
                    return 0;
                }

                case "diff":
                {
                    if (parsed.Positionals is not [var baseArg])
                    {
                        Console.Error.WriteLine("Diff needs a base.");
                        return 2;
                    }

                    if (parsed.Value("--fail-on") is { } failOn && failOn != "breaking")
                    {
                        Console.Error.WriteLine($"Unknown fail-on '{failOn}'; use breaking.");
                        return 2;
                    }

                    var format = parsed.Value("--format") ?? "text";
                    var breakingOnly = parsed.Has("--breaking-only");
                    var view = parsed.Has("--intent") && !parsed.Has("--behavior") ? DiffView.Intent
                        : parsed.Has("--behavior") && !parsed.Has("--intent") ? DiffView.Behavior
                        : DiffView.Both;
                    var headModel = ModelBuilder.Build(config);

                    DiffBase checkout;

                    try
                    {
                        checkout = DiffBase.Load(config, baseArg);
                    }
                    catch (InvalidOperationException exception)
                    {
                        Console.Error.WriteLine(exception.Message);
                        return 2;
                    }

                    using (checkout)
                    {
                        var diff = Differ.Diff(
                            checkout.Model, headModel, checkout.Label, DiffBase.HeadLabel(config));

                        Console.WriteLine(format switch
                        {
                            "markdown" => DiffFormatter.Markdown(diff, breakingOnly, view),
                            "json" => DiffFormatter.Json(diff, breakingOnly, view),
                            _ => DiffFormatter.Text(diff, breakingOnly, view),
                        });

                        return parsed.Value("--fail-on") == "breaking" && diff.BreakingCountIn(view) > 0 ? 1 : 0;
                    }
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

    /// <summary>One batch approval request: a selector and its review report, when one is named.</summary>
    sealed record BatchRequest(string Selector, string? ReviewReport);

    /// <summary>
    ///     Collects the batch requests in the order given: every <c>--batch</c> value,
    ///     and every line of a <c>--batches</c> file at the place the option stands.
    ///     Blank lines and lines starting with <c>#</c> are ignored; a second field
    ///     on a file line names that batch's review report.
    /// </summary>
    /// <param name="rest">The approve arguments after the subcommand, scanned for batch options.</param>
    /// <param name="root">The consumer root a relative batches file resolves against.</param>
    /// <param name="error">The usage error when a batches file is missing, or null.</param>
    /// <returns>The batch requests in the order given.</returns>
    static List<BatchRequest> BatchRequests(string[] rest, string root, out string? error)
    {
        error = null;
        var requests = new List<BatchRequest>();

        for (var index = 0; index < rest.Length; index++)
        {
            var arg = rest[index];
            string? selector = null;
            string? file = null;

            if (arg == "--batch" && index + 1 < rest.Length)
            {
                selector = rest[++index];
            }
            else if (arg.StartsWith("--batch=", StringComparison.Ordinal))
            {
                selector = arg["--batch=".Length..];
            }
            else if (arg == "--batches" && index + 1 < rest.Length)
            {
                file = rest[++index];
            }
            else if (arg.StartsWith("--batches=", StringComparison.Ordinal))
            {
                file = arg["--batches=".Length..];
            }
            else
            {
                continue;
            }

            if (selector is not null)
            {
                requests.Add(new BatchRequest(selector, null));
                continue;
            }

            if (file is null)
            {
                continue;
            }

            var path = Path.IsPathRooted(file)
                ? file
                : File.Exists(Path.Combine(root, file))
                    ? Path.Combine(root, file)
                    : File.Exists(file) ? Path.GetFullPath(file) : Path.Combine(root, file);

            if (!File.Exists(path))
            {
                error = $"No batches file '{file}'.";
                return requests;
            }

            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.Trim();

                if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                {
                    continue;
                }

                var split = trimmed.IndexOfAny([' ', '\t']);

                if (split < 0)
                {
                    requests.Add(new BatchRequest(trimmed, null));
                }
                else
                {
                    var report = trimmed[(split + 1)..].Trim();

                    requests.Add(new BatchRequest(
                        trimmed[..split], report.Length > 0 ? report : null));
                }
            }
        }

        return requests;
    }

    /// <summary>
    ///     Approves many review batches in one run, in the order given, on the model
    ///     built once: each batch is planned and guarded as one batch is, a refused
    ///     batch writes nothing and holds no later batch back, and each applied
    ///     batch updates the in-memory model for the batches after it.
    /// </summary>
    /// <param name="model">The built project model, updated in place as batches apply.</param>
    /// <param name="parsed">The parsed approve arguments.</param>
    /// <param name="requests">The batch requests in the order given.</param>
    /// <param name="all">Whether --all was given, which batch approval refuses.</param>
    /// <param name="ids">The positional record ids, which batch approval refuses.</param>
    /// <param name="root">The consumer root relative paths print against.</param>
    /// <returns>The exit code: 0 when every batch applies, 1 when one is refused, 2 for a usage error.</returns>
    static int ApproveBatches(
        ProjectModel model, CliArgs parsed,
        IReadOnlyList<BatchRequest> requests, bool all, List<string> ids, string root)
    {
        if (all || ids.Count > 0)
        {
            Console.Error.WriteLine("Approve --batch takes a selector and no record ids.");
            return 2;
        }

        var by = parsed.Value("--by");
        var secondReviewer = parsed.Value("--second-reviewer");
        var defaultReport = parsed.Value("--review-report");
        var on = parsed.Value("--on") ?? DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var dryRun = parsed.Has("--dry-run");
        var approved = 0;
        var refused = 0;

        // The open-finding counts are taken once for the run: the manifests record the
        // findings open when the run began, and every batch is planned against them.
        var findings = ReviewQueue.FindingsBySubject(model);

        // Batch membership follows the selector, not the statuses an approval changes, so the
        // queue is computed once too; each plan reads its records from the updated model.
        var queue = ReviewQueue.Compute(model, findings);

        foreach (var request in requests)
        {
            var rawReport = request.ReviewReport ?? defaultReport;
            var reviewReport = rawReport is { Length: > 0 } report && !Path.IsPathRooted(report)
                ? Path.Combine(root, report)
                : rawReport;
            var batch = BatchApprover.Find(queue, request.Selector);

            if (batch is null)
            {
                Console.WriteLine($"{request.Selector}: refused: No batch '{request.Selector}'.");
                refused++;
                continue;
            }

            var plan = BatchApprover.Plan(model, batch, by, secondReviewer, reviewReport, findings);

            if (dryRun)
            {
                Console.WriteLine(BatchApprover.DryRun(plan, model.Config.SampleShare));

                if (plan.Failures.Count > 0)
                {
                    refused++;
                }
                else
                {
                    approved++;
                    BatchApprover.Apply(model, plan, by, on);
                }

                continue;
            }

            if (plan.Failures.Count > 0)
            {
                var reasons = string.Join("; ", plan.Failures.Select(failure => $"{failure.Id}: {failure.Guard}"));

                Console.WriteLine($"{request.Selector}: refused: {reasons}");
                refused++;
                continue;
            }

            var stamps = plan.Ordered
                .Where(action => action.Action is "promoted" or "re-stamped")
                .Select(action => new Approver.BatchStamp(
                    action.Record,
                    action.TargetStatus,
                    by ?? action.Record.Decider ?? action.Record.Owner ?? "unknown",
                    on))
                .ToList();

            Approver.StampBatch(stamps);

            var manifest = BatchApprover.WriteManifest(
                model, plan, by ?? "", secondReviewer, rawReport, on);

            BatchApprover.Apply(model, plan, by, on);
            model.Ledger.Save(model.Config);
            approved++;
            Console.WriteLine($"{request.Selector}: approved: {Relative(root, manifest)}"
                + (plan.LeftOut.Count > 0 ? $", {plan.LeftOut.Count} left out" : ""));
        }

        Console.WriteLine($"{approved} batches approved, {refused} refused.");
        return refused > 0 ? 1 : 0;
    }

    /// <summary>Approves one review batch: its drafts, re-stamps, manifest, and guards.</summary>
    /// <param name="config">The consumer configuration of the project.</param>
    /// <param name="model">The built project model the batch belongs to.</param>
    /// <param name="parsed">The parsed approve arguments.</param>
    /// <param name="selector">The batch selector naming the batch.</param>
    /// <param name="all">Whether --all was given, which batch approval refuses.</param>
    /// <param name="ids">The positional record ids, which batch approval refuses.</param>
    /// <param name="root">The consumer root relative paths print against.</param>
    /// <returns>The exit code: 0 on approval, 1 when the batch is missing or refused, 2 for a usage error.</returns>
    static int ApproveBatch(
        ConsumerConfig config, ProjectModel model, CliArgs parsed, string selector, bool all, List<string> ids, string root)
    {
        if (all || ids.Count > 0)
        {
            Console.Error.WriteLine("Approve --batch takes a selector and no record ids.");
            return 2;
        }

        var batch = BatchApprover.Find(ReviewQueue.Compute(model), selector);

        if (batch is null)
        {
            Console.Error.WriteLine($"No batch '{selector}'.");
            return 1;
        }

        var by = parsed.Value("--by");
        var secondReviewer = parsed.Value("--second-reviewer");
        var reviewReport = parsed.Value("--review-report") is { Length: > 0 } report && !Path.IsPathRooted(report)
            ? Path.Combine(root, report)
            : parsed.Value("--review-report");
        var on = parsed.Value("--on") ?? DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var plan = BatchApprover.Plan(model, batch, by, secondReviewer, reviewReport);

        if (parsed.Has("--dry-run"))
        {
            Console.WriteLine(BatchApprover.DryRun(plan, model.Config.SampleShare));
            return plan.Failures.Count > 0 ? 1 : 0;
        }

        foreach (var record in plan.LeftOut.OrderBy(entry => entry.Id, StringComparer.Ordinal))
        {
            Console.WriteLine($"{record.Id}: left out (tier 1)");
        }

        if (plan.Failures.Count > 0)
        {
            foreach (var (id, guard) in plan.Failures)
            {
                Console.WriteLine($"{id}: {guard}");
            }

            Console.WriteLine($"Refused: {plan.Failures.Count} guard failures.");
            return 1;
        }

        var stamps = plan.Ordered
            .Where(action => action.Action is "promoted" or "re-stamped")
            .Select(action => new Approver.BatchStamp(
                action.Record,
                action.TargetStatus,
                by ?? action.Record.Decider ?? action.Record.Owner ?? "unknown",
                on))
            .ToList();
        var stamped = Approver.StampBatch(stamps);
        var manifest = BatchApprover.WriteManifest(
            model, plan, by ?? "", secondReviewer, parsed.Value("--review-report"), on);

        foreach (var action in plan.Ordered.Where(entry => entry.Action is "promoted" or "re-stamped"))
        {
            Console.WriteLine(action.Action == "promoted"
                ? $"{config.Relative(action.Record.File)}: {action.Record.Id} promoted to {action.TargetStatus} {action.Hash}"
                : $"{config.Relative(action.Record.File)}: {action.Record.Id} re-stamped {action.Hash}");
        }

        var held = plan.Ordered.Count - stamped.Count;

        Console.WriteLine($"{Relative(root, manifest)}: manifest ({plan.Ordered.Count} records, sample {plan.Sample.Count}).");
        Console.WriteLine($"{Counted(stamped.Count, "record")} approved, {held} held"
            + (plan.LeftOut.Count > 0 ? $", {plan.LeftOut.Count} left out" : "")
            + ".");
        model.Ledger.Save(config);
        return 0;
    }

    /// <summary>A path relative to the root, written with forward slashes.</summary>
    /// <param name="root">The root the path is made relative to.</param>
    /// <param name="path">The absolute path to relativize.</param>
    /// <returns>The relative path with forward slashes.</returns>
    static string Relative(string root, string path) =>
        Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');

    /// <summary>A count with its noun, pluralized unless the count is one.</summary>
    /// <param name="count">The number counted.</param>
    /// <param name="noun">The singular noun.</param>
    /// <returns>The count and noun, such as 1 record or 3 records.</returns>
    static string Counted(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";

    /// <summary>The usage error for an unknown diff format.</summary>
    /// <param name="format">The format value given, or null.</param>
    /// <returns>The error naming the value, or null for text, markdown, json or no value.</returns>
    static string? DiffFormatError(string? format) =>
        format is null or "text" or "markdown" or "json"
            ? null
            : $"Unknown format '{format}'; use text, markdown or json.";

    /// <summary>
    ///     Decides one finding's severity from its subject, its path and the
    ///     qualification of the reader that produced it: TypeScript findings take the
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
            finding.Subject is not null && model.ById.TryGetValue(finding.Subject, out var subject) ? subject.Status : null,
            (finding.Code == "reader-missing" && config.IsContinuousIntegration) || finding.Reader switch
            {
                Finding.TypeScriptReader => model.TypeScriptQualified,
                Finding.RoslynReader => true,
                _ => true,
            });

        return release && severity.Outcome == Outcome.Reported && IsReleaseReadiness(model, finding)
            ? new Severity(Outcome.Blocking, null)
            : severity;
    }

    /// <summary>
    ///     Whether the finding gates a release: an unverified use case, or an
    ///     unanswered question on a tier 1 record.
    /// </summary>
    /// <param name="model">The built project model the finding belongs to.</param>
    /// <param name="finding">The finding to test.</param>
    /// <returns>True when the finding gates a release.</returns>
    static bool IsReleaseReadiness(ProjectModel model, Finding finding) =>
        finding.Code == "use-case-unverified"
        || (finding.Code == "question-expired"
            && finding.Subject is not null
            && model.ById.TryGetValue(Identifiers.BaseId(finding.Subject), out var subject)
            && ReviewQueue.HasRisk(subject));

    /// <summary>
    ///     Parses a <c>--level</c> value into its level set: a comma-separated
    ///     subset of <c>G</c>, <c>S</c> and <c>A</c>, or null when the option is
    ///     absent. Any other value is a usage error naming the value.
    /// </summary>
    /// <param name="value">The --level value, or null when the option is absent.</param>
    /// <returns>The level set with a null error, or a null set with the usage error.</returns>
    static (HashSet<string>? Levels, string? Error) ParseLevels(string? value)
    {
        if (value is null)
        {
            return (null, null);
        }

        var levels = new HashSet<string>(StringComparer.Ordinal);

        foreach (var part in value.Split(','))
        {
            var level = part.Trim();

            if (level is not ("G" or "S" or "A"))
            {
                return (null, $"Unknown level '{level}'; use a comma-separated subset of G, S, A.");
            }

            levels.Add(level);
        }

        return (levels, null);
    }

    /// <summary>One finding's printed line: its path, line, level, code and message.</summary>
    /// <param name="config">The consumer configuration, for the relative path.</param>
    /// <param name="finding">The finding to print.</param>
    /// <param name="severity">The decided severity of the finding.</param>
    /// <returns>The printed line for the finding.</returns>
    static string FindingLine(ConsumerConfig config, Finding finding, Severity severity) =>
        $"{config.Relative(finding.Path)}:{finding.Line} [{TierLabel(finding, severity)}] [{finding.Code}] {finding.Message}";

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

    /// <summary>The printed tier: the tier plus the reason a reported finding does not block.</summary>
    /// <param name="finding">The finding whose tier is printed.</param>
    /// <param name="severity">The decided severity, whose reason is appended.</param>
    /// <returns>The tier, with the non-blocking reason when one is recorded.</returns>
    static string TierLabel(Finding finding, Severity severity) =>
        severity.Reason is null ? finding.Tier : finding.Tier + " " + severity.Reason;

    /// <summary>One standing finding as its JSON object.</summary>
    /// <param name="config">The consumer configuration, for the relative path.</param>
    /// <param name="finding">The finding to project.</param>
    /// <param name="severity">The decided severity of the finding.</param>
    /// <returns>The JSON object for the finding.</returns>
    static object AsJson(ConsumerConfig config, Finding finding, Severity severity) => new
    {
        code = finding.Code,
        tier = finding.Tier,
        outcome = severity.Outcome == Outcome.Blocking ? "blocking" : "reported",
        reason = severity.Reason,
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
        tier = finding.Tier,
        outcome = "known",
        reason = (string?)null,
        message = finding.Message,
        path = config.Relative(finding.Path),
        line = finding.Line,
        subject = finding.Subject,
    };
}
