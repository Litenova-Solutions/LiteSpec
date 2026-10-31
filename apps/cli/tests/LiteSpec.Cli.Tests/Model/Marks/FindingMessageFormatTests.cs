using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Model;
using LiteSpec.Tests.Support;
using Record = LiteSpec.Model.Record;

namespace LiteSpec.Tests.Model.Marks;

/// <summary>Proves every finding message from the code-side checkers starts with a subject title and quoted identifier.</summary>
public sealed class FindingMessageFormatTests
{
    /// <summary>Every message starts with a title and quoted identifier: "Use case '...'", "Route '...'", and the rest.</summary>
    static readonly Regex Format = new(@"^[A-Z][A-Za-z ]* '[^']+'", RegexOptions.Compiled);

    /// <summary>Phrases a B8 message never carries: instructions and past-tense read failures.</summary>
    static readonly string[] Banned =
    [
        "register it in the container",
        "delete the code",
        "write its parent record",
        "drafting the requirement",
        "runs after the commit",
        "after the commit from an event handler or a job",
        "could not be read",
        "Handler for '",
        "Query handler for '",
        "Endpoint for '",
    ];

    /// <proves>litespec/core/language#write-each-finding-message-as-a-fact</proves>
    [Fact]
    public void Code_Messages_Start_With_A_Subject_And_Identifier()
    {
        using var temp = Cli.Temp("marks");
        var model = CodeModel(temp.Path);

        var codes = new HashSet<string>(StringComparer.Ordinal)
        {
            "write-undeclared", "result-undeclared", "effect-undeclared", "use-case-method-drift",
            "event-tense-invalid", "name-drift", "operation-id-duplicate", "route-invalid",
            "name-reason-missing", "collaborator-repository-invalid", "repository-read-invalid",
            "query-filter-invalid", "dependency-count-invalid", "effect-timing-invalid",
            "repository-unimplemented",
        };

        var findings = Checker.Check(model).Findings.Where(finding => codes.Contains(finding.Code)).ToList();

        Assert.NotEmpty(findings);
        Assert.All(findings, AssertFormat);
    }

    /// <proves>litespec/core/language#write-each-finding-message-as-a-fact</proves>
    [Fact]
    public void Contract_And_Template_Store_Messages_Start_With_A_Subject_And_Identifier()
    {
        using var temp = Cli.Temp("marks");
        File.WriteAllText(
            Path.Combine(temp.Path, "litespec.project.json"),
            """{"owners": ["lending-team"], "paths": {"contract": "missing.json"}, "stackConfig": {"dotnet": {"templateStore": "MessageTemplates.cs"}}}""");
        var model = new ProjectModel { Config = ConsumerConfig.Load(temp.Path) };
        model.Code.Endpoints.Add(new EndpointOperation("BorrowCopy", "use-case/lending.borrow-copy", "POST", "/api/loans/borrow", Path.Combine(temp.Path, "Endpoint.cs"), 3));

        var findings = Checker.Check(model).Findings
            .Where(finding => finding.Code is "operation-use-case-drift" or "template-unimplemented")
            .ToList();

        Assert.NotEmpty(findings);
        Assert.All(findings, AssertFormat);
    }

    /// <proves>litespec/core/language#write-each-finding-message-as-a-fact</proves>
    [Fact]
    public void Mark_Messages_Start_With_A_Subject_And_Identifier()
    {
        var findings = new List<Finding>();

        foreach (var name in new[] { "act", "count", "drift", "effects", "exposed", "follows", "missing", "proof-targets", "style", "unimplemented", "unresolved" })
        {
            using var fixture = MarksFixture.Prepare(name);
            findings.AddRange(MarksFixture.Check(fixture.Path));
        }

        var codes = new HashSet<string>(StringComparer.Ordinal)
        {
            "mark-missing", "mark-style-invalid", "mark-count-invalid", "mark-unknown", "mark-folder-drift",
            "decision-unimplemented", "runtime-value-drift", "use-case-act-missing",
            "act-event-undeclared", "act-event-unimplemented", "aggregate-unimplemented",
            "reaction-unimplemented", "job-unimplemented", "external-system-unimplemented",
            "use-case-endpoint-missing", "effect-unimplemented", "use-case-unreached",
        };

        var ours = findings.Where(finding => codes.Contains(finding.Code)).ToList();

        Assert.NotEmpty(ours);
        Assert.All(ours, AssertFormat);
    }

