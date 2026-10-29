using System.Text.Json;
using LiteSpec.Model.Diff;

namespace LiteSpec.Model;

/// <summary>
///     The <c>litespec model</c> command: build, check, render and query a consumer's project model.
/// </summary>
/// <follows>litespec/core/model#mark-every-check-with-its-tier</follows>
static class ModelCommand
{
    public const string Usage = """
        Usage:
          litespec model build --root <consumer> [--out model.json]
          litespec model check --root <consumer> [--format json] [--staged]
          litespec model render --root <consumer>
          litespec model query --root <consumer> <id>
          litespec model names [--root <consumer>] [--format text|json]
          litespec model review --root <consumer> [--decider <owner>] [--tier 1|3] [--module <module>] [--type <type>] [--format json]
          litespec model approve --root <consumer> (<id>... | --all) [--by <owner>] [--on <YYYY-MM-DD>] [--dry-run]
          litespec model approve --root <consumer> --batch <selector> --by <owner> [--second-reviewer <owner>|agent] [--review-report <file>] [--on <YYYY-MM-DD>] [--dry-run]
          litespec model backfill --root <consumer> [<module>] [--dry-run]
          litespec model diff <base> --root <consumer> [--format text|markdown|json] [--intent|--behavior] [--fail-on breaking] [--breaking-only]
          litespec model --help

        Exit codes: 0 no open blocking finding, 1 an open blocking finding, no record with the id, a refused batch, or a breaking intent change with --fail-on breaking, 2 usage error.
        """;

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
                "check" => ["--staged"],
                "backfill" => ["--dry-run"],
                _ => [],
            },
            command switch
            {
                "diff" => ["--root", "--out", "--format", "--fail-on"],
                "approve" => ["--root", "--by", "--on", "--batch", "--second-reviewer", "--review-report"],
                "backfill" => ["--root"],
                "review" => ["--root", "--out", "--format", "--decider", "--tier", "--module", "--type"],
                _ => ["--root", "--out", "--format"],
            },
            maxPositionals: command switch
            {
                "query" or "diff" or "backfill" => 1,
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
                    var output = parsed.Value("--out") ?? Path.Combine(root, ".model", "model.json");

                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
                    File.WriteAllText(output, model.ToJson() + "\n");
                    model.Ledger.Save(root);

                    foreach (var file in FrontendRecords.Write(model))
                    {
                        Console.WriteLine(file);
                    }

                    Console.WriteLine($"{Relative(root, output)}: {model.Records.Count} records, {model.Code.Handlers.Count} handlers, {model.Screens.Count} screens, {model.Tests.Count} scenarios.");
                    return 0;
                }

                case "check":
                {
                    var model = ModelBuilder.Build(config);
                    var (findings, known) = Checker.Check(model);

                    model.Ledger.Save(root);
                    var json = parsed.Json;
                    StagedScope.Scope? staged = null;

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

                    var phase = config.Phase.ToProjectValue();
                    var qualified = Finding.ReaderQualified(config.Stacks);
                    var shown = findings
                        .Select(finding => (Finding: finding, Severity: Decide(model, config, finding, phase, qualified)))
                        .Where(entry => entry.Severity.Outcome != Outcome.Silent)
                        .ToList();
                    var shownKnown = known
                        .Where(gap => Decide(model, config, gap, phase, qualified).Outcome != Outcome.Silent)
                        .ToList();

                    if (json)
                    {
                        object payload = staged is null
                            ? new
                            {
                                findings = shown.Select(entry => AsJson(config, entry.Finding, entry.Severity)).ToList(),
                                known = shownKnown.Select(gap => AsKnownJson(config, gap)).ToList(),
                            }
                            : new
                            {
                                scope = "staged",
                                stagedFiles = staged.StagedFiles,
                                stagedRecords = staged.RecordIds.Count,
                                findings = shown.Select(entry => AsJson(config, entry.Finding, entry.Severity)).ToList(),
                                known = shownKnown.Select(gap => AsKnownJson(config, gap)).ToList(),
                            };

                        Console.WriteLine(JsonSerializer.Serialize(payload, CliJson.Options));
                    }
                    else
                    {
                        if (staged is not null)
                        {
                            Console.WriteLine($"Staged scope: {staged.StagedFiles} files, {staged.RecordIds.Count} records.");
                        }

                        foreach (var entry in shown)
                        {
                            Console.WriteLine($"{config.Relative(entry.Finding.Path)}:{entry.Finding.Line} [{TierLabel(entry.Finding, entry.Severity)}] [{entry.Finding.Code}] {entry.Finding.Message}");
                        }

                        foreach (var gap in shownKnown)
                        {
                            Console.WriteLine($"{config.Relative(gap.Path)}:{gap.Line} [known {gap.Tier}] [{gap.Code}] {gap.Message}");
                        }

                        Console.WriteLine($"{shown.Count} findings, {shownKnown.Count} known.");
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

                    model.Ledger.Save(root);
                    Console.WriteLine(parsed.Json ? ReviewQueue.Json(batches) : ReviewQueue.Human(batches));
                    return 0;
                }

                case "approve":
                {
                    var model = ModelBuilder.Build(config);
                    var all = parsed.Has("--all");
                    var ids = parsed.Positionals;

                    if (parsed.Value("--batch") is { } selector)
                    {
                        return ApproveBatch(config, model, parsed, selector, all, ids, root);
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
                        ? written.Where(record => record.Status is "approved" or "reviewed" && string.IsNullOrEmpty(record.ApprovedHash)).ToList()
                        : written.Where(record => ids.Contains(record.Id)).ToList();
                    var unknown = all ? [] : ids.Where(id => written.All(record => record.Id != id)).ToList();

                    if (parsed.Has("--dry-run"))
                    {
                        var planned = all
                            ? written.Where(record => record.Status is "approved" or "reviewed").ToList()
                            : targets;
                        var dry = Approver.DryRun(planned);

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

                case "backfill":
                {
                    var model = ModelBuilder.Build(config);
                    var (lines, unknownModule) = Backfill.Run(
                        model, parsed.Positionals.FirstOrDefault(), parsed.Has("--dry-run"));

                    if (unknownModule)
                    {
                        Console.Error.WriteLine($"No use-case, reaction or job records in module '{parsed.Positionals[0]}'.");
                        return 1;
                    }

                    foreach (var line in lines)
                    {
                        Console.WriteLine(line);
                    }

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

    /// <summary>Approves one review batch: its drafts, re-stamps, manifest, and guards.</summary>
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
        model.Ledger.Save(root);
        return 0;
    }

    static string Relative(string root, string path) =>
        Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');

    static string Counted(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";

    static string? DiffFormatError(string? format) =>
        format is null or "text" or "markdown" or "json"
            ? null
            : $"Unknown format '{format}'; use text, markdown or json.";

    /// <summary>Decides one finding's severity from its subject, its path, the phase and the reader.</summary>
    static Severity Decide(ProjectModel model, ConsumerConfig config, Finding finding, string phase, bool qualified) =>
        Severity.Of(
            finding,
            finding.Subject is not null && model.EffectiveStatus.TryGetValue(finding.Subject, out var status) ? status : null,
            UnderTestRoot(config, finding.Path),
            phase,
            qualified);

    /// <summary>Whether the path lies under a test root.</summary>
    static bool UnderTestRoot(ConsumerConfig config, string path)
    {
        var comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return config.TestRoots.Any(root =>
            string.Equals(path, root, comparison)
            || path.StartsWith(root + Path.DirectorySeparatorChar, comparison));
    }

    /// <summary>The printed tier: the tier plus the reason a reported S finding does not block.</summary>
    static string TierLabel(Finding finding, Severity severity) =>
        severity.Reason is null ? finding.Tier : finding.Tier + " " + severity.Reason;

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
