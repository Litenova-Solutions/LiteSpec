using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the advisory checks against hand-built models.</summary>
public sealed class AdvisoryCheckTests
{
    /// <summary>Use Case Without Summary Reports.</summary>
    /// <proves>litespec/core/use-cases#write-prose-on-every-use-case</proves>
    [Fact]
    public void UseCaseWithoutSummaryReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "prose-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' holds no summary field.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>Use Case With Summary Passes.</summary>
    /// <proves>litespec/core/use-cases#write-prose-on-every-use-case</proves>
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
    /// <proves>litespec/core/use-cases#write-prose-on-every-use-case</proves>
    [Fact]
    public void ReactionWithoutSummaryReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.notify-borrower-on-loan-borrowed", Path.Combine(temp.Path, "notify.yaml"), """{"triggeredBy": ["event/loan.loan-borrowed"], "effects": [{"sends": "notification/lending.loan-due"}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "prose-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Reaction 'reaction/lending.notify-borrower-on-loan-borrowed' holds no summary field.", finding.Message);
        Assert.Equal("reaction/lending.notify-borrower-on-loan-borrowed", finding.Subject);
    }

    /// <summary>Goal Without Statement Reports.</summary>
    /// <proves>litespec/core/use-cases#write-prose-on-every-use-case</proves>
    [Fact]
    public void GoalWithoutStatementReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("goal", "goal/faster-pickup", Path.Combine(temp.Path, "pickup.yaml"), """{"summary": "Readers pick up ready holds faster.", "measure": "Median pickup wait", "target": "2 days"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "prose-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Goal 'goal/faster-pickup' holds no statement field.", finding.Message);
        Assert.Equal("goal/faster-pickup", finding.Subject);
    }

    /// <summary>Goal With Summary And Statement Passes.</summary>
    /// <proves>litespec/core/use-cases#write-prose-on-every-use-case</proves>
    [Fact]
    public void GoalWithSummaryAndStatementPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("goal", "goal/faster-pickup", Path.Combine(temp.Path, "pickup.yaml"), """{"summary": "Readers pick up ready holds faster.", "statement": "Readers pick up ready holds faster.", "measure": "Median pickup wait", "target": "2 days"}"""));

        Assert.DoesNotContain("prose-missing", TestModel.Codes(model));
    }

    /// <summary>Atomic With Two Cites Rules Reports.</summary>
    /// <proves>litespec/core/use-cases#state-the-consistency-of-every-use-case</proves>
    [Fact]
    public void AtomicWithTwoCitesRulesReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command", "consistency": {"mode": "atomic", "cites": ["rule/loan.one-open-loan", "rule/copy.one-home-shelf"]}}"""));

        Assert.Contains("atomic-group-duplicate", TestModel.Codes(model));
    }

    /// <summary>Atomic With One Cites Rule Passes.</summary>
    /// <proves>litespec/core/use-cases#state-the-consistency-of-every-use-case</proves>
    [Fact]
    public void AtomicWithOneCitesRulePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command", "consistency": {"mode": "atomic", "cites": ["rule/loan.one-open-loan"]}}"""));

        Assert.DoesNotContain("atomic-group-duplicate", TestModel.Codes(model));
    }
}
