using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves exercises-uncited reports a scenario exercising a rule its use case applies nowhere.</summary>
public sealed class ExercisesUncitedCheckTests
{
    /// <summary>Scenario Exercising A Rule Its Use Case Applies Nowhere Reports.</summary>
    /// <proves>litespec/core/use-cases#exercise-rules-from-scenarios</proves>
    [Fact]
    public void ScenarioExercisingARuleItsUseCaseAppliesNowhereReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, "expense.yaml"), """{}"""),
            TestModel.Record("rule", "rule/expense.reimbursed-once", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "rules", "reimbursed-once", "reimbursed-once.yaml"), """{"kind": "invariant", "statement": "An expense is reimbursed at most once.", "governs": ["aggregate/spending.expense"], "refusal": "refusal/spending.already-reimbursed"}"""),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "steps": [{"statement": "Spending stores the expense."}]}"""),
            TestModel.Record("result", "result/submit-expense.submitted", Path.Combine(temp.Path, "submit-expense.yaml"), """{"outcome": "success", "meaning": "The expense is submitted."}"""),
            TestModel.Record("scenario", "scenario/submit-expense.submitting-succeeds", Path.Combine(temp.Path, "submit-expense.yaml"), """{"given": "An employee paid for a train ticket.", "when": "The employee submits the expense.", "then": "The expense is submitted.", "result": "result/submit-expense.submitted", "exercises": ["rule/expense.reimbursed-once"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "exercises-uncited");
        Assert.Equal("Scenario 'scenario/submit-expense.submitting-succeeds' exercises 'rule/expense.reimbursed-once', which use case 'use-case/spending.submit-expense' cites nowhere in its steps, inputs, or results.", finding.Message);
        Assert.Equal("scenario/submit-expense.submitting-succeeds", finding.Subject);
        Assert.Equal("A", finding.Tier);
    }

    /// <summary>Scenario Exercising A Rule Its Steps Cite Passes.</summary>
    /// <proves>litespec/core/use-cases#exercise-rules-from-scenarios</proves>
    [Fact]
    public void ScenarioExercisingARuleItsStepsCitePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, "expense.yaml"), """{}"""),
            TestModel.Record("rule", "rule/expense.receipt-required-above-threshold", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "rules", "receipt-required-above-threshold", "receipt-required-above-threshold.yaml"), """{"kind": "precondition", "statement": "An expense above the policy threshold needs a receipt.", "governs": ["aggregate/spending.expense"], "refusal": "refusal/spending.receipt-missing"}"""),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "steps": [{"statement": "Spending checks the receipt rule.", "cites": ["rule/expense.receipt-required-above-threshold"]}]}"""),
            TestModel.Record("result", "result/submit-expense.submitted", Path.Combine(temp.Path, "submit-expense.yaml"), """{"outcome": "success", "meaning": "The expense is submitted."}"""),
            TestModel.Record("scenario", "scenario/submit-expense.submitting-succeeds", Path.Combine(temp.Path, "submit-expense.yaml"), """{"given": "An employee paid for a train ticket.", "when": "The employee submits the expense.", "then": "The expense is submitted.", "result": "result/submit-expense.submitted", "exercises": ["rule/expense.receipt-required-above-threshold"]}"""));

        Assert.DoesNotContain("exercises-uncited", TestModel.Codes(model));
    }

    /// <summary>Scenario Exercising A Rule Its Inputs Cite Passes.</summary>
    /// <proves>litespec/core/use-cases#exercise-rules-from-scenarios</proves>
    [Fact]
    public void ScenarioExercisingARuleItsInputsCitePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, "expense.yaml"), """{}"""),
            TestModel.Record("rule", "rule/expense.receipt-required-above-threshold", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "rules", "receipt-required-above-threshold", "receipt-required-above-threshold.yaml"), """{"kind": "precondition", "statement": "An expense above the policy threshold needs a receipt.", "governs": ["aggregate/spending.expense"], "refusal": "refusal/spending.receipt-missing"}"""),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "input": [{"name": "receipt", "meaning": "The proof of payment.", "type": "text", "required": false, "rules": ["rule/expense.receipt-required-above-threshold"], "origin": "caller", "classification": "internal"}], "steps": [{"statement": "Spending stores the expense."}]}"""),
            TestModel.Record("result", "result/submit-expense.submitted", Path.Combine(temp.Path, "submit-expense.yaml"), """{"outcome": "success", "meaning": "The expense is submitted."}"""),
            TestModel.Record("scenario", "scenario/submit-expense.submitting-succeeds", Path.Combine(temp.Path, "submit-expense.yaml"), """{"given": "An employee paid for a train ticket.", "when": "The employee submits the expense.", "then": "The expense is submitted.", "result": "result/submit-expense.submitted", "exercises": ["rule/expense.receipt-required-above-threshold"]}"""));

        Assert.DoesNotContain("exercises-uncited", TestModel.Codes(model));
    }

    /// <summary>Scenario Exercising A Rule Its Results Cite Passes.</summary>
    /// <proves>litespec/core/use-cases#exercise-rules-from-scenarios</proves>
    [Fact]
    public void ScenarioExercisingARuleItsResultsCitePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, "expense.yaml"), """{}"""),
            TestModel.Record("rule", "rule/expense.receipt-required-above-threshold", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "rules", "receipt-required-above-threshold", "receipt-required-above-threshold.yaml"), """{"kind": "precondition", "statement": "An expense above the policy threshold needs a receipt.", "governs": ["aggregate/spending.expense"], "refusal": "refusal/spending.receipt-missing"}"""),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit-expense.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "steps": [{"statement": "Spending stores the expense."}]}"""),
            TestModel.Record("result", "result/submit-expense.receipt-missing", Path.Combine(temp.Path, "submit-expense.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.receipt-missing"}"""),
            TestModel.Record("scenario", "scenario/submit-expense.submitting-without-a-receipt-is-refused", Path.Combine(temp.Path, "submit-expense.yaml"), """{"given": "An employee lost the receipt.", "when": "The employee submits the expense.", "then": "The submission is refused.", "result": "result/submit-expense.receipt-missing", "exercises": ["rule/expense.receipt-required-above-threshold"]}"""));

        Assert.DoesNotContain("exercises-uncited", TestModel.Codes(model));
    }

    /// <summary>Reaction Scenario Exercising A Rule Passes.</summary>
    /// <proves>litespec/core/use-cases#exercise-rules-from-scenarios</proves>
    [Fact]
    public void ReactionScenarioExercisingARulePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/spending.suppressed-manager-gets-no-reminder", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "rules", "suppressed-manager-gets-no-reminder", "suppressed-manager-gets-no-reminder.yaml"), """{"kind": "precondition", "statement": "A manager holding a reminder preference gets no approval reminder.", "governs": []}"""),
            TestModel.Record("scenario", "scenario/send-approver-reminders.suppressed-manager-gets-nothing", Path.Combine(temp.Path, "send-approver-reminders.yaml"), """{"given": "A manager holds a reminder preference.", "when": "The reminder run reaches the manager.", "then": "No reminder is sent.", "exercises": ["rule/spending.suppressed-manager-gets-no-reminder"]}"""));

        Assert.DoesNotContain("exercises-uncited", TestModel.Codes(model));
    }

    /// <summary>Scenario Exercising An Unknown Rule Does Not Report Unrelated.</summary>
    /// <proves>litespec/core/use-cases#exercise-rules-from-scenarios</proves>
    [Fact]
    public void ScenarioExercisingAnUnknownRuleDoesNotReportUnrelated()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit-expense.yaml"), """{"kind": "command", "steps": [{"statement": "Spending stores the expense."}]}"""),
            TestModel.Record("result", "result/submit-expense.submitted", Path.Combine(temp.Path, "submit-expense.yaml"), """{"outcome": "success", "meaning": "The expense is submitted."}"""),
            TestModel.Record("scenario", "scenario/submit-expense.submitting-succeeds", Path.Combine(temp.Path, "submit-expense.yaml"), """{"given": "An employee paid for a train ticket.", "when": "The employee submits the expense.", "then": "The expense is submitted.", "result": "result/submit-expense.submitted", "exercises": ["rule/expense.no-such-rule"]}"""));

        Assert.DoesNotContain("exercises-uncited", TestModel.Codes(model));
    }
}
