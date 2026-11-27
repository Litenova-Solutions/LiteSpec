using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the prose checks against hand-built models.</summary>
public sealed class ProseCheckTests
{
    /// <summary>Use Case Without Summary Reports.</summary>
    /// <proves>litespec/core/use-cases#summarize-every-use-case</proves>
    [Fact]
    public void UseCaseWithoutSummaryReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "prose-missing");
        Assert.Equal("R", finding.Tier);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' holds no summary field.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>Use Case With Summary Passes.</summary>
    /// <proves>litespec/core/use-cases#summarize-every-use-case</proves>
    [Fact]
    public void UseCaseWithSummaryPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.list-loans", Path.Combine(temp.Path, "loans.yaml"), """{"kind": "query", "summary": "A reader lists their loans."}"""));

        Assert.DoesNotContain("prose-missing", TestModel.Codes(model));
    }

    /// <summary>Reaction Without Summary Reports.</summary>
    /// <proves>litespec/core/use-cases#summarize-every-use-case</proves>
    [Fact]
    public void ReactionWithoutSummaryReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.notify-borrower-on-loan-borrowed", Path.Combine(temp.Path, "notify.yaml"), """{"triggeredBy": ["domain-event/loan.loan-borrowed"], "effects": [{"sends": "notification/lending.loan-due"}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "prose-missing");
        Assert.Equal("R", finding.Tier);
        Assert.Equal("Reaction 'reaction/lending.notify-borrower-on-loan-borrowed' holds no summary field.", finding.Message);
        Assert.Equal("reaction/lending.notify-borrower-on-loan-borrowed", finding.Subject);
    }

    /// <summary>Goal Without Statement Reports.</summary>
    /// <proves>litespec/core/use-cases#summarize-every-use-case</proves>
    [Fact]
    public void GoalWithoutStatementReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("goal", "goal/faster-pickup", Path.Combine(temp.Path, "pickup.yaml"), """{"summary": "Readers pick up ready holds faster.", "measure": "Median pickup wait", "target": "2 days"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "prose-missing");
        Assert.Equal("R", finding.Tier);
        Assert.Equal("Goal 'goal/faster-pickup' holds no statement field.", finding.Message);
        Assert.Equal("goal/faster-pickup", finding.Subject);
    }

    /// <summary>Goal With Summary And Statement Passes.</summary>
    /// <proves>litespec/core/use-cases#summarize-every-use-case</proves>
    [Fact]
    public void GoalWithSummaryAndStatementPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("goal", "goal/faster-pickup", Path.Combine(temp.Path, "pickup.yaml"), """{"summary": "Readers pick up ready holds faster.", "statement": "Readers pick up ready holds faster.", "measure": "Median pickup wait", "target": "2 days"}"""));

        Assert.DoesNotContain("prose-missing", TestModel.Codes(model));
    }

    /// <summary>Refusal Message Naming Its Own Code Reports.</summary>
    /// <proves>litespec/core/writing#keep-raw-refusal-codes-out-of-product-copy</proves>
    [Fact]
    public void RefusalMessageNamingItsOwnCodeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/submit-expense.closed-report", Path.Combine(temp.Path, "submit-expense.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.report-closed", "message": "This expense falls in a closed report (spending.report-closed). Ask finance to move it."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "refusal-code-shown");
        Assert.Equal("R", finding.Tier);
        Assert.Equal("Result 'result/submit-expense.closed-report' names refusal code 'spending.report-closed' in its refusal message.", finding.Message);
        Assert.Equal("result/submit-expense.closed-report", finding.Subject);
    }

    /// <summary>Refusal Message Naming Another Declared Code Reports.</summary>
    /// <proves>litespec/core/writing#keep-raw-refusal-codes-out-of-product-copy</proves>
    [Fact]
    public void RefusalMessageNamingAnotherDeclaredCodeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/submit-expense.closed-report", Path.Combine(temp.Path, "submit-expense.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.report-closed", "message": "This expense falls in a closed report. This is not lending.copy-not-available."}"""),
            TestModel.Record("result", "result/borrow-copy.unavailable", Path.Combine(temp.Path, "borrow-copy.yaml"), """{"outcome": "refusal", "refusal": "refusal/lending.copy-not-available", "message": "The copy is already lent."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "refusal-code-shown");
        Assert.Equal("R", finding.Tier);
        Assert.Equal("Result 'result/submit-expense.closed-report' names refusal code 'lending.copy-not-available' in its refusal message.", finding.Message);
        Assert.Equal("result/submit-expense.closed-report", finding.Subject);
    }

    /// <summary>Refusal Translation Naming A Code Reports.</summary>
    /// <proves>litespec/core/writing#keep-raw-refusal-codes-out-of-product-copy</proves>
    [Fact]
    public void RefusalTranslationNamingACodeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/submit-expense.closed-report", Path.Combine(temp.Path, "submit-expense.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.report-closed", "message": "This expense falls in a closed report.", "translations": {"nl": "Deze declaratie valt in een gesloten rapport SPENDING_REPORT_CLOSED."}}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "refusal-code-shown");
        Assert.Equal("R", finding.Tier);
        Assert.Equal("Result 'result/submit-expense.closed-report' names refusal code 'spending.report-closed' in its refusal message.", finding.Message);
        Assert.Equal("result/submit-expense.closed-report", finding.Subject);
    }

    /// <summary>Refusal Message Naming No Code Passes.</summary>
    /// <proves>litespec/core/writing#keep-raw-refusal-codes-out-of-product-copy</proves>
    [Fact]
    public void RefusalMessageNamingNoCodePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/approve-expense.decided", Path.Combine(temp.Path, "approve-expense.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.not-submitted", "message": "This expense is already decided, so there is nothing to approve."}"""));

        Assert.DoesNotContain("refusal-code-shown", TestModel.Codes(model));
    }

    /// <summary>Summary Carrying A Banned Phrase Reports.</summary>
    /// <proves>litespec/core/model#state-what-happens-in-every-behavior-statement</proves>
    [Fact]
    public void SummaryCarryingABannedPhraseReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.list-loans", Path.Combine(temp.Path, "loans.yaml"), """{"kind": "query", "summary": "A reader instantly lists their loans."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "statement-phrase-vague");
        Assert.Equal("R", finding.Tier);
        Assert.Equal("Use case 'use-case/lending.list-loans' carries banned phrase 'instantly' in summary.", finding.Message);
        Assert.Equal("use-case/lending.list-loans", finding.Subject);
    }

    /// <summary>Summary Carrying No Banned Phrase Passes.</summary>
    /// <proves>litespec/core/model#state-what-happens-in-every-behavior-statement</proves>
    [Fact]
    public void SummaryCarryingNoBannedPhrasePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.list-loans", Path.Combine(temp.Path, "loans.yaml"), """{"kind": "query", "summary": "A reader lists their loans."}"""));

        Assert.DoesNotContain("statement-phrase-vague", TestModel.Codes(model));
    }
}
