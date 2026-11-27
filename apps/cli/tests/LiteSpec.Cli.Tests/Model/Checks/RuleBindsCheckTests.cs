using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves a rule spanning aggregates names them in governs, which absorbed binds.</summary>
public sealed class RuleBindsCheckTests
{
    /// <summary>Rule Spanning Two Aggregates With Both Governed Passes.</summary>
    /// <proves>litespec/core/domain-model#name-every-spanned-aggregate-in-binds</proves>
    [Fact]
    public void RuleSpanningTwoAggregatesWithBothGovernedPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/spending.receipt-filed-once", Path.Combine(temp.Path, "spending.yaml"), """{"kind": "invariant", "statement": "A receipt file supports one expense across spending.", "governs": ["aggregate/spending.expense", "aggregate/spending.receipt"]}"""));

        Assert.DoesNotContain("rule-binds-missing", TestModel.Codes(model));
    }

    /// <summary>Rule Governing One Aggregate Passes.</summary>
    /// <proves>litespec/core/domain-model#name-every-spanned-aggregate-in-binds</proves>
    [Fact]
    public void RuleGoverningOneAggregatePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/expense.reimbursed-once", Path.Combine(temp.Path, "expense.yaml"), """{"kind": "invariant", "statement": "An expense is reimbursed at most once.", "governs": ["use-case/spending.reimburse-expense", "aggregate/spending.expense"]}"""));

        Assert.DoesNotContain("rule-binds-missing", TestModel.Codes(model));
    }
}
