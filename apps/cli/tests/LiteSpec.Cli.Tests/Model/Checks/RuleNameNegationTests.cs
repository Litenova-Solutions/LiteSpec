using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves rule-name-invalid reports only a negation the main claim states, not a subordinate scope clause.</summary>
public sealed class RuleNameNegationTests
{
    /// <summary>Rule With Negation Only In Scope Clause Passes.</summary>
    /// <proves>litespec/core/domain-model#keep-the-negation-word-in-the-rule-name</proves>
    [Fact]
    public void RuleWithNegationOnlyInScopeClausePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "rule",
                "rule/loan.late-fee-applies",
                Path.Combine(temp.Path, "loan.md"),
                """{"kind": "invariant", "statement": "A late fee applies to cases this rule does not list.", "governs": ["use-case/lending.borrow-copy"]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command"}"""));

        Assert.DoesNotContain("rule-name-invalid", TestModel.Codes(model));
    }

    /// <summary>Rule With Negated Main Clause Reports.</summary>
    /// <proves>litespec/core/domain-model#keep-the-negation-word-in-the-rule-name</proves>
    [Fact]
    public void RuleWithNegatedMainClauseReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "rule",
                "rule/order.cancel-after-shipment",
                Path.Combine(temp.Path, "order.md"),
                """{"kind": "invariant", "statement": "An order never cancels after shipment.", "governs": ["use-case/lending.borrow-copy"]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command"}"""));

        var finding = Assert.Single(WithCode(Checker.Check(model).Findings, "rule-name-invalid"));

        Assert.Equal("Rule 'rule/order.cancel-after-shipment' says 'never' but its name keeps no restricting word.", finding.Message);
        Assert.Equal("rule/order.cancel-after-shipment", finding.Subject);
    }

    /// <summary>Rule With Negation Before Its Condition Reports.</summary>
    /// <proves>litespec/core/domain-model#keep-the-negation-word-in-the-rule-name</proves>
    [Fact]
    public void RuleWithNegationBeforeItsConditionReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "rule",
                "rule/copy.has-one-home-shelf",
                Path.Combine(temp.Path, "copy.md"),
                """{"kind": "invariant", "statement": "A copy has no home shelf when retired.", "governs": ["use-case/lending.register-copy"]}"""),
            TestModel.Record("use-case", "use-case/lending.register-copy", Path.Combine(temp.Path, "register.yaml"), """{"kind": "command"}"""));

        Assert.Contains("rule-name-invalid", TestModel.Codes(model));
    }

    /// <summary>Keeps the findings of one code.</summary>
    /// <param name="findings">The findings to filter.</param>
    /// <param name="code">The finding code to keep.</param>
    /// <returns>The findings carrying the code.</returns>
    static IReadOnlyList<Finding> WithCode(IReadOnlyList<Finding> findings, string code) =>
        findings.Where(finding => finding.Code == code).ToList();
}
