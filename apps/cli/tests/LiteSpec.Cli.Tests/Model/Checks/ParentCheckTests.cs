using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the parent check: every record with a required written parent names it, at tier G.</summary>
public sealed class ParentCheckTests
{
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Requirement_With_BasedOn_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "basedOn": ["constraint/library-keeps-records"]}"""),
            TestModel.Record("constraint", "constraint/library-keeps-records", Path.Combine(temp.Path, "charter.md"), """{"authority": "Library charter.", "kind": "organizational", "applies": true, "appliesBecause": "The library keeps records."}"""));

        Assert.DoesNotContain("record-without-parent", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Requirement_With_Deciding_Decision_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""),
            TestModel.Record("decision", "decision/0001-loan-period", Path.Combine(temp.Path, "decisions.md"), """{"status": "approved", "decidedOn": "2026-02-01", "options": [{"name": "Five loans", "for": "Shelves stay full.", "against": "Readers queue."}], "chosen": "Five loans", "changeWhen": "Holds pass one hundred a day.", "decides": ["requirement/borrowing.max-active-loans"]}"""));

        Assert.DoesNotContain("record-without-parent", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Requirement_Without_Origin_Reports_At_Tier_G()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Requirement 'requirement/borrowing.max-active-loans' names no parent: it names no feature, basedOn or deciding decision.", finding.Message);
        Assert.Equal("requirement/borrowing.max-active-loans", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Draft_Requirement_Without_Origin_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "kind": "functional", "statement": "A reader holds at most five active loans."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Requirement 'requirement/borrowing.max-active-loans' names no parent: it names no feature, basedOn or deciding decision.", finding.Message);
        Assert.Equal("requirement/borrowing.max-active-loans", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Requirement_Naming_Draft_Feature_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "features": ["feature/hold-queue"]}"""),
            TestModel.Record("feature", "feature/hold-queue", Path.Combine(temp.Path, "holds.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "lifecycle": "planned", "plannedFor": "Pilot launch", "solves": ["problem/overdue-loans-pile-up"]}"""));

        Assert.DoesNotContain("record-without-parent", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Requirement_With_Features_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "features": ["feature/hold-queue"]}"""),
            TestModel.Record("feature", "feature/hold-queue", Path.Combine(temp.Path, "holds.md"), """{"status": "approved", "lifecycle": "shipped", "solves": ["problem/overdue-loans-pile-up"]}"""));

        Assert.DoesNotContain("record-without-parent", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Requirement_With_Dangling_Features_Stays_Silent()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "features": ["feature/no-such-feature"]}"""));

        Assert.DoesNotContain("record-without-parent", TestModel.Codes(model));
        Assert.Contains("reference-unresolved", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Refused_Feature_With_Solves_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/hold-queue", Path.Combine(temp.Path, "holds.md"), """{"status": "approved", "lifecycle": "refused", "solves": ["problem/overdue-loans-pile-up"], "refusedBecause": "The desk handles holds by hand."}"""));

        Assert.DoesNotContain("record-without-parent", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Refused_Feature_Without_Solves_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/hold-queue", Path.Combine(temp.Path, "holds.md"), """{"status": "approved", "lifecycle": "refused", "refusedBecause": "The desk handles holds by hand."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Feature 'feature/hold-queue' names no parent: 'solves' names no problem.", finding.Message);
        Assert.Equal("feature/hold-queue", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Journey_Serving_Goal_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("journey", "journey/borrow-and-return", Path.Combine(temp.Path, "borrow.md"), """{"actor": ["actor/reader"], "aim": "Borrow and return a copy.", "serves": ["goal/faster-pickup"], "steps": [{"id": "s1", "says": "The reader borrows the copy."}]}"""),
            TestModel.Record("goal", "goal/faster-pickup", Path.Combine(temp.Path, "goals.md"), """{"statement": "Readers wait less.", "measure": "Median pickup wait.", "target": "Under two days.", "dueBy": "Pilot launch"}"""));

        Assert.DoesNotContain("record-without-parent", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Journey_Serving_Step_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("journey", "journey/borrow-and-return", Path.Combine(temp.Path, "borrow.md"), """{"actor": ["actor/reader"], "aim": "Borrow and return a copy.", "serves": ["journey/renew-loan#s1"], "steps": [{"id": "s1", "says": "The reader borrows the copy."}]}"""),
            TestModel.Record("journey", "journey/renew-loan", Path.Combine(temp.Path, "renew.md"), """{"actor": ["actor/reader"], "aim": "Renew a loan.", "serves": ["goal/faster-pickup"], "steps": [{"id": "s1", "says": "The reader renews the loan."}]}"""),
            TestModel.Record("goal", "goal/faster-pickup", Path.Combine(temp.Path, "goals.md"), """{"statement": "Readers wait less.", "measure": "Median pickup wait.", "target": "Under two days.", "dueBy": "Pilot launch"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Journey 'journey/borrow-and-return' names no parent: 'serves' names no goal.", finding.Message);
        Assert.Equal("journey/borrow-and-return", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Journey_Without_Serves_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("journey", "journey/borrow-and-return", Path.Combine(temp.Path, "borrow.md"), """{"actor": ["actor/reader"], "aim": "Borrow and return a copy.", "steps": [{"id": "s1", "says": "The reader borrows the copy."}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Journey 'journey/borrow-and-return' names no parent: 'serves' names no goal.", finding.Message);
        Assert.Equal("journey/borrow-and-return", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Journey_With_Dangling_Serves_Stays_Silent()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("journey", "journey/borrow-and-return", Path.Combine(temp.Path, "borrow.md"), """{"actor": ["actor/reader"], "aim": "Borrow and return a copy.", "serves": ["goal/no-such-goal"], "steps": [{"id": "s1", "says": "The reader borrows the copy."}]}"""));

        Assert.DoesNotContain("record-without-parent", TestModel.Codes(model));
        Assert.Contains("reference-unresolved", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Scenario_With_Result_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, "lending.md"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/borrow-copy.borrowed", Path.Combine(temp.Path, "borrow.md"), """{"outcome": "success", "meaning": "The copy is lent."}"""),
            TestModel.Record("scenario", "scenario/borrow-copy.borrowing-an-available-copy-succeeds", Path.Combine(temp.Path, "borrow.md"), """{"given": "A copy sits available on its shelf.", "when": "The reader borrows the copy.", "then": "The loan is active.", "result": "result/borrow-copy.borrowed"}"""));

        Assert.DoesNotContain("record-without-parent", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Scenario_Without_Result_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, "lending.md"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"kind": "command"}"""),
            TestModel.Record("scenario", "scenario/borrow-copy.borrowing-an-available-copy-succeeds", Path.Combine(temp.Path, "borrow.md"), """{"given": "A copy sits available on its shelf.", "when": "The reader borrows the copy.", "then": "The loan is active."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Scenario 'scenario/borrow-copy.borrowing-an-available-copy-succeeds' names no parent: 'result' names no result.", finding.Message);
        Assert.Equal("scenario/borrow-copy.borrowing-an-available-copy-succeeds", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Scenario_With_Missing_Home_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("scenario", "scenario/no-such-case.filing-succeeds", Path.Combine(temp.Path, "file.md"), """{"given": "A clerk holds a filed copy.", "when": "The clerk files the copy.", "then": "The copy is filed.", "result": "result/no-such-case.filed"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Scenario 'scenario/no-such-case.filing-succeeds' names no parent: its home 'no-such-case' names no use case.", finding.Message);
        Assert.Equal("scenario/no-such-case.filing-succeeds", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Result_With_Home_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, "lending.md"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/borrow-copy.borrowed", Path.Combine(temp.Path, "borrow.md"), """{"outcome": "success", "meaning": "The copy is lent."}"""));

        Assert.DoesNotContain("record-without-parent", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Result_With_Missing_Home_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/no-such-case.filed", Path.Combine(temp.Path, "file.md"), """{"outcome": "success", "meaning": "The copy is filed."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Result 'result/no-such-case.filed' names no parent: its home 'no-such-case' names no use case.", finding.Message);
        Assert.Equal("result/no-such-case.filed", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Screen_With_Agreeing_EntryPoint_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("screen", "screen/library-app.loans", Path.Combine(temp.Path, "loans.md"), """{"entryPoint": "entry-point/library-app", "address": "/loans"}"""),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.md"), """{"kind": "browser"}"""));

        Assert.DoesNotContain("record-without-parent", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Screen_Without_EntryPoint_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("screen", "screen/library-app.loans", Path.Combine(temp.Path, "loans.md"), """{"address": "/loans"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Screen 'screen/library-app.loans' names no parent: 'entryPoint' names no entry point.", finding.Message);
        Assert.Equal("screen/library-app.loans", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Screen_With_Disagreeing_EntryPoint_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("screen", "screen/library-app.loans", Path.Combine(temp.Path, "loans.md"), """{"entryPoint": ["entry-point/kiosk"], "address": "/loans"}"""),
            TestModel.Record("entry-point", "entry-point/kiosk", Path.Combine(temp.Path, "entry.md"), """{"kind": "device"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Screen 'screen/library-app.loans' names no parent: 'entryPoint' 'entry-point/kiosk' disagrees with its home 'library-app'.", finding.Message);
        Assert.Equal("screen/library-app.loans", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Use_Case_With_Module_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, "lending.md"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"kind": "command"}"""));

        Assert.DoesNotContain("record-without-parent", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Use_Case_With_Missing_Module_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/nope.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"kind": "command"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Use case 'use-case/nope.borrow-copy' names no parent: its home 'nope' names no module.", finding.Message);
        Assert.Equal("use-case/nope.borrow-copy", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Aggregate_With_Missing_Module_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/nope.loan", Path.Combine(temp.Path, "loan.md"), """{"states": [{"name": "Active", "meaning": "The loan is active."}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Aggregate 'aggregate/nope.loan' names no parent: its home 'nope' names no module.", finding.Message);
        Assert.Equal("aggregate/nope.loan", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Rule_With_Aggregate_Home_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, "lending.md"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.md"), """{"states": [{"name": "Active", "meaning": "The loan is active."}]}"""),
            TestModel.Record("rule", "rule/loan.loan-period", Path.Combine(temp.Path, "rules.md"), """{"kind": "invariant", "statement": "A loan lasts 21 days.", "governs": ["aggregate/lending.loan"]}"""));

        Assert.DoesNotContain("record-without-parent", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Rule_With_Missing_Home_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/nope.loan-period", Path.Combine(temp.Path, "rules.md"), """{"kind": "invariant", "statement": "A loan lasts 21 days.", "governs": ["aggregate/lending.loan"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Rule 'rule/nope.loan-period' names no parent: its home 'nope' names no module or aggregate.", finding.Message);
        Assert.Equal("rule/nope.loan-period", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Event_With_Aggregate_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, "lending.md"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.md"), """{"states": [{"name": "Active", "meaning": "The loan is active."}]}"""),
            TestModel.Record("event", "event/loan.loan-borrowed", Path.Combine(temp.Path, "loan.md"), """{"meaning": "The copy is lent.", "carries": []}"""));

        Assert.DoesNotContain("record-without-parent", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Event_With_Missing_Aggregate_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("event", "event/nope.loan-borrowed", Path.Combine(temp.Path, "loan.md"), """{"meaning": "The copy is lent.", "carries": []}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Event 'event/nope.loan-borrowed' names no parent: its home 'nope' names no aggregate.", finding.Message);
        Assert.Equal("event/nope.loan-borrowed", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Reaction_With_Missing_Module_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/nope.email-clerk-on-loan-borrowed", Path.Combine(temp.Path, "notify.md"), """{"effects": [{"issues": "use-case/lending.borrow-copy"}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Reaction 'reaction/nope.email-clerk-on-loan-borrowed' names no parent: its home 'nope' names no module.", finding.Message);
        Assert.Equal("reaction/nope.email-clerk-on-loan-borrowed", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Job_With_Missing_Module_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/nope.scan-due-loans", Path.Combine(temp.Path, "scan.md"), """{}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Job 'job/nope.scan-due-loans' names no parent: its home 'nope' names no module.", finding.Message);
        Assert.Equal("job/nope.scan-due-loans", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Notification_With_Missing_Module_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", "notification/nope.loan-due", Path.Combine(temp.Path, "notify.md"), """{"template": "loan-due"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Notification 'notification/nope.loan-due' names no parent: its home 'nope' names no module.", finding.Message);
        Assert.Equal("notification/nope.loan-due", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Setting_With_Missing_Module_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("setting", "setting/nope.max-renewals", Path.Combine(temp.Path, "settings.md"), """{}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Setting 'setting/nope.max-renewals' names no parent: its home 'nope' names no module.", finding.Message);
        Assert.Equal("setting/nope.max-renewals", finding.Subject);
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Retired_Record_Without_Parent_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"status": "retired", "kind": "functional", "statement": "A reader holds at most five active loans."}"""));

        Assert.DoesNotContain("record-without-parent", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Root_Type_Without_Parent_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("goal", "goal/faster-pickup", Path.Combine(temp.Path, "goals.md"), """{"statement": "Readers wait less.", "measure": "Median pickup wait.", "target": "Under two days.", "dueBy": "Pilot launch"}"""));

        Assert.DoesNotContain("record-without-parent", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Result_Without_Home_Reports_Parent()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/borrowed", Path.Combine(temp.Path, "borrow.md"), """{"outcome": "success", "meaning": "The copy is lent."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Result 'result/borrowed' names no parent: its identifier names no home.", finding.Message);
        Assert.Equal("result/borrowed", finding.Subject);
        Assert.DoesNotContain("identifier-form", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Use_Case_With_Unparseable_Id_Reports_Parent()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow.copy", Path.Combine(temp.Path, "borrow.md"), """{"kind": "command"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-without-parent");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Use case 'use-case/lending.borrow.copy' names no parent: its identifier names no home.", finding.Message);
        Assert.Equal("use-case/lending.borrow.copy", finding.Subject);
        Assert.DoesNotContain("identifier-form", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Requirement_Without_Home_Stays_Identifier_Form()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "features": ["feature/hold-queue"]}"""),
            TestModel.Record("feature", "feature/hold-queue", Path.Combine(temp.Path, "holds.md"), """{"status": "approved", "lifecycle": "shipped", "solves": ["problem/overdue-loans-pile-up"]}"""));

        Assert.Contains("identifier-form", TestModel.Codes(model));
        Assert.DoesNotContain("record-without-parent", TestModel.Codes(model));
    }
}
