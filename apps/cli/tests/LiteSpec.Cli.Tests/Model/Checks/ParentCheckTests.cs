using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the parent check: every record with a required written parent names it, at tier G.</summary>
public sealed class ParentCheckTests
{
    /// <summary>Feature With Solves Passes.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void FeatureWithSolvesPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/hold-queue", Path.Combine(temp.Path, "holds.yaml"), """{"solves": ["problem/overdue-loans-pile-up"]}"""));

        Assert.DoesNotContain("record-parent-missing", TestModel.Codes(model));
    }

    /// <summary>Feature Without Solves Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void FeatureWithoutSolvesReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/hold-queue", Path.Combine(temp.Path, "holds.yaml"), """{}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Feature 'feature/hold-queue' has no parent and 'solves' names no problem.", finding.Message);
        Assert.Equal("feature/hold-queue", finding.Subject);
    }

    /// <summary>Journey Serving Goal Passes.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void JourneyServingGoalPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("journey", "journey/borrow-and-return", Path.Combine(temp.Path, "borrow.yaml"), """{"actor": ["actor/reader"], "serves": ["goal/faster-pickup"], "steps": [{"id": "s1", "statement": "The reader borrows the copy."}]}"""),
            TestModel.Record("goal", "goal/faster-pickup", Path.Combine(temp.Path, "goals.yaml"), """{"statement": "Readers wait less.", "measure": "Median pickup wait.", "target": "Under two days.", "dueBy": "Pilot launch"}"""));

        Assert.DoesNotContain("record-parent-missing", TestModel.Codes(model));
    }

    /// <summary>Journey Serving Step Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void JourneyServingStepReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("journey", "journey/borrow-and-return", Path.Combine(temp.Path, "borrow.yaml"), """{"actor": ["actor/reader"], "serves": ["journey/renew-loan#s1"], "steps": [{"id": "s1", "statement": "The reader borrows the copy."}]}"""),
            TestModel.Record("journey", "journey/renew-loan", Path.Combine(temp.Path, "renew.yaml"), """{"actor": ["actor/reader"], "serves": ["goal/faster-pickup"], "steps": [{"id": "s1", "statement": "The reader renews the loan."}]}"""),
            TestModel.Record("goal", "goal/faster-pickup", Path.Combine(temp.Path, "goals.yaml"), """{"statement": "Readers wait less.", "measure": "Median pickup wait.", "target": "Under two days.", "dueBy": "Pilot launch"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Journey 'journey/borrow-and-return' has no parent and 'serves' names no goal.", finding.Message);
        Assert.Equal("journey/borrow-and-return", finding.Subject);
    }

    /// <summary>Journey Without Serves Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void JourneyWithoutServesReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("journey", "journey/borrow-and-return", Path.Combine(temp.Path, "borrow.yaml"), """{"actor": ["actor/reader"], "steps": [{"id": "s1", "statement": "The reader borrows the copy."}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Journey 'journey/borrow-and-return' has no parent and 'serves' names no goal.", finding.Message);
        Assert.Equal("journey/borrow-and-return", finding.Subject);
    }

    /// <summary>Journey With Dangling Serves Stays Silent.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void JourneyWithDanglingServesStaysSilent()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("journey", "journey/borrow-and-return", Path.Combine(temp.Path, "borrow.yaml"), """{"actor": ["actor/reader"], "serves": ["goal/no-such-goal"], "steps": [{"id": "s1", "statement": "The reader borrows the copy."}]}"""));

        Assert.DoesNotContain("record-parent-missing", TestModel.Codes(model));
        Assert.Contains("reference-unknown", TestModel.Codes(model));
    }

    /// <summary>Scenario With Result Passes.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void ScenarioWithResultPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, "lending.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/borrow-copy.borrowed", Path.Combine(temp.Path, "borrow.yaml"), """{"outcome": "success", "meaning": "The copy is lent."}"""),
            TestModel.Record("scenario", "scenario/borrow-copy.borrowing-an-available-copy-succeeds", Path.Combine(temp.Path, "borrow.yaml"), """{"given": "A copy sits available on its shelf.", "when": "The reader borrows the copy.", "then": "The loan is active.", "result": "result/borrow-copy.borrowed"}"""));

        Assert.DoesNotContain("record-parent-missing", TestModel.Codes(model));
    }

    /// <summary>Scenario Without Result Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void ScenarioWithoutResultReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, "lending.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command"}"""),
            TestModel.Record("scenario", "scenario/borrow-copy.borrowing-an-available-copy-succeeds", Path.Combine(temp.Path, "borrow.yaml"), """{"given": "A copy sits available on its shelf.", "when": "The reader borrows the copy.", "then": "The loan is active."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Scenario 'scenario/borrow-copy.borrowing-an-available-copy-succeeds' has no parent and 'result' names no result.", finding.Message);
        Assert.Equal("scenario/borrow-copy.borrowing-an-available-copy-succeeds", finding.Subject);
    }

    /// <summary>Scenario With Missing Home Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void ScenarioWithMissingHomeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("scenario", "scenario/no-such-case.filing-succeeds", Path.Combine(temp.Path, "file.yaml"), """{"given": "A clerk holds a filed copy.", "when": "The clerk files the copy.", "then": "The copy is filed.", "result": "result/no-such-case.filed"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Scenario 'scenario/no-such-case.filing-succeeds' has no parent and its home 'no-such-case' names no use case.", finding.Message);
        Assert.Equal("scenario/no-such-case.filing-succeeds", finding.Subject);
    }

    /// <summary>Result With Home Passes.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void ResultWithHomePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, "lending.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/borrow-copy.borrowed", Path.Combine(temp.Path, "borrow.yaml"), """{"outcome": "success", "meaning": "The copy is lent."}"""));

        Assert.DoesNotContain("record-parent-missing", TestModel.Codes(model));
    }

    /// <summary>Result With Missing Home Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void ResultWithMissingHomeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/no-such-case.filed", Path.Combine(temp.Path, "file.yaml"), """{"outcome": "success", "meaning": "The copy is filed."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Result 'result/no-such-case.filed' has no parent and its home 'no-such-case' names no use case.", finding.Message);
        Assert.Equal("result/no-such-case.filed", finding.Subject);
    }

    /// <summary>Screen With Home Passes.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void ScreenWithHomePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("screen", "screen/library-app.loans", Path.Combine(temp.Path, "loans.yaml"), """{"address": "/loans"}"""),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser"}"""));

        Assert.DoesNotContain("record-parent-missing", TestModel.Codes(model));
    }

    /// <summary>Screen With Missing Home Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void ScreenWithMissingHomeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("screen", "screen/library-app.loans", Path.Combine(temp.Path, "loans.yaml"), """{"address": "/loans"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Screen 'screen/library-app.loans' has no parent and its home 'library-app' names no entry point.", finding.Message);
        Assert.Equal("screen/library-app.loans", finding.Subject);
    }

    /// <summary>Use Case With Module Passes.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void UseCaseWithModulePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, "lending.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command"}"""));

        Assert.DoesNotContain("record-parent-missing", TestModel.Codes(model));
    }

    /// <summary>Use Case With Missing Module Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void UseCaseWithMissingModuleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/nope.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Use case 'use-case/nope.borrow-copy' has no parent and its home 'nope' names no module.", finding.Message);
        Assert.Equal("use-case/nope.borrow-copy", finding.Subject);
    }

    /// <summary>Aggregate With Missing Module Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void AggregateWithMissingModuleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/nope.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Active", "meaning": "The loan is active."}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Aggregate 'aggregate/nope.loan' has no parent and its home 'nope' names no module.", finding.Message);
        Assert.Equal("aggregate/nope.loan", finding.Subject);
    }

    /// <summary>Rule With Aggregate Home Passes.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void RuleWithAggregateHomePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, "lending.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Active", "meaning": "The loan is active."}]}"""),
            TestModel.Record("rule", "rule/loan.loan-period", Path.Combine(temp.Path, "rules.yaml"), """{"kind": "invariant", "statement": "A loan lasts 21 days.", "governs": ["aggregate/lending.loan"]}"""));

        Assert.DoesNotContain("record-parent-missing", TestModel.Codes(model));
    }

    /// <summary>A product-wide rule whose home is the product passes.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void RuleWithProductHomePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("product", "product/library", Path.Combine(temp.Path, "product.yaml"), """{"summary": "The library lends copies."}"""),
            TestModel.Record("rule", "rule/library.every-act-is-audited", Path.Combine(temp.Path, "rules.yaml"), """{"kind": "invariant", "statement": "Every act names its actor."}"""));

        Assert.DoesNotContain("record-parent-missing", TestModel.Codes(model));
    }

    /// <summary>Rule With Missing Home Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void RuleWithMissingHomeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/nope.loan-period", Path.Combine(temp.Path, "rules.yaml"), """{"kind": "invariant", "statement": "A loan lasts 21 days.", "governs": ["aggregate/lending.loan"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Rule 'rule/nope.loan-period' has no parent and its home 'nope' names no module, aggregate or product.", finding.Message);
        Assert.Equal("rule/nope.loan-period", finding.Subject);
    }

    /// <summary>Event With Aggregate Passes.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void EventWithAggregatePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, "lending.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Active", "meaning": "The loan is active."}]}"""),
            TestModel.Record("event", "event/loan.loan-borrowed", Path.Combine(temp.Path, "loan.yaml"), """{"meaning": "The copy is lent.", "carries": []}"""));

        Assert.DoesNotContain("record-parent-missing", TestModel.Codes(model));
    }

    /// <summary>Event With Missing Aggregate Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void EventWithMissingAggregateReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("event", "event/nope.loan-borrowed", Path.Combine(temp.Path, "loan.yaml"), """{"meaning": "The copy is lent.", "carries": []}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Event 'event/nope.loan-borrowed' has no parent and its home 'nope' names no aggregate.", finding.Message);
        Assert.Equal("event/nope.loan-borrowed", finding.Subject);
    }

    /// <summary>Reaction With Missing Module Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void ReactionWithMissingModuleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/nope.email-clerk-on-loan-borrowed", Path.Combine(temp.Path, "notify.yaml"), """{"triggeredBy": ["event/loan.loan-borrowed"], "effects": [{"runs": "use-case/lending.borrow-copy"}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Reaction 'reaction/nope.email-clerk-on-loan-borrowed' has no parent and its home 'nope' names no module.", finding.Message);
        Assert.Equal("reaction/nope.email-clerk-on-loan-borrowed", finding.Subject);
    }

    /// <summary>Job With Missing Module Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void JobWithMissingModuleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/nope.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"kind": "scheduled", "schedule": {"every": "PT1H"}, "effects": [{"runs": "use-case/lending.mark-due"}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Job 'job/nope.scan-due' has no parent and its home 'nope' names no module.", finding.Message);
        Assert.Equal("job/nope.scan-due", finding.Subject);
    }

    /// <summary>Notification With Missing Module Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void NotificationWithMissingModuleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", "notification/nope.loan-due", Path.Combine(temp.Path, "notify.yaml"), """{"template": "loan-due"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Notification 'notification/nope.loan-due' has no parent and its home 'nope' names no module.", finding.Message);
        Assert.Equal("notification/nope.loan-due", finding.Subject);
    }

    /// <summary>Setting With Missing Module Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void SettingWithMissingModuleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("setting", "setting/nope.max-renewals", Path.Combine(temp.Path, "settings.yaml"), """{}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Setting 'setting/nope.max-renewals' has no parent and its home 'nope' names no module.", finding.Message);
        Assert.Equal("setting/nope.max-renewals", finding.Subject);
    }

    /// <summary>Root Type Without Parent Passes.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void RootTypeWithoutParentPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("goal", "goal/faster-pickup", Path.Combine(temp.Path, "goals.yaml"), """{"statement": "Readers wait less.", "measure": "Median pickup wait.", "target": "Under two days.", "dueBy": "Pilot launch"}"""));

        Assert.DoesNotContain("record-parent-missing", TestModel.Codes(model));
    }

    /// <summary>Result Without Home Reports Parent.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void ResultWithoutHomeReportsParent()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/borrowed", Path.Combine(temp.Path, "borrow.yaml"), """{"outcome": "success", "meaning": "The copy is lent."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Result 'result/borrowed' has no parent and its identifier names no home.", finding.Message);
        Assert.Equal("result/borrowed", finding.Subject);
        Assert.DoesNotContain("identifier-invalid", TestModel.Codes(model));
    }

    /// <summary>Use Case With Unparseable Id Reports Parent.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void UseCaseWithUnparseableIdReportsParent()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow.copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-parent-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Use case 'use-case/lending.borrow.copy' has no parent and its identifier names no home.", finding.Message);
        Assert.Equal("use-case/lending.borrow.copy", finding.Subject);
        Assert.DoesNotContain("identifier-invalid", TestModel.Codes(model));
    }

    /// <summary>Requirement Without Home Stays Identifier Form.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void RequirementWithoutHomeStaysIdentifierForm()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));

        Assert.Contains("identifier-invalid", TestModel.Codes(model));
        Assert.DoesNotContain("record-parent-missing", TestModel.Codes(model));
    }
}
