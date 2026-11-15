using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves rule-binds-missing reports a rule spanning aggregates that names none or not all of them in binds.</summary>
public sealed class RuleBindsCheckTests
{
    /// <summary>Rule Spanning Two Aggregates With Both Bound Passes.</summary>
    /// <proves>litespec/core/domain-model#name-every-aggregate-a-rule-spans-in-binds</proves>
    [Fact]
    public void RuleSpanningTwoAggregatesWithBothBoundPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/spending.receipt-filed-once", Path.Combine(temp.Path, "spending.yaml"), """{"kind": "invariant", "statement": "A receipt file supports one expense across spending.", "governs": ["aggregate/spending.expense", "aggregate/spending.receipt"], "binds": ["aggregate/spending.expense", "aggregate/spending.receipt"]}"""));

        Assert.DoesNotContain("rule-binds-missing", TestModel.Codes(model));
    }

    /// <summary>Rule Spanning Two Aggregates With None Bound Reports.</summary>
    /// <proves>litespec/core/domain-model#name-every-aggregate-a-rule-spans-in-binds</proves>
    [Fact]
    public void RuleSpanningTwoAggregatesWithNoneBoundReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/receipt.attaches-before-decision", Path.Combine(temp.Path, "receipt.yaml"), """{"kind": "precondition", "statement": "A receipt attaches before the expense is decided.", "governs": ["use-case/spending.attach-receipt", "aggregate/spending.expense", "aggregate/spending.receipt"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "rule-binds-missing");
        Assert.Equal("Rule 'rule/receipt.attaches-before-decision' spans 'aggregate/spending.expense', 'aggregate/spending.receipt' but binds nothing.", finding.Message);
        Assert.Equal("rule/receipt.attaches-before-decision", finding.Subject);
        Assert.Equal("G", finding.Tier);
    }

    /// <summary>Rule Spanning Two Aggregates With One Bound Reports.</summary>
    /// <proves>litespec/core/domain-model#name-every-aggregate-a-rule-spans-in-binds</proves>
    [Fact]
    public void RuleSpanningTwoAggregatesWithOneBoundReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/receipt.attaches-before-decision", Path.Combine(temp.Path, "receipt.yaml"), """{"kind": "precondition", "statement": "A receipt attaches before the expense is decided.", "governs": ["aggregate/spending.expense", "aggregate/spending.receipt"], "binds": ["aggregate/spending.expense"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "rule-binds-missing");
        Assert.Equal("Rule 'rule/receipt.attaches-before-decision' spans 'aggregate/spending.expense', 'aggregate/spending.receipt' but binds 'aggregate/spending.expense'.", finding.Message);
    }

    /// <summary>Rule Governing One Aggregate Without Binds Passes.</summary>
    /// <proves>litespec/core/domain-model#name-every-aggregate-a-rule-spans-in-binds</proves>
    [Fact]
    public void RuleGoverningOneAggregateWithoutBindsPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/expense.reimbursed-once", Path.Combine(temp.Path, "expense.yaml"), """{"kind": "invariant", "statement": "An expense is reimbursed at most once.", "governs": ["use-case/spending.reimburse-expense", "aggregate/spending.expense"]}"""));

        Assert.DoesNotContain("rule-binds-missing", TestModel.Codes(model));
    }
}
