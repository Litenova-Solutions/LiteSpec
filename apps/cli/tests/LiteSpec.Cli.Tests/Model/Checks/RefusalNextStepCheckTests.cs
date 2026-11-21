using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves refusal-next-step-unknown reports a refusal sentence naming a way out no record provides.</summary>
public sealed class RefusalNextStepCheckTests
{
    /// <summary>Refusal Sentence Naming An Unknown Use Case Reports.</summary>
    /// <proves>litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence</proves>
    [Fact]
    public void RefusalSentenceNamingAnUnknownUseCaseReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/submit-expense.closed-report", Path.Combine(temp.Path, "submit-expense.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.report-closed", "sentence": "This expense falls in a closed report. Submit it with use-case/spending.reopen-report."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "refusal-next-step-unknown");
        Assert.Equal("Result 'result/submit-expense.closed-report' names 'use-case/spending.reopen-report' in its refusal sentence, which no record holds.", finding.Message);
        Assert.Equal("result/submit-expense.closed-report", finding.Subject);
        Assert.Equal("A", finding.Tier);
    }

    /// <summary>Refusal Sentence Promising Another Way Out Reports.</summary>
    /// <proves>litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence</proves>
    [Fact]
    public void RefusalSentencePromisingAnotherWayOutReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/submit-expense.closed-report", Path.Combine(temp.Path, "submit-expense.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.report-closed", "sentence": "This expense falls in a closed report. Submit it to the next open report instead."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "refusal-next-step-unknown");
        Assert.Equal("Result 'result/submit-expense.closed-report' carries a refusal sentence promising another way out with 'instead'.", finding.Message);
        Assert.Equal("A", finding.Tier);
    }

    /// <summary>Refusal Sentence Promising Another Way Out With Alternatively Reports.</summary>
    /// <proves>litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence</proves>
    [Fact]
    public void RefusalSentencePromisingAnotherWayOutWithAlternativelyReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/submit-expense.closed-report", Path.Combine(temp.Path, "submit-expense.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.report-closed", "sentence": "This expense falls in a closed report. Alternatively, ask finance to move it."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "refusal-next-step-unknown");
        Assert.Equal("Result 'result/submit-expense.closed-report' carries a refusal sentence promising another way out with 'Alternatively'.", finding.Message);
    }

    /// <summary>Refusal Sentence Without A Way Out Passes.</summary>
    /// <proves>litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence</proves>
    [Fact]
    public void RefusalSentenceWithoutAWayOutPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/approve-expense.decided", Path.Combine(temp.Path, "approve-expense.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.not-submitted", "sentence": "This expense is already decided, so there is nothing to approve."}"""));

        Assert.DoesNotContain("refusal-next-step-unknown", TestModel.Codes(model));
    }

    /// <summary>Refusal Sentence With Instead Of Passes.</summary>
    /// <proves>litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence</proves>
    [Fact]
    public void RefusalSentenceWithInsteadOfPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/change-receipt-threshold.negative-threshold", Path.Combine(temp.Path, "change-receipt-threshold.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.negative-threshold", "sentence": "Enter the amount in cents instead of euros."}"""));

        Assert.DoesNotContain("refusal-next-step-unknown", TestModel.Codes(model));
    }

    /// <summary>Refusal Sentence Naming An Existing Use Case Passes.</summary>
    /// <proves>litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence</proves>
    [Fact]
    public void RefusalSentenceNamingAnExistingUseCasePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit-expense.yaml"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/attach-receipt.no-expense", Path.Combine(temp.Path, "attach-receipt.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.expense-missing", "sentence": "No submitted expense holds this receipt. Submit one with use-case/spending.submit-expense."}"""));

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
            TestModel.Record("result", "result/submit-expense.submitted", Path.Combine(temp.Path, "submit-expense.yaml"), """{"outcome": "success", "meaning": "The expense is submitted.", "sentence": "The expense is submitted and nothing else happens instead."}"""));

        Assert.DoesNotContain("refusal-next-step-unknown", TestModel.Codes(model));
    }
}
