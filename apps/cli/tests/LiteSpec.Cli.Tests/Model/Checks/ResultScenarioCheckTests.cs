using LiteSpec.Model;
using LiteSpec.Tests.Support;
using System.Text.Json;
using Record = LiteSpec.Model.Record;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the result-scenario-missing check: a written result shown by no scenario, at tier G for a success and tier A for a refusal.</summary>
public sealed class ResultScenarioCheckTests
{
    /// <proves>litespec/core/model#give-every-use-case-its-results</proves>
    [Fact]
    public void Success_Result_Named_By_A_Scenario_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.md"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/submit-expense.submitted", Path.Combine(temp.Path, "submit.md"), """{"outcome": "success", "meaning": "The expense is submitted."}"""),
            TestModel.Record("scenario", "scenario/submit-expense.submitting-with-a-receipt-succeeds", Path.Combine(temp.Path, "submit.md"), """{"given": "An employee paid for a train ticket and kept the receipt.", "when": "The employee submits the expense with the receipt.", "then": "The expense is submitted.", "result": "result/submit-expense.submitted"}"""));

        Assert.DoesNotContain("result-scenario-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-use-case-its-results</proves>
    [Fact]
    public void Success_Result_With_No_Scenario_Reports_At_Tier_G()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.md"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/submit-expense.submitted", Path.Combine(temp.Path, "submit.md"), """{"outcome": "success", "meaning": "The expense is submitted."}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "result-scenario-missing").ToList();
        var finding = Assert.Single(findings);

        Assert.Equal("G", finding.Tier);
        Assert.Contains("result/submit-expense.submitted", finding.Message, StringComparison.Ordinal);
        Assert.Contains("is shown by no scenario", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/model#give-every-use-case-its-results</proves>
    [Fact]
    public void Refusal_Result_With_No_Scenario_Reports_At_Tier_A()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.md"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/submit-expense.receipt-missing", Path.Combine(temp.Path, "submit.md"), """{"outcome": "refusal", "code": "spending.receipt-missing", "meaning": "The expense is above the receipt threshold and carries no receipt."}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "result-scenario-missing").ToList();
        var finding = Assert.Single(findings);

        Assert.Equal("A", finding.Tier);
        Assert.Contains("result/submit-expense.receipt-missing", finding.Message, StringComparison.Ordinal);
        Assert.Contains("names code 'spending.receipt-missing'", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/model#give-every-use-case-its-results</proves>
    [Fact]
    public void Derived_Result_With_No_Scenario_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.md"), """{"kind": "command"}"""),
            Entry("result", "result/submit-expense.forbidden", Path.Combine(temp.Path, "submit.md"), """{"outcome": "refusal", "code": "spending.forbidden", "meaning": "The caller lacks the grant the use case requires."}""", "use-case/spending.submit-expense", true));

        Assert.DoesNotContain("result-scenario-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-use-case-its-results</proves>
    [Fact]
    public void Automation_Scenario_Naming_The_Runs_Use_Case_Result_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.md"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/submit-expense.submitted", Path.Combine(temp.Path, "submit.md"), """{"outcome": "success", "meaning": "The expense is submitted."}"""),
            TestModel.Record("automation", "automation/spending.email-clerk-on-expense-submitted", Path.Combine(temp.Path, "notify.md"), """{"triggeredBy": {"event": "event/spending.expense-submitted"}, "effects": [{"runs": "use-case/spending.submit-expense"}]}"""),
            Entry("scenario", "scenario/submit-expense.clerk-emailed-on-submission", Path.Combine(temp.Path, "notify.md"), """{"given": "An expense was submitted.", "when": "The clerk is emailed.", "then": "The clerk holds the submitted expense.", "result": "result/submit-expense.submitted"}""", "automation/spending.email-clerk-on-expense-submitted"));

        Assert.DoesNotContain("result-scenario-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-use-case-its-results</proves>
    [Fact]
    public void UseCaseOf_Resolves_Derived_Shaped_Id_Holder()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.md"), """{"kind": "command"}"""));

        Assert.True(Identifiers.TrySplit("result/submit-expense.forbidden", out _, out var home, out _));
        var holder = ModelBuilder.UseCaseOf(model, home!);
        Assert.NotNull(holder);
        Assert.Equal("use-case/spending.submit-expense", holder.Id);
    }

    /// <proves>litespec/core/model#give-every-use-case-its-results</proves>
    [Fact]
    public void Scenario_Ending_In_Other_Use_Case_Result_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.md"), """{"kind": "command"}"""),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve.md"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/approve-expense.approved", Path.Combine(temp.Path, "approve.md"), """{"outcome": "success", "meaning": "The expense is approved."}"""),
            TestModel.Record("scenario", "scenario/submit-expense.submitting-with-a-receipt-succeeds", Path.Combine(temp.Path, "submit.md"), """{"given": "An employee paid for a train ticket and kept the receipt.", "when": "The employee submits the expense with the receipt.", "then": "The expense is approved.", "result": "result/approve-expense.approved"}"""));

        Assert.Contains("scenario-result-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#give-every-use-case-its-results</proves>
    [Fact]
    public void Scenario_Ending_In_Own_Use_Case_Derived_Forbidden_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.md"), """{"kind": "command"}"""),
            Entry("result", "result/submit-expense.forbidden", Path.Combine(temp.Path, "submit.md"), """{"outcome": "refusal", "code": "spending.forbidden", "meaning": "The caller lacks the grant the use case requires."}""", "use-case/spending.submit-expense", true),
            TestModel.Record("scenario", "scenario/submit-expense.submitting-without-a-grant-is-refused", Path.Combine(temp.Path, "submit.md"), """{"given": "An employee holds no grant to submit expenses.", "when": "The employee submits the expense.", "then": "The expense is refused.", "result": "result/submit-expense.forbidden"}"""));

        Assert.DoesNotContain("scenario-result-unknown", TestModel.Codes(model));
    }

    /// <summary>An entry of the type and id on the page, with the JSON fields.</summary>
    static Record Entry(string type, string id, string file, string json, string pageId, bool derived = false)
    {
        using var document = JsonDocument.Parse(json);
        var raw = document.RootElement.Clone();

        return new Record
        {
            Type = type,
            Id = id,
            Title = id,
            Owner = "lending-team",
            Status = "approved",
            File = file,
            Line = 1,
            IsEntry = true,
            PageId = pageId,
            Derived = derived,
            Raw = raw,
        };
    }
}
