using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the rule checks treat a store like an aggregate: a rule spanning aggregates or stores binds every one of them, and governs drift compares governed and reached aggregates and stores.</summary>
public sealed class RuleStoreChecksTests
{
    /// <summary>Rule Governing One Store With Matching Binds Passes Drift.</summary>
    /// <proves>litespec/core/domain-model#state-what-each-rule-governs</proves>
    [Fact]
    public void RuleGoverningOneStoreWithMatchingBindsPassesDrift()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/spending.suppressed-manager-gets-no-reminder", Path.Combine(temp.Path, "spending.yaml"), """{"kind": "precondition", "statement": "A manager the suppression list names gets no approval reminder.", "governs": ["store/spending.suppression-list"], "binds": ["store/spending.suppression-list"]}"""),
            TestModel.Record("store", "store/spending.suppression-list", Path.Combine(temp.Path, "suppression-list.yaml"), """{"summary": "The suppression list names the managers who get no approval reminder.", "owner": "module/spending"}"""));

        Assert.DoesNotContain("rule-governs-drift", TestModel.Codes(model));
        Assert.DoesNotContain("rule-binds-missing", TestModel.Codes(model));
    }

    /// <summary>Rule Governing A Store Without Binds Reports Drift Naming Stores.</summary>
    /// <proves>litespec/core/domain-model#state-what-each-rule-governs</proves>
    [Fact]
    public void RuleGoverningStoreWithoutBindsReportsDriftNamingStores()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/spending.suppressed-manager-gets-no-reminder", Path.Combine(temp.Path, "spending.yaml"), """{"kind": "precondition", "statement": "A manager the suppression list names gets no approval reminder.", "governs": ["store/spending.suppression-list"]}"""),
            TestModel.Record("store", "store/spending.suppression-list", Path.Combine(temp.Path, "suppression-list.yaml"), """{"summary": "The suppression list names the managers who get no approval reminder.", "owner": "module/spending"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "rule-governs-drift");
        Assert.Contains("aggregates and stores", finding.Message);
        Assert.Contains("store/spending.suppression-list", finding.Message);
        Assert.Equal("rule/spending.suppressed-manager-gets-no-reminder", finding.Subject);
        Assert.Equal("G", finding.Tier);
    }

    /// <summary>Rule Governing An Aggregate And A Store With Both Bound Passes Drift.</summary>
    /// <proves>litespec/core/domain-model#state-what-each-rule-governs</proves>
    [Fact]
    public void RuleGoverningAggregateAndStoreWithBothBoundPassesDrift()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/lending.hold-names-a-logged-copy", Path.Combine(temp.Path, "lending.yaml"), """{"kind": "invariant", "statement": "A hold names a copy the delivery log holds.", "governs": ["aggregate/lending.loan", "store/lending.delivery-log"], "binds": ["aggregate/lending.loan", "store/lending.delivery-log"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Active", "meaning": "The loan is open."}]}"""),
            TestModel.Record("store", "store/lending.delivery-log", Path.Combine(temp.Path, "delivery-log.yaml"), """{"summary": "The log of delivered holds.", "owner": "module/lending"}"""));

        Assert.DoesNotContain("rule-governs-drift", TestModel.Codes(model));
        Assert.DoesNotContain("rule-binds-missing", TestModel.Codes(model));
    }

    /// <summary>Rule Governing Two Aggregates With Both Bound Passes Drift.</summary>
    /// <proves>litespec/core/domain-model#state-what-each-rule-governs</proves>
    [Fact]
    public void RuleGoverningTwoAggregatesWithBothBoundPassesDrift()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/spending.receipt-filed-once", Path.Combine(temp.Path, "spending.yaml"), """{"kind": "invariant", "statement": "A receipt file supports one expense across spending.", "governs": ["aggregate/spending.expense", "aggregate/spending.receipt"], "binds": ["aggregate/spending.expense", "aggregate/spending.receipt"]}"""),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, "expense.yaml"), """{"states": [{"name": "Submitted", "meaning": "The expense waits for approval."}]}"""),
            TestModel.Record("aggregate", "aggregate/spending.receipt", Path.Combine(temp.Path, "receipt.yaml"), """{"states": [{"name": "Filed", "meaning": "The receipt is filed."}]}"""));

        Assert.DoesNotContain("rule-governs-drift", TestModel.Codes(model));
    }

    /// <summary>Rule Spanning An Aggregate And A Store With Neither Bound Reports Binds Missing.</summary>
    /// <proves>litespec/core/domain-model#name-every-spanned-aggregate-in-binds</proves>
    [Fact]
    public void RuleSpanningAggregateAndStoreWithNeitherBoundReportsBindsMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/lending.hold-names-a-logged-copy", Path.Combine(temp.Path, "lending.yaml"), """{"kind": "invariant", "statement": "A hold names a copy the delivery log holds.", "governs": ["aggregate/lending.loan", "store/lending.delivery-log"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "rule-binds-missing");
        Assert.Equal("rule/lending.hold-names-a-logged-copy", finding.Subject);
        Assert.Equal("G", finding.Tier);
    }

    /// <summary>Rule Spanning An Aggregate And A Store With Both Bound Passes Binds Missing.</summary>
    /// <proves>litespec/core/domain-model#name-every-spanned-aggregate-in-binds</proves>
    [Fact]
    public void RuleSpanningAggregateAndStoreWithBothBoundPassesBindsMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/lending.hold-names-a-logged-copy", Path.Combine(temp.Path, "lending.yaml"), """{"kind": "invariant", "statement": "A hold names a copy the delivery log holds.", "governs": ["aggregate/lending.loan", "store/lending.delivery-log"], "binds": ["aggregate/lending.loan", "store/lending.delivery-log"]}"""));

        Assert.DoesNotContain("rule-binds-missing", TestModel.Codes(model));
    }

    /// <summary>Rule Governing One Store Without Binds Passes Binds Missing.</summary>
    /// <proves>litespec/core/domain-model#name-every-spanned-aggregate-in-binds</proves>
    [Fact]
    public void RuleGoverningOneStoreWithoutBindsPassesBindsMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/spending.suppressed-manager-gets-no-reminder", Path.Combine(temp.Path, "spending.yaml"), """{"kind": "precondition", "statement": "A manager the suppression list names gets no approval reminder.", "governs": ["store/spending.suppression-list"]}"""));

        Assert.DoesNotContain("rule-binds-missing", TestModel.Codes(model));
    }
}
