using Record = LiteSpec.Model.Record;
using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the behavior-unrecorded check against hand-built models.</summary>
public sealed class BehaviorUnrecordedTests
{
    /// <summary>Handler Without A Mark Reports Mark Missing.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void HandlerWithoutAMarkReportsMarkMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.Handler, "GhostHandler", null,
            Path.Combine(temp.Path, "GhostHandler.cs"), 8, [], InTests: false));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("behavior-unrecorded", codes);
        Assert.Contains("mark-missing", codes);
    }

    /// <summary>Handler With A Record Passes.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void HandlerWithARecordPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.ghost", Path.Combine(temp.Path, "ghost.yaml"), """{"kind": "command"}"""));
        model.Code.Handlers.Add(new HandlerInfo(
            "use-case/lending.ghost", "lending", "ghost", "Ghost", "command",
            Path.Combine(temp.Path, "GhostHandler.cs")));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <summary>Endpoint Without A Mark Reports Mark Missing.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void EndpointWithoutAMarkReportsMarkMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.Endpoint, "GhostEndpoint", null,
            Path.Combine(temp.Path, "GhostEndpoint.cs"), 5, [], InTests: false));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("behavior-unrecorded", codes);
        Assert.Contains("mark-missing", codes);
    }

    /// <summary>Event Handler Without A Mark Reports Mark Missing.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void EventHandlerWithoutAMarkReportsMarkMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.EventHandler, "GhostReaction", null,
            Path.Combine(temp.Path, "GhostReaction.cs"), 3, [], InTests: false));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("behavior-unrecorded", codes);
        Assert.Contains("mark-missing", codes);
    }

    /// <summary>Mark Naming No Record Reports Mark Unresolved.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void MarkNamingNoRecordReportsMarkUnresolved()
    {
        using var temp = TestCli.Temp("checks");
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

    /// <summary>Job Without A Mark Names The Job.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void JobWithoutAMarkNamesTheJob()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.Job, "NightlyJob", null,
            Path.Combine(temp.Path, "NightlyJob.cs"), 5, [], InTests: false));

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "mark-missing").Message;
        Assert.Contains("Job 'NightlyJob'", message);
    }

    /// <summary>Reaction With A Matching Tag Passes.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void ReactionWithAMatchingTagPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.ghost", Path.Combine(temp.Path, "ghost.yaml"), """{"kind": "command"}"""));
        model.Code.Reactions.Add(new ReactionInfo(
            "GhostReaction", Path.Combine(temp.Path, "GhostReaction.cs"), 3, "use-case/lending.ghost"));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <summary>Domain Event Without A Mark Reports Mark Missing.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void DomainEventWithoutAMarkReportsMarkMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.DomainEvent, "GhostEvent", null,
            Path.Combine(temp.Path, "GhostEvent.cs"), 3, [], InTests: false));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("behavior-unrecorded", codes);
        Assert.Contains("mark-missing", codes);
    }

    /// <summary>Event With A Record Passes.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void EventWithARecordPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("event", "event/ghost.raised", Path.Combine(temp.Path, "ghost.yaml"), """{"meaning": "A ghost was raised.", "carries": []}"""));
        model.Code.Events.Add(new DomainEventInfo(
            "GhostRaisedEvent", Path.Combine(temp.Path, "GhostRaisedEvent.cs"), 3, "event/ghost.raised"));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <summary>Surplus Literal Reports Behavior Unrecorded.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void SurplusLiteralReportsBehaviorUnrecorded()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Literals.Add(new CodeLiteral(
            "lending.ghost-refusal", Path.Combine(temp.Path, "GhostHandler.cs"), 8, "GhostHandler"));

        Assert.Contains("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <summary>Rule Refusal Code Declares A Guard.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void RuleRefusalCodeDeclaresAGuard()
    {
        using var temp = TestCli.Temp("checks");
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

    /// <summary>Declared Literal Passes.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void DeclaredLiteralPasses()
    {
        using var temp = TestCli.Temp("checks");
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

    /// <summary>Screen Call Without An Endpoint Reports Behavior Unrecorded.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void ScreenCallWithoutAnEndpointReportsBehaviorUnrecorded()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Screens.Add(new ScreenInfo(
            "catalog",
            "/ghost",
            "apps/catalog/src/routes/ghost/+page.svelte",
            [new GeneratedOperation("ghostOp", "POST", "/api/lending/ghost-op")],
            []));

        Assert.Contains("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <summary>Screen Call Matching A Route Passes.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void ScreenCallMatchingARoutePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Records.Add(TestModel.Record("use-case", "use-case/lending.ghost", Path.Combine(temp.Path, "ghost.yaml"), "{}"));
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

    /// <summary>Screen Call Matching An Operation Name Passes.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void ScreenCallMatchingAnOperationNamePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Records.Add(TestModel.Record("use-case", "use-case/lending.ghost", Path.Combine(temp.Path, "ghost.yaml"), "{}"));
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

    /// <summary>Screen Calls Are Skipped For Client Only Shape.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void ScreenCallsAreSkippedForClientOnlyShape()
    {
        using var temp = TestCli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.yaml"), """{"shape": "client-only"}""");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Screens.Add(new ScreenInfo(
            "catalog",
            "/ghost",
            "apps/catalog/src/routes/ghost/+page.svelte",
            [new GeneratedOperation("ghostOp", "POST", "/api/lending/ghost-op")],
            []));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }
    /// <summary>Departure Holds The Findings On The Paths It Names.</summary>
    /// <proves>litespec/core/agent#apply-guidance-precedence</proves>
    [Fact]
    public void DepartureHoldsTheFindingsOnThePathsItNames()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("departure", "departure/ghost-route", Path.Combine(temp.Path, "ghost-route.yaml"), """{"covers": ["mark-missing"], "paths": ["src/GhostReaction.cs"], "reason": "Infrastructure.", "exit": "The gap closes.", "expiresOn": "2999-01-01"}"""));
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.EventHandler, "GhostReaction", null,
            Path.Combine(temp.Path, "src", "GhostReaction.cs"), 3, [], InTests: false));

        var (findings, known) = Checker.Check(model);

        Assert.DoesNotContain(findings, finding => finding.Code == "mark-missing");
        Assert.Contains(known, finding => finding.Code == "mark-missing");
    }

    /// <summary>Departure Past Its Expires On Holds Nothing.</summary>
    /// <proves>litespec/core/agent#apply-guidance-precedence</proves>
    [Fact]
    public void DeparturePastItsExpiresOnHoldsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("departure", "departure/ghost-route", Path.Combine(temp.Path, "ghost-route.yaml"), """{"covers": ["mark-missing"], "paths": ["src/GhostReaction.cs"], "reason": "Infrastructure.", "exit": "The gap closes.", "expiresOn": "2000-01-01"}"""));
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.EventHandler, "GhostReaction", null,
            Path.Combine(temp.Path, "src", "GhostReaction.cs"), 3, [], InTests: false));

        Assert.Contains("mark-missing", TestModel.Codes(model));
    }
}
