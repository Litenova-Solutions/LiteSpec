using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves guard-module-foreign reports a guard reaching into another module.</summary>
public sealed class GuardModuleForeignCheckTests
{
    /// <summary>Guard On Another Module's Aggregate Reports.</summary>
    /// <proves>litespec/core/use-cases#hold-every-cross-aggregate-guard-until-commit</proves>
    [Fact]
    public void GuardOnAnotherModulesAggregateReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "aggregate.yaml"), """{}"""),
            TestModel.Record("aggregate", "aggregate/finance.refund", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "aggregates", "refunds", "aggregate.yaml"), """{}"""),
            TestModel.Record("use-case", "use-case/finance.refund-fine", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "use-cases", "refund-fine.yaml"), """{"kind": "command", "writes": ["aggregate/finance.refund"], "consistency": {"mode": "atomic", "guards": ["aggregate/lending.loan"]}}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "guard-module-foreign");
        Assert.Equal("Command 'use-case/finance.refund-fine' guards on 'aggregate/lending.loan' in module 'lending', whose rule cannot hold when the command commits.", finding.Message);
        Assert.Equal("use-case/finance.refund-fine", finding.Subject);
        Assert.Equal("G", finding.Tier);
    }

    /// <summary>Bound Rule Over Another Module's Read Aggregate Reports Instead Of Undeclared.</summary>
    /// <proves>litespec/core/use-cases#hold-every-cross-aggregate-guard-until-commit</proves>
    [Fact]
    public void BoundRuleOverAnotherModulesReadAggregateReportsInsteadOfUndeclared()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "aggregate.yaml"), """{}"""),
            TestModel.Record("aggregate", "aggregate/finance.refund", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "aggregates", "refunds", "aggregate.yaml"), """{}"""),
            TestModel.Record("rule", "rule/finance.refund-needs-loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "rules", "finance-rules.yaml"), """{"kind": "precondition", "statement": "A refund needs its loan.", "governs": ["aggregate/lending.loan"]}"""),
            TestModel.Record("use-case", "use-case/finance.refund-fine", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "use-cases", "refund-fine.yaml"), """{"kind": "command", "writes": ["aggregate/finance.refund"], "reads": ["aggregate/lending.loan"], "consistency": {"mode": "atomic", "boundBy": ["rule/finance.refund-needs-loan"]}}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "guard-module-foreign");
        Assert.Equal("Command 'use-case/finance.refund-fine' reads 'aggregate/lending.loan' in module 'lending' under rule 'rule/finance.refund-needs-loan', which cannot hold when the command commits.", finding.Message);
        Assert.DoesNotContain("guard-undeclared", TestModel.Codes(model));
    }

    /// <summary>Guard On The Own Module's Aggregate Passes.</summary>
    /// <proves>litespec/core/use-cases#hold-every-cross-aggregate-guard-until-commit</proves>
    [Fact]
    public void GuardOnTheOwnModulesAggregatePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/spending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "aggregate.yaml"), """{}"""),
            TestModel.Record("aggregate", "aggregate/spending.policy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "aggregates", "policies", "aggregate.yaml"), """{}"""),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "use-cases", "submit-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "reads": ["aggregate/spending.policy"], "consistency": {"mode": "atomic", "guards": ["aggregate/spending.policy"]}}"""));

        Assert.DoesNotContain("guard-module-foreign", TestModel.Codes(model));
    }

    /// <summary>Guard Without A Module Folder Passes.</summary>
    /// <proves>litespec/core/use-cases#hold-every-cross-aggregate-guard-until-commit</proves>
    [Fact]
    public void GuardWithoutAModuleFolderPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{}"""),
            TestModel.Record("use-case", "use-case/finance.refund-fine", Path.Combine(temp.Path, "refund-fine.yaml"), """{"kind": "command", "consistency": {"mode": "atomic", "guards": ["aggregate/lending.loan"]}}"""));

        Assert.DoesNotContain("guard-module-foreign", TestModel.Codes(model));
    }

    /// <summary>Missing Guard On The Own Module Still Reports Undeclared.</summary>
    /// <proves>litespec/core/use-cases#hold-every-cross-aggregate-guard-until-commit</proves>
    [Fact]
    public void MissingGuardOnTheOwnModuleStillReportsUndeclared()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/spending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "aggregate.yaml"), """{}"""),
            TestModel.Record("aggregate", "aggregate/spending.policy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "aggregates", "policies", "aggregate.yaml"), """{}"""),
            TestModel.Record("rule", "rule/expense.receipt-required-above-threshold", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "rules", "expense-rules.yaml"), """{"kind": "precondition", "statement": "An expense above the policy threshold needs a receipt.", "governs": ["aggregate/spending.policy"]}"""),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "use-cases", "submit-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "reads": ["aggregate/spending.policy"], "consistency": {"mode": "atomic", "boundBy": ["rule/expense.receipt-required-above-threshold"]}}"""));

        Assert.Single(Checker.Check(model).Findings, found => found.Code == "guard-undeclared");
        Assert.DoesNotContain("guard-module-foreign", TestModel.Codes(model));
    }
}