    /// <proves>litespec/core/language#write-each-finding-message-as-a-fact</proves>
    [Fact]
    public void Screen_Setting_Constituent_And_Record_Messages_Start_With_A_Subject_And_Identifier()
    {
        using var temp = Cli.Temp("marks");
        var model = MixedModel(temp.Path);

        var codes = new HashSet<string>(StringComparer.Ordinal)
        {
            "screen-unimplemented", "behavior-unrecorded", "screen-offers-drift", "screen-serves-drift",
            "step-screen-missing", "refusal-sentence-missing", "proves-unreached", "proves-duplicate",
            "setting-key-missing", "setting-unimplemented", "result-scenario-missing",
            "constituent-parent-missing", "quality-alert-missing", "admission-invalid",
        };

        var findings = Checker.Check(model).Findings.Where(finding => codes.Contains(finding.Code)).ToList();

        Assert.NotEmpty(findings);
        Assert.All(findings, AssertFormat);
    }

    static void AssertFormat(Finding finding)
    {
        Assert.Matches(Format, finding.Message);
        Assert.EndsWith(".", finding.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("  ", finding.Message, StringComparison.Ordinal);

        foreach (var banned in Banned)
        {
            Assert.DoesNotContain(banned, finding.Message, StringComparison.Ordinal);
        }
    }

    /// <summary>A model triggering the code-reading findings: one handler per shape, one raising method, and two endpoints sharing an operation.</summary>
    static ProjectModel CodeModel(string root)
    {
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            """{"owners": ["lending-team"], "stackConfig": {"dotnet": {"infrastructure": "src/Infrastructure"}}}""");
        Directory.CreateDirectory(Path.Combine(root, "src", "Infrastructure"));
        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };

        model.Records.Add(Rec("use-case", "use-case/lending.void-order", Path.Combine(root, "void.md"), """{"status": "reviewed", "kind": "command", "effects": [{"sends": "notification/lending.voided", "timing": "inline"}]}"""));
        model.Records.Add(Rec("aggregate", "aggregate/lending.order", Path.Combine(root, "order.md"), """{"status": "reviewed", "states": [{"name": "Open", "meaning": "The order runs."}]}"""));
        model.Records.Add(Rec("use-case", "use-case/lending.borrow-copy", Path.Combine(root, "borrow.md"), """{"status": "reviewed", "kind": "command", "changes": ["aggregate/lending.order"]}"""));

