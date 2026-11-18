using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves rule-sibling-uncited reports a command missing an invariant most writers of its aggregate cite.</summary>
public sealed class SiblingRuleCheckTests
{
    /// <summary>Command Missing An Invariant Its Siblings Cite Reports.</summary>
    /// <proves>litespec/core/use-cases#cite-the-invariants-most-writers-cite</proves>
    [Fact]
    public void CommandMissingAnInvariantItsSiblingsCiteReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, "expense.yaml"), """{}"""),
            TestModel.Record("rule", "rule/expense.reimbursed-once", Path.Combine(temp.Path, "expense-rules.yaml"), """{"kind": "invariant", "statement": "An expense is reimbursed at most once.", "governs": ["aggregate/spending.expense"]}"""),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "steps": [{"statement": "Spending approves the expense.", "cites": ["rule/expense.reimbursed-once"]}]}"""),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "steps": [{"statement": "Spending reimburses the expense.", "cites": ["rule/expense.reimbursed-once"]}]}"""),
            TestModel.Record("use-case", "use-case/spending.refuse-expense", Path.Combine(temp.Path, "refuse-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "steps": [{"statement": "Spending refuses the expense."}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "rule-sibling-uncited");
        Assert.Equal("Command 'use-case/spending.refuse-expense' writes 'aggregate/spending.expense' without citing invariant 'rule/expense.reimbursed-once', which 2 of 3 commands writing 'aggregate/spending.expense' cite.", finding.Message);
        Assert.Equal("use-case/spending.refuse-expense", finding.Subject);
        Assert.Equal("G", finding.Tier);
    }

    /// <summary>Commands Citing The Invariant Pass.</summary>
    /// <proves>litespec/core/use-cases#cite-the-invariants-most-writers-cite</proves>
    [Fact]
    public void CommandsCitingTheInvariantPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, "expense.yaml"), """{}"""),
            TestModel.Record("rule", "rule/expense.reimbursed-once", Path.Combine(temp.Path, "expense-rules.yaml"), """{"kind": "invariant", "statement": "An expense is reimbursed at most once.", "governs": ["aggregate/spending.expense"]}"""),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "steps": [{"statement": "Spending approves the expense.", "cites": ["rule/expense.reimbursed-once"]}]}"""),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "steps": [{"statement": "Spending reimburses the expense.", "cites": ["rule/expense.reimbursed-once"]}]}"""));

        Assert.DoesNotContain("rule-sibling-uncited", TestModel.Codes(model));
    }

    /// <summary>Invariant Cited By A Minority Of Writers Passes.</summary>
    /// <proves>litespec/core/use-cases#cite-the-invariants-most-writers-cite</proves>
    [Fact]
    public void InvariantCitedByAMinorityOfWritersPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, "expense.yaml"), """{}"""),
            TestModel.Record("rule", "rule/expense.reimbursed-once", Path.Combine(temp.Path, "expense-rules.yaml"), """{"kind": "invariant", "statement": "An expense is reimbursed at most once.", "governs": ["aggregate/spending.expense"]}"""),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "steps": [{"statement": "Spending stores the expense."}]}"""),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "steps": [{"statement": "Spending approves the expense."}]}"""),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "steps": [{"statement": "Spending reimburses the expense.", "cites": ["rule/expense.reimbursed-once"]}]}"""));

        Assert.DoesNotContain("rule-sibling-uncited", TestModel.Codes(model));
    }

    /// <summary>Precondition Cited By Most Writers Passes.</summary>
    /// <proves>litespec/core/use-cases#cite-the-invariants-most-writers-cite</proves>
    [Fact]
    public void PreconditionCitedByMostWritersPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, "expense.yaml"), """{}"""),
            TestModel.Record("rule", "rule/expense.decided-from-submitted", Path.Combine(temp.Path, "expense-rules.yaml"), """{"kind": "precondition", "statement": "An expense is decided only from Submitted.", "governs": ["aggregate/spending.expense"]}"""),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "steps": [{"statement": "Spending approves the expense.", "cites": ["rule/expense.decided-from-submitted"]}]}"""),
            TestModel.Record("use-case", "use-case/spending.refuse-expense", Path.Combine(temp.Path, "refuse-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "steps": [{"statement": "Spending refuses the expense.", "cites": ["rule/expense.decided-from-submitted"]}]}"""),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "steps": [{"statement": "Spending stores the expense."}]}"""));

        Assert.DoesNotContain("rule-sibling-uncited", TestModel.Codes(model));
    }

    /// <summary>Single Writer Passes.</summary>
    /// <proves>litespec/core/use-cases#cite-the-invariants-most-writers-cite</proves>
    [Fact]
    public void SingleWriterPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/spending.policy", Path.Combine(temp.Path, "policy.yaml"), """{}"""),
            TestModel.Record("rule", "rule/policy.threshold-never-negative", Path.Combine(temp.Path, "policy-rules.yaml"), """{"kind": "invariant", "statement": "A threshold is never negative.", "governs": ["aggregate/spending.policy"]}"""),
            TestModel.Record("use-case", "use-case/spending.change-receipt-threshold", Path.Combine(temp.Path, "change-receipt-threshold.yaml"), """{"kind": "command", "writes": ["aggregate/spending.policy"], "steps": [{"statement": "Spending sets the threshold."}]}"""));

        Assert.DoesNotContain("rule-sibling-uncited", TestModel.Codes(model));
    }

    /// <summary>Command Citing Through Its Refusal Result Counts As Citing.</summary>
    /// <proves>litespec/core/use-cases#cite-the-invariants-most-writers-cite</proves>
    [Fact]
    public void CommandCitingThroughItsRefusalResultCountsAsCiting()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, "expense.yaml"), """{}"""),
            TestModel.Record("rule", "rule/expense.reimbursed-once", Path.Combine(temp.Path, "expense-rules.yaml"), """{"kind": "invariant", "statement": "An expense is reimbursed at most once.", "governs": ["aggregate/spending.expense"], "refusal": "spending.already-reimbursed"}"""),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "steps": [{"statement": "Spending approves the expense.", "cites": ["rule/expense.reimbursed-once"]}]}"""),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "steps": [{"statement": "Spending reimburses the expense."}]}"""),
            TestModel.Record("result", "result/reimburse-expense.already-reimbursed", Path.Combine(temp.Path, "reimburse-expense.yaml"), """{"outcome": "refusal", "code": "spending.already-reimbursed"}"""),
            TestModel.Record("use-case", "use-case/spending.refuse-expense", Path.Combine(temp.Path, "refuse-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "steps": [{"statement": "Spending refuses the expense."}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "rule-sibling-uncited");
        Assert.Equal("use-case/spending.refuse-expense", finding.Subject);
    }
}
