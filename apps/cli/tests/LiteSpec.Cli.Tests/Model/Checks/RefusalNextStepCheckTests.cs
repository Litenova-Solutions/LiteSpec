using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves refusal-next-step-unknown reports a refusal message naming a way out no record provides.</summary>
public sealed class RefusalNextStepCheckTests
{
    /// <summary>Refusal Message Naming An Unknown Use Case Reports.</summary>
    /// <proves>litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence</proves>
    [Fact]
    public void RefusalMessageNamingAnUnknownUseCaseReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/submit-expense.closed-report", Path.Combine(temp.Path, "submit-expense.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.report-closed", "message": "This expense falls in a closed report. Submit it with use-case/spending.reopen-report."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "refusal-next-step-unknown");
        Assert.Equal("Result 'result/submit-expense.closed-report' names 'use-case/spending.reopen-report' in its refusal message, which no record holds.", finding.Message);
        Assert.Equal("result/submit-expense.closed-report", finding.Subject);
        Assert.Equal("G", finding.Tier);
    }

    /// <summary>Refusal Message Promising Another Way Out Reports.</summary>
    /// <proves>litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence</proves>
    [Fact]
    public void RefusalMessagePromisingAnotherWayOutReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/submit-expense.closed-report", Path.Combine(temp.Path, "submit-expense.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.report-closed", "message": "This expense falls in a closed report. Submit it to the next open report instead."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "refusal-next-step-unknown");
        Assert.Equal("Result 'result/submit-expense.closed-report' carries a refusal message promising another way out with 'instead'.", finding.Message);
        Assert.Equal("G", finding.Tier);
    }

    /// <summary>Refusal Message Promising Another Way Out With Alternatively Reports.</summary>
    /// <proves>litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence</proves>
    [Fact]
    public void RefusalMessagePromisingAnotherWayOutWithAlternativelyReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/submit-expense.closed-report", Path.Combine(temp.Path, "submit-expense.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.report-closed", "message": "This expense falls in a closed report. Alternatively, ask finance to move it."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "refusal-next-step-unknown");
        Assert.Equal("Result 'result/submit-expense.closed-report' carries a refusal message promising another way out with 'Alternatively'.", finding.Message);
    }

    /// <summary>Refusal Message Without A Way Out Passes.</summary>
    /// <proves>litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence</proves>
    [Fact]
    public void RefusalMessageWithoutAWayOutPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/approve-expense.decided", Path.Combine(temp.Path, "approve-expense.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.not-submitted", "message": "This expense is already decided, so there is nothing to approve."}"""));

        Assert.DoesNotContain("refusal-next-step-unknown", TestModel.Codes(model));
    }

    /// <summary>Refusal Message With Instead Of Passes.</summary>
    /// <proves>litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence</proves>
    [Fact]
    public void RefusalMessageWithInsteadOfPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/change-receipt-threshold.negative-threshold", Path.Combine(temp.Path, "change-receipt-threshold.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.negative-threshold", "message": "Enter the amount in cents instead of euros."}"""));

        Assert.DoesNotContain("refusal-next-step-unknown", TestModel.Codes(model));
    }

    /// <summary>Refusal Message Naming An Existing Use Case Passes.</summary>
    /// <proves>litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence</proves>
    [Fact]
    public void RefusalMessageNamingAnExistingUseCasePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit-expense.yaml"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/attach-receipt.no-expense", Path.Combine(temp.Path, "attach-receipt.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.expense-missing", "message": "No submitted expense holds this receipt. Submit one with use-case/spending.submit-expense."}"""));

        Assert.DoesNotContain("refusal-next-step-unknown", TestModel.Codes(model));
    }

    /// <summary>Success Result With A Cue Word Passes.</summary>
    /// <proves>litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence</proves>
    [Fact]
    public void SuccessResultWithACueWordPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/submit-expense.submitted", Path.Combine(temp.Path, "submit-expense.yaml"), """{"outcome": "success", "meaning": "The expense is submitted.", "message": "The expense is submitted and nothing else happens instead."}"""));

        Assert.DoesNotContain("refusal-next-step-unknown", TestModel.Codes(model));
    }
}