        var handlerFile = Path.Combine(root, "src", "App", "Shared", "VoidOrderCommandHandler.cs");
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.void-order", "lending", "void-order", "VoidOrder", "command", handlerFile,
            Repositories: [new CodeRepository("IHoldRepository", handlerFile, 5)],
            Refusals: [new CodeRefusal("lending.ghost-refusal", handlerFile, 9)],
            Dispatches: [new CodeDispatch("runs", "PurgeLoansCommand", "", handlerFile, 13)],
            MessageType: "CancelOrderCommand",
            DependencyCount: 11,
            AggregateCalls: [new CodeCall("order", "Abandon", handlerFile, 8)],
            Collaborators: [new CodeCollaborator("OrderEraser", handlerFile, 7)],
            LoopReads: [new CodeLoopRead("IOrderRepository", "FindByIdAsync", handlerFile, 10)],
            MemoryFilters: [new CodeMemoryFilter(handlerFile, 9)]));
        model.Code.AggregateMethods.Add(new AggregateMethod("Order", "Abandon", ["OrderVoidedEvent"], Path.Combine(root, "Order.cs"), 5));
        model.Code.Endpoints.Add(new EndpointOperation("CancelOrder", "use-case/lending.void-order", "POST", "/api/ledger/{orderId}/cancel", Path.Combine(root, "A.cs"), 3));
        model.Code.Endpoints.Add(new EndpointOperation("CancelOrder", "use-case/lending.void-order", "POST", "/api/ledger/{orderId}/cancel", Path.Combine(root, "B.cs"), 3));
        model.Code.AuditActions.Add(new CodeAuditAction("VoidOrder", "cancel-order", Path.Combine(root, "Audit.cs"), 3));
        model.Code.DomainTypes.Add(new DomainType("IOrderRepository", "Library.Domain", Path.Combine(root, "IOrderRepository.cs"), 3, true));

        return model;
    }

    /// <summary>A model triggering screen, setting, constituent and record findings without readers or files.</summary>
    static ProjectModel MixedModel(string root)
    {
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            """
            {"owners": ["lending-team"], "size": "large",
             "paths": {"frontends": [{"name": "web", "path": "apps/web", "platform": "svelte"}]},
             "stackConfig": {"dotnet": {"domain": "src/Domain", "sharedKernel": "src/Domain/Shared"}}}
            """);
        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };

        model.Records.Add(Rec("entry-point", "entry-point/app", Path.Combine(root, "app.md"), """{"status": "reviewed", "kind": "browser"}"""));
        model.Records.Add(Rec("use-case", "use-case/lending.list-loans", Path.Combine(root, "list.md"), """{"status": "reviewed", "kind": "query", "reachedThrough": ["entry-point/app"], "serves": ["journey/walk#s1"]}"""));
        model.Records.Add(Rec("journey", "journey/walk", Path.Combine(root, "walk.md"), """{"status": "reviewed", "actor": ["actor/employee"], "steps": [{"id": "s1", "useCase": "use-case/lending.list-loans"}]}"""));
        model.Records.Add(Rec("result", "result/list-loans.refused", Path.Combine(root, "list.md"), """{"status": "reviewed", "outcome": "refusal", "meaning": "The listing is refused.", "code": "lending.refused"}"""));
        model.Records.Add(Rec("scenario", "scenario/list-loans.listing-succeeds", Path.Combine(root, "list.md"), """{"status": "reviewed", "given": "A loan.", "when": "The employee lists loans.", "then": "Both are listed.", "result": "result/list-loans.listed"}"""));
        model.Records.Add(Rec("result", "result/list-loans.listed", Path.Combine(root, "list.md"), """{"status": "reviewed", "outcome": "success", "meaning": "The loans are listed."}"""));
        model.Records.Add(Rec("screen", "screen/app.loans", Path.Combine(root, "loans.md"), """{"status": "reviewed", "entryPoint": ["entry-point/app"], "offers": ["use-case/lending.list-loans"], "serves": []}"""));
        model.Records.Add(Rec("setting", "setting/lending.loan-limit", Path.Combine(root, "limit.md"), """{"status": "reviewed", "setBy": "operator", "valueType": "integer"}"""));
        model.Records.Add(Rec("setting", "setting/lending.hold-window", Path.Combine(root, "window.md"), """{"status": "reviewed", "setBy": "operator", "valueType": "integer", "key": "Lending:HoldWindow"}"""));
        model.Records.Add(Rec("requirement", "requirement/performance.list-loans-latency", Path.Combine(root, "latency.md"), """{"status": "reviewed", "kind": "quality", "statement": "Listing loans answers fast.", "measure": "p99 under 300 ms."}"""));
        model.Records.Add(Rec(
            "decision", "decision/0002-store-loans-as-documents",
            Path.Combine(root, "docs", "implementation", "decisions", "0002-store-loans-as-documents.md"),
            """{"status": "draft", "decider": "lending-team", "decideBy": "2030-01-31", "admits": [{"package": "nuget:UnknownLib", "layer": "Infrastructure"}]}"""));

        var routeFile = Path.Combine(root, "apps", "web", "src", "routes", "loans", "+page.svelte");
        model.Code.Elements.Add(new MarkedElement(CodeKind.FrontendRoute, "/loans", null, routeFile, 3, [], InTests: false));
        model.Screens.Add(new ScreenInfo("web", "/loans", "apps/web/src/routes/loans/+page.svelte", [], ["use-case/lending.list-loans"], "screen/app.loans"));
        model.BrowserTests.Add(new BrowserTestInfo("web", "first", Path.Combine(root, "first.spec.ts"), 3, ["scenario/list-loans.listing-succeeds"], null, ["/elsewhere"], [], []));
        model.BrowserTests.Add(new BrowserTestInfo("web", "second", Path.Combine(root, "second.spec.ts"), 3, ["scenario/list-loans.listing-succeeds"], null, ["/elsewhere"], [], []));
        model.Code.DomainTypes.Add(new DomainType("CopyTitle", "Library.Domain.Lending.Copies", Path.Combine(root, "src", "Domain", "Lending", "Copies", "CopyTitle.cs"), 3, false));

        return model;
    }

    static Record Rec(string type, string id, string file, string json)
    {
        using var document = JsonDocument.Parse(json);
        var raw = document.RootElement.Clone();

        return new Record
        {
            Type = type,
            Id = id,
            Title = id,
            Owner = "lending-team",
            Status = "reviewed",
            File = file,
            Line = 1,
            Raw = raw,
        };
    }
}
