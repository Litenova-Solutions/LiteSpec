using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Model.Readers;
using LiteSpec.Model.Readers.Coverage;
using LiteSpec.Model.Readers.Measures;

namespace LiteSpec.Model;

/// <summary>
///     Builds the project model: reads records, code, screens, tests, the test run,
///     scenario coverage and measured-check results, then computes the implements,
///     calls and proves links and the status.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static partial class ModelBuilder
{
    /// <summary>Builds the model for the consumer.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <returns>The built project model.</returns>
    public static ProjectModel Build(ConsumerConfig config)
    {
        var total = System.Diagnostics.Stopwatch.StartNew();
        var model = new ProjectModel { Config = config };

        var read = Timed("records", () => RecordReader.Read(config));

        model.Problems.AddRange(read.Problems);
        model.Problems.AddRange(config.Problems);
        model.Records.AddRange(read.Records);
        model.Bodies.AddRange(read.Bodies);
        model.Provisions = read.Provisions;
        model.AppliedPages = AppliedPagesOf(config, model.Problems);
        var stacks = StackReaders.For(config);
        model.Problems.AddRange(stacks.Problems);
        var backendRead = Timed("code", () => stacks.Backend.Read(config));

        model.Code = backendRead.Code;
        model.Problems.AddRange(backendRead.Problems);

        var screens = Timed("screens", () => ReadScreens(config, stacks, model.Code));

        model.Screens.AddRange(screens.Screens);
        model.ScreenComments.AddRange(screens.Comments);
        model.BrowserTests.AddRange(screens.BrowserTests);
        model.Problems.AddRange(screens.Findings);
        model.TypeScriptQualified = screens.TypeScriptQualified;

        var scenarioRead = Timed("tests", () => stacks.Scenarios.Read(config));
        var tests = scenarioRead.Tests;

        model.Problems.AddRange(scenarioRead.Problems);
        model.Tests.AddRange(tests.Scenarios);
        model.Code.Elements.AddRange(tests.Elements);
        model.Code.StrayMarks.AddRange(tests.StrayMarks);
        model.StepCalls = tests.StepCalls;
        model.StepFacts = tests.StepFacts;
        model.ProbeRoutes = tests.ProbeRoutes;
        model.TestComments.AddRange(tests.Comments);
        model.TestRun = Timed("testrun", () => stacks.TestRuns.Read(config));
        model.Ledger = ProofLedger.Load(config);
        model.Problems.AddRange(model.Ledger.Problems);

        var coverage = Timed("coverage", () => CoverageReader.Read(config));

        model.UnreachedCode.AddRange(coverage.Gaps);
        model.CoverageRead = coverage.Read;
        model.Measures.AddRange(Timed("measures", () => MeasureReader.Read(config)));

        Timed("compute", () =>
        {
            ModelLinks.MatchEvents(model);
            ModelLinks.ComputeImplements(model);
            ModelLinks.ComputeFollows(model);
            ComputeCalls(model);
            RefusalCodes.Compute(model);
            ProofConfirmation.Compute(model);
            ModelStatus.Compute(model);
            ModelStatus.ComputeAuthority(model);
            Constituents.Compute(model);
        });

        Timing.Report($"[time] build total {total.Elapsed.TotalSeconds:0.0}s");

        return model;
    }

    /// <summary>Runs one build phase and reports how long it took.</summary>
    /// <param name="name">The phase name.</param>
    /// <param name="run">The phase.</param>
    /// <returns>The value the phase produced.</returns>
    /// <typeparam name="T">The value type the phase produces.</typeparam>
    private static T Timed<T>(string name, Func<T> run)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var value = run();

        Timing.Report($"[time] {name} {watch.Elapsed.TotalSeconds:0.0}s");

        return value;
    }

    /// <summary>Runs one build phase and reports how long it took.</summary>
    /// <param name="name">The phase name.</param>
    /// <param name="run">The phase.</param>
    private static void Timed(string name, Action run) =>
        Timed(name, () => { run(); return 0; });

    /// <summary>
    ///     The standard pages the project's stacks and extensions apply, through the
    ///     shared stack layout. Empty when the carried release or its manifest cannot
    ///     be read, so a provision check falls back to existence only; that failure
    ///     is reported as a <c>reader-missing</c> problem naming the manifest.
    /// </summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="problems">The problems a failure is reported into.</param>
    /// <returns>The standard pages the project's stacks and extensions apply.</returns>
    private static HashSet<string> AppliedPagesOf(ConsumerConfig config, List<Finding> problems)
    {
        var home = LiteSpecHome.Root;

        if (home is null)
        {
            problems.Add(new Finding(
                "reader-missing",
                $"The LiteSpec manifest '{LiteSpecHome.ManifestFile}' could not be read: no LiteSpec release was found; the applied pages were not read.",
                Path.Combine(config.Root, LiteSpecHome.ManifestFile),
                1,
                null)
            {
                Reader = Finding.RoslynReader,
                ReaderIsQualified = true,
            });

            return new HashSet<string>(StringComparer.Ordinal);
        }

        var manifestFile = Path.Combine(home, LiteSpecHome.ManifestFile);

        if (!File.Exists(manifestFile))
        {
            problems.Add(new Finding(
                "reader-missing",
                $"The LiteSpec manifest '{manifestFile}' could not be read: the file does not exist; the applied pages were not read.",
                manifestFile,
                1,
                null)
            {
                Reader = Finding.RoslynReader,
                ReaderIsQualified = true,
            });

            return new HashSet<string>(StringComparer.Ordinal);
        }

        try
        {
            if (System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(manifestFile)) is not System.Text.Json.Nodes.JsonObject manifest)
            {
                problems.Add(new Finding(
                    "reader-missing",
                    $"The LiteSpec manifest '{manifestFile}' could not be read: its root is not a JSON object; the applied pages were not read.",
                    manifestFile,
                    1,
                    null)
                {
                    Reader = Finding.RoslynReader,
                    ReaderIsQualified = true,
                });

                return new HashSet<string>(StringComparer.Ordinal);
            }

            return new HashSet<string>(
                Authoring.Layout.AppliedPages(home, manifest, config.Shape, config.Stacks, config.Extensions),
                StringComparer.Ordinal);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            problems.Add(new Finding(
                "reader-missing",
                $"The LiteSpec manifest '{manifestFile}' could not be read: {exception.Message}; the applied pages were not read.",
                manifestFile,
                1,
                null)
            {
                Reader = Finding.RoslynReader,
                ReaderIsQualified = true,
            });

            return new HashSet<string>(StringComparer.Ordinal);
        }
    }

    /// <summary>
    ///     Reads every frontend's routes through its own reader. Screens keep the
    ///     frontends' order; comments are ordered by file and line across frontends.
    /// </summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="stacks">The readers the project's stacks select.</param>
    /// <param name="code">The code model.</param>
    /// <returns>The screens, comments, browser tests, and findings every frontend read.</returns>
    private static ScreenRead ReadScreens(ConsumerConfig config, StackSet stacks, CodeModel code)
    {
        var screens = new List<ScreenInfo>();
        var comments = new List<CodeComment>();
        var browserTests = new List<BrowserTestInfo>();
        var findings = new List<Finding>();
        var qualified = false;

        foreach (var frontend in stacks.Frontends)
        {
            var read = frontend.Read(config, code);

            screens.AddRange(read.Screens);
            comments.AddRange(read.Comments);
            browserTests.AddRange(read.BrowserTests);
            findings.AddRange(read.Findings);
            qualified |= read.TypeScriptQualified;
        }

        return new ScreenRead(
            screens,
            [.. comments
                .OrderBy(comment => comment.File, StringComparer.Ordinal)
                .ThenBy(comment => comment.Line)])
        {
            BrowserTests = browserTests,
            Findings = findings,
            TypeScriptQualified = qualified,
        };
    }

    /// <summary>The record's links with their targets, for model.json.</summary>
    /// <param name="model">The project model.</param>
    /// <param name="record">The record whose links are resolved.</param>
    /// <returns>The record's links with their targets.</returns>
    public static Dictionary<string, List<string>> ResolvedLinks(ProjectModel model, Record record) =>
        ModelLinks.ResolvedLinks(model, record);

    /// <summary>Every id a link field names, reading through nested objects and self() grants.</summary>
    /// <param name="value">The link field value.</param>
    /// <returns>Every id a link field names.</returns>
    public static List<string> TargetsOf(JsonElement value)
    {
        var targets = new List<string>();

        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                if (value.GetString() is { } single)
                {
                    targets.Add(UseCaseFacts.UnwrapSelf(single));
                }

                break;

            case JsonValueKind.Array:
                foreach (var entry in value.EnumerateArray())
                {
                    targets.AddRange(TargetsOf(entry));
                }

                break;

            case JsonValueKind.Object:
                foreach (var name in new[] { "role", "useCase", "requires", "events", "actor", "grant", "runs", "sends", "emits", "calls", "fallback", "enforcedBy", "acceptedBy", "compensation", "authority", "operation", "event" })
                {
                    if (value.TryGetProperty(name, out var nested))
                    {
                        targets.AddRange(TargetsOf(nested));
                    }
                }

                break;
        }

        return targets;
    }

    /// <summary>Computes the calls links and the unmatched screen operations.</summary>
    /// <param name="model">The project model.</param>
    internal static void ComputeCalls(ProjectModel model)
    {
        foreach (var screen in model.Screens)
        {
            foreach (var call in screen.PairedCalls())
            {
                model.Calls.Add(new CallsLink(
                    $"{screen.Frontend} {screen.Route}", call.UseCase, call.Operation));
            }
        }

        var byRoute = new HashSet<string>(StringComparer.Ordinal);
        var byOperation = new HashSet<string>(StringComparer.Ordinal);

        foreach (var endpoint in model.Code.Endpoints)
        {
            if (endpoint.Operation.Length > 0)
            {
                byOperation.Add(Naming.OperationKey(endpoint.Operation));
            }

            if (endpoint.Method.Length > 0 && endpoint.Route.Length > 0)
            {
                byRoute.Add($"{endpoint.Method} {Template(endpoint.Route)}");
            }
        }

        foreach (var screen in model.Screens)
        {
            foreach (var operation in screen.Operations)
            {
                if (!byRoute.Contains($"{operation.Method} {Template(operation.Path)}")
                    && !byOperation.Contains(Naming.OperationKey(operation.Name)))
                {
                    model.UnmatchedCalls.Add(new UnmatchedCall(
                        $"{screen.Frontend} {screen.Route}", operation.Name, screen.File));
                }
            }
        }

        model.UnmatchedCalls.Sort((left, right) =>
        {
            var screen = string.Compare(left.Screen, right.Screen, StringComparison.Ordinal);

            return screen != 0
                ? screen
                : string.Compare(left.Operation, right.Operation, StringComparison.Ordinal);
        });
    }

    /// <summary>
    ///     The route template with parameter constraints removed, mirroring the frontend
    ///     reader's join so unmatched calls are exactly the operations it drops.
    /// </summary>
    /// <param name="route">The route template.</param>
    /// <returns>The route template with parameter constraints removed.</returns>
    private static string Template(string route) => TemplateConstraint().Replace(route, "{$1}");

    /// <summary>The parameter-constraint part of a route template.</summary>
    /// <returns>The regular expression matching a parameter constraint.</returns>
    [GeneratedRegex(@"\{(?<name>[^}:]+):[^}]+\}")]
    private static partial Regex TemplateConstraint();

    /// <summary>Whether the use case lets anonymous callers in.</summary>
    /// <param name="useCase">The use case record.</param>
    /// <returns>True when the use case's access grant is anonymous.</returns>
    public static bool Anonymous(Record useCase) => UseCaseFacts.Anonymous(useCase);

    /// <summary>
    ///     The permission the use case's access grant names, reading through self(),
    ///     or null when the grant is anonymous, system, or missing.
    /// </summary>
    /// <param name="useCase">The use case record.</param>
    /// <returns>The permission the grant names, or null when it names none.</returns>
    public static string? GrantedPermission(Record useCase) => UseCaseFacts.GrantedPermission(useCase);

    /// <summary>
    ///     The refusal code of the rule: the code of the refusal result it names,
    ///     or its bare refusal code. The shared refusal rule over the model.
    /// </summary>
    /// <param name="model">The project model.</param>
    /// <param name="rule">The rule record.</param>
    /// <returns>The refusal code of the rule, or null when it refuses with none.</returns>
    public static string? RefusalResultCode(ProjectModel model, Record rule) => RefusalCodes.ResultCode(model, rule);

    /// <summary>Whether any of the use case's inputs carries rules.</summary>
    /// <param name="useCase">The use case record.</param>
    /// <returns>True when one of the use case's inputs carries rules.</returns>
    public static bool HasValidatedInput(Record useCase) => UseCaseFacts.HasValidatedInput(useCase);

    /// <summary>Whether the use case runs in the background: a reaction or job runs it.</summary>
    /// <param name="model">The project model.</param>
    /// <param name="useCase">The use case record.</param>
    /// <returns>True when a reaction or job runs the use case.</returns>
    public static bool BackgroundOnly(ProjectModel model, Record useCase) => UseCaseFacts.BackgroundOnly(model, useCase);

    /// <summary>The aggregate whose folder holds the use case's page.</summary>
    /// <param name="model">The project model.</param>
    /// <param name="useCase">The use case record.</param>
    /// <returns>The aggregate holding the use case's page, or null when none holds it.</returns>
    public static Record? AggregateOf(ProjectModel model, Record useCase) => UseCaseFacts.AggregateOf(model, useCase);

    /// <summary>The use case a result or scenario home names, when one holds it.</summary>
    /// <param name="model">The project model.</param>
    /// <param name="home">The home name a result or scenario identifier carries.</param>
    /// <returns>The use case the home names, or null when none holds it.</returns>
    public static Record? UseCaseOf(ProjectModel model, string home) =>
        model.UseCasesByName.TryGetValue(home, out var useCase) ? useCase : null;

    /// <summary>
    ///     Confirms a proves claim. A steps test proves through the public boundary: a
    ///     when step of the use case's steps class or generated-client operation plus a
    ///     then step, asserting the refusal code for a refusal. A refusal also confirms
    ///     from the HTTP status the project's refusal table maps to its code, or from a
    ///     client code the project's refusal map translates to it. An integration test
    ///     proves by constructing the message it sends or requesting the use case's own
    ///     endpoint, plus an assertion. No test constructs a handler, an aggregate, or a
    ///     repository directly. A rule claim confirms through a scenario the same test
    ///     proves, or through an integration fact that resolves a service from the host
    ///     and calls the message type of a governed use case or the governed aggregate.
    ///     A requirement, decision, external-system, or integration-event claim
    ///     confirms on its own once it resolves: no step, message, or effect analysis
    ///     applies, and its passing run marks it tested. A provision claim confirms
    ///     when the carried release holds it.
    /// </summary>
    /// <param name="model">The project model.</param>
    /// <param name="test">The scenario test.</param>
    /// <param name="claim">The record the test claims to prove.</param>
    /// <returns>True when the test confirms the claim.</returns>
    public static bool Confirm(ProjectModel model, ScenarioInfo test, string claim) =>
        ProofConfirmation.Confirm(model, test, claim);

    /// <summary>
    ///     Confirms a browser test's claim: a scenario confirms when one of its
    ///     <c>gotos</c> matches the <c>address</c> of a screen that offers the
    ///     scenario's use case (a <c>[param]</c> or <c>{}</c> segment matches any
    ///     segment), and its <c>codes</c> hold the refusal code (a refusal
    ///     scenario) or one of its asserted texts is the result's sentence or the
    ///     scenario's Then text (a success scenario). Rule and provision claims
    ///     follow the same rules as a backend test's claims.
    /// </summary>
    /// <param name="model">The project model.</param>
    /// <param name="test">The browser test.</param>
    /// <param name="claim">The record the test claims to prove.</param>
    /// <returns>True when the browser test confirms the claim.</returns>
    public static bool BrowserConfirm(ProjectModel model, BrowserTestInfo test, string claim) =>
        ProofConfirmation.BrowserConfirm(model, test, claim);
}
