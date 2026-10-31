using Record = LiteSpec.Model.Record;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the behavior-unrecorded check against hand-built models.</summary>
public sealed class BehaviorUnrecordedTests
{
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Handler_Without_A_Mark_Reports_Mark_Missing()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.Handler, "GhostHandler", null,
            Path.Combine(temp.Path, "GhostHandler.cs"), 8, [], InTests: false));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("behavior-unrecorded", codes);
        Assert.Contains("mark-missing", codes);
    }

    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Handler_With_A_Record_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.ghost", Path.Combine(temp.Path, "ghost.md"), """{"kind": "command"}"""));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.ghost", "lending", "ghost", "Ghost", "command",
            Path.Combine(temp.Path, "GhostHandler.cs")));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Endpoint_Without_A_Mark_Reports_Mark_Missing()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.Endpoint, "GhostEndpoint", null,
            Path.Combine(temp.Path, "GhostEndpoint.cs"), 5, [], InTests: false));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("behavior-unrecorded", codes);
        Assert.Contains("mark-missing", codes);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Event_Handler_Without_A_Mark_Reports_Mark_Missing()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.EventHandler, "GhostReaction", null,
            Path.Combine(temp.Path, "GhostReaction.cs"), 3, [], InTests: false));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("behavior-unrecorded", codes);
        Assert.Contains("mark-missing", codes);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Mark_Naming_No_Record_Reports_Mark_Unresolved()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var file = Path.Combine(temp.Path, "GhostReaction.cs");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.EventHandler, "GhostReaction", null, file, 3,
            [new CodeMark("reaction/lending.nope", MarkStyle.Xml, MarkVerb.Implements, file, 2)],
            InTests: false));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("behavior-unrecorded", codes);
        Assert.Contains("mark-unknown", codes);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Job_Without_A_Mark_Names_The_Job()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.Job, "NightlyJob", null,
            Path.Combine(temp.Path, "NightlyJob.cs"), 5, [], InTests: false));

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "mark-missing").Message;
        Assert.Contains("Job 'NightlyJob'", message);
    }

    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Reaction_With_A_Matching_Tag_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.ghost", Path.Combine(temp.Path, "ghost.md"), """{"kind": "command"}"""));
        model.Code.Reactions.Add(new ReactionInfo(
            "GhostReaction", Path.Combine(temp.Path, "GhostReaction.cs"), 3, "use-case/lending.ghost"));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Domain_Event_Without_A_Mark_Reports_Mark_Missing()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.DomainEvent, "GhostEvent", null,
            Path.Combine(temp.Path, "GhostEvent.cs"), 3, [], InTests: false));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("behavior-unrecorded", codes);
        Assert.Contains("mark-missing", codes);
    }

    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Event_With_A_Record_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("event", "event/ghost.raised", Path.Combine(temp.Path, "ghost.md"), """{"meaning": "A ghost was raised.", "carries": []}"""));
        model.Code.Events.Add(new DomainEventInfo(
            "GhostRaisedEvent", Path.Combine(temp.Path, "GhostRaisedEvent.cs"), 3, "event/ghost.raised"));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Surplus_Literal_Reports_Behavior_Unrecorded()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Literals.Add(new CodeLiteral(
            "lending.ghost-refusal", Path.Combine(temp.Path, "GhostHandler.cs"), 8, "GhostHandler"));

        Assert.Contains("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Rule_Refusal_Code_Declares_A_Guard()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "rule",
                "rule/loan.due-date-required",
                Path.Combine(temp.Path, "loan.md"),
                """{"kind": "invariant", "statement": "A loan has a due date.", "refusal": "lending.due-date-required"}"""));
        model.Code.Literals.Add(new CodeLiteral(
            "lending.due-date-required", Path.Combine(temp.Path, "Loan.cs"), 8, "Loan"));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Declared_Literal_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "result",
                "result/ghost.refused",
                Path.Combine(temp.Path, "ghost.md"),
                """{"outcome": "refusal", "code": "lending.ghost-refusal", "meaning": "The ghost refused."}"""));
        model.Code.Literals.Add(new CodeLiteral(
            "lending.ghost-refusal", Path.Combine(temp.Path, "GhostHandler.cs"), 8, "GhostHandler"));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Screen_Call_Without_An_Endpoint_Reports_Behavior_Unrecorded()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Screens.Add(new ScreenInfo(
            "catalog",
            "/ghost",
            "apps/catalog/src/routes/ghost/+page.svelte",
            [new GeneratedOperation("ghostOp", "POST", "/api/lending/ghost-op")],
            []));

        Assert.Contains("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Screen_Call_Matching_A_Route_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Records.Add(TestModel.Record("use-case", "use-case/lending.ghost", Path.Combine(temp.Path, "ghost.md"), "{}"));
        model.Code.Endpoints.Add(new EndpointOperation(
            "", "use-case/lending.ghost", "POST", "/api/lending/ghost-op",
            Path.Combine(temp.Path, "GhostEndpoint.cs")));
        model.Screens.Add(new ScreenInfo(
            "catalog",
            "/ghost",
            "apps/catalog/src/routes/ghost/+page.svelte",
            [new GeneratedOperation("ghostOp", "POST", "/api/lending/ghost-op")],
            []));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Screen_Call_Matching_An_Operation_Name_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Records.Add(TestModel.Record("use-case", "use-case/lending.ghost", Path.Combine(temp.Path, "ghost.md"), "{}"));
        model.Code.Endpoints.Add(new EndpointOperation(
            "GhostOp", "use-case/lending.ghost", "", "",
            Path.Combine(temp.Path, "GhostEndpoint.cs")));
        model.Screens.Add(new ScreenInfo(
            "catalog",
            "/ghost",
            "apps/catalog/src/routes/ghost/+page.svelte",
            [new GeneratedOperation("ghostOp", "POST", "/elsewhere")],
            []));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Screen_Calls_Are_Skipped_For_Client_Only_Shape()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), """{"shape": "client-only"}""");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Screens.Add(new ScreenInfo(
            "catalog",
            "/ghost",
            "apps/catalog/src/routes/ghost/+page.svelte",
            [new GeneratedOperation("ghostOp", "POST", "/api/lending/ghost-op")],
            []));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }
    /// <proves>litespec/core/model#apply-guidance-precedence</proves>
    [Fact]
    public void Approved_Exception_Holds_The_Findings_On_The_Paths_It_Names()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("exception", "exception/ghost-route", Path.Combine(temp.Path, "ghost-route.md"), """{"status": "approved", "kind": "gap", "covers": ["mark-missing"], "paths": ["src/GhostReaction.cs"], "reason": "Infrastructure.", "expiresOn": "2999-01-01"}"""));
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.EventHandler, "GhostReaction", null,
            Path.Combine(temp.Path, "src", "GhostReaction.cs"), 3, [], InTests: false));

        var (findings, known) = Checker.Check(model);

        Assert.DoesNotContain(findings, finding => finding.Code == "mark-missing");
        Assert.Contains(known, finding => finding.Code == "mark-missing");
    }

    /// <proves>litespec/core/model#apply-guidance-precedence</proves>
    [Fact]
    public void Exception_Past_Its_Expires_On_Holds_Nothing()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("exception", "exception/ghost-route", Path.Combine(temp.Path, "ghost-route.md"), """{"status": "approved", "kind": "gap", "covers": ["mark-missing"], "paths": ["src/GhostReaction.cs"], "reason": "Infrastructure.", "expiresOn": "2000-01-01"}"""));
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.EventHandler, "GhostReaction", null,
            Path.Combine(temp.Path, "src", "GhostReaction.cs"), 3, [], InTests: false));

        Assert.Contains("mark-missing", TestModel.Codes(model));
    }
}
