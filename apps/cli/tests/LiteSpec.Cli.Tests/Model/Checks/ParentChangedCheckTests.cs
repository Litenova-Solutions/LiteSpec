using LiteSpec.Model;
using LiteSpec.Tests.Support;
using Record = LiteSpec.Model.Record;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves a record citing, fulfilling or serving a changed record is flagged until it is updated.</summary>
public sealed class ParentChangedCheckTests
{
    /// <proves>litespec/core/model#flag-every-record-whose-parent-changed</proves>
    [Fact]
    public void Approved_Record_Citing_Changed_Record_Reports_At_Tier_G()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ChangedProblem(temp.Path, changed: true),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "cites": ["problem/overdue-loans-pile-up"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "parent-changed");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Record 'requirement/borrowing.max-active-loans' cites 'problem/overdue-loans-pile-up', which changed after approval.", finding.Message);
        Assert.Equal("requirement/borrowing.max-active-loans", finding.Subject);
    }

    /// <proves>litespec/core/model#flag-every-record-whose-parent-changed</proves>
    [Fact]
    public void Draft_Record_Citing_Changed_Record_Reports_At_Tier_A()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ChangedProblem(temp.Path, changed: true),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "kind": "functional", "statement": "A reader holds at most five active loans.", "cites": ["problem/overdue-loans-pile-up"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "parent-changed");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Record 'requirement/borrowing.max-active-loans' cites 'problem/overdue-loans-pile-up', which changed after approval.", finding.Message);
        Assert.Equal("requirement/borrowing.max-active-loans", finding.Subject);
    }

    /// <proves>litespec/core/model#flag-every-record-whose-parent-changed</proves>
    [Fact]
    public void Record_Citing_Unchanged_Record_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ChangedProblem(temp.Path, changed: false),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "cites": ["problem/overdue-loans-pile-up"]}"""));

        Assert.DoesNotContain("parent-changed", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#flag-every-record-whose-parent-changed</proves>
    [Fact]
    public void Rule_Fulfilling_Changed_Requirement_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ChangedRequirement(temp.Path),
            TestModel.Record("rule", "rule/lending.allow-renewal", Path.Combine(temp.Path, "lending.md"), """{"fulfills": ["requirement/borrowing.max-active-loans"], "issues": ["event/lending.renewal-allowed"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "parent-changed");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Record 'rule/lending.allow-renewal' fulfills 'requirement/borrowing.max-active-loans', which changed after approval.", finding.Message);
        Assert.Equal("rule/lending.allow-renewal", finding.Subject);
    }

    /// <proves>litespec/core/model#flag-every-record-whose-parent-changed</proves>
    [Fact]
    public void Journey_Serving_Changed_Goal_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ChangedGoal(temp.Path),
            TestModel.Record("journey", "journey/borrow-and-return", Path.Combine(temp.Path, "borrow.md"), """{"actor": ["actor/reader"], "serves": ["goal/faster-pickup"], "steps": [{"id": "s1", "statement": "The reader borrows the copy."}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "parent-changed");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Record 'journey/borrow-and-return' serves 'goal/faster-pickup', which changed after approval.", finding.Message);
        Assert.Equal("journey/borrow-and-return", finding.Subject);
    }

    /// <proves>litespec/core/model#flag-every-record-whose-parent-changed</proves>
    [Fact]
    public void Changed_Record_Without_Children_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ChangedProblem(temp.Path, changed: true));

        Assert.DoesNotContain("parent-changed", TestModel.Codes(model));
    }

    /// <summary>A problem whose approved hash matches its fields, or misses when changed.</summary>
    static Record ChangedProblem(string root, bool changed) =>
        Stamped(
            TestModel.Record("problem", "problem/overdue-loans-pile-up", Path.Combine(root, "problems.md"), """{"status": "approved", "affects": ["actor/reader"], "statement": "Overdue loans pile up on the shelves."}"""),
            changed);

    /// <summary>A requirement whose approved hash misses its fields.</summary>
    static Record ChangedRequirement(string root) =>
        Stamped(
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(root, "borrowing.md"), """{"status": "approved", "kind": "functional", "statement": "A reader holds at most five active loans."}"""),
            changed: true);

    /// <summary>A goal whose approved hash misses its fields.</summary>
    static Record ChangedGoal(string root) =>
        Stamped(
            TestModel.Record("goal", "goal/faster-pickup", Path.Combine(root, "goals.md"), """{"status": "approved", "statement": "Readers pick up holds faster.", "measure": "Median pickup wait.", "target": "Two days.", "dueBy": "2027-01-01"}"""),
            changed: true);

    /// <summary>The record with its approved hash stamped, flipped when the record changed.</summary>
    static Record Stamped(Record probe, bool changed)
    {
        var hash = Checker.SemanticHash(probe);
        var stamped = changed ? (hash[0] == '0' ? '1' : '0') + hash[1..] : hash;

        return TestModel.Record(
            probe.Type,
            probe.Id,
            probe.File,
            probe.Line,
            probe.Raw.GetRawText().TrimEnd('}') + $", \"approvedHash\": \"{stamped}\"}}");
    }
}
