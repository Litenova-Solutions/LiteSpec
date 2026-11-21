using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the guard checks: a command bound by a rule over an aggregate it only reads names that aggregate in its guards, and a guarded command is proven against a competing command.</summary>
public sealed class GuardCheckTests
{
    /// <summary>Command Reading Guarded Aggregate With No Guards Reports.</summary>
    /// <proves>litespec/core/use-cases#hold-every-cross-aggregate-guard-until-commit</proves>
    [Fact]
    public void CommandReadingGuardedAggregateWithNoGuardsReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/spending.expense-on-open-report", Path.Combine(temp.Path, "spending.yaml"), """{"kind": "invariant", "statement": "An expense is submitted only on an open report.", "governs": ["aggregate/spending.report"]}"""),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "reads": ["aggregate/spending.report"], "consistency": {"mode": "atomic", "boundBy": ["rule/spending.expense-on-open-report"]}}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "guard-undeclared");

        Assert.Equal("G", finding.Tier);
        Assert.Contains("use-case/spending.submit-expense", finding.Message, StringComparison.Ordinal);
        Assert.Contains("aggregate/spending.report", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Command Naming Guarded Aggregate In Guards Passes.</summary>
    /// <proves>litespec/core/use-cases#hold-every-cross-aggregate-guard-until-commit</proves>
    [Fact]
    public void CommandNamingGuardedAggregateInGuardsPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/spending.expense-on-open-report", Path.Combine(temp.Path, "spending.yaml"), """{"kind": "invariant", "statement": "An expense is submitted only on an open report.", "governs": ["aggregate/spending.report"]}"""),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "reads": ["aggregate/spending.report"], "consistency": {"mode": "atomic", "boundBy": ["rule/spending.expense-on-open-report"], "guards": ["aggregate/spending.report"]}}"""));

        Assert.DoesNotContain("guard-undeclared", TestModel.Codes(model));
    }

    /// <summary>Command Writing Governed Aggregate Passes.</summary>
    /// <proves>litespec/core/use-cases#hold-every-cross-aggregate-guard-until-commit</proves>
    [Fact]
    public void CommandWritingGovernedAggregatePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/spending.expense-on-open-report", Path.Combine(temp.Path, "spending.yaml"), """{"kind": "invariant", "statement": "An expense is submitted only on an open report.", "governs": ["aggregate/spending.report"]}"""),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense", "aggregate/spending.report"], "reads": ["aggregate/spending.report"], "consistency": {"mode": "atomic", "boundBy": ["rule/spending.expense-on-open-report"]}}"""));

        Assert.DoesNotContain("guard-undeclared", TestModel.Codes(model));
    }

    /// <summary>Command Not Reading Governed Aggregate Passes.</summary>
    /// <proves>litespec/core/use-cases#hold-every-cross-aggregate-guard-until-commit</proves>
    [Fact]
    public void CommandNotReadingGovernedAggregatePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/spending.expense-on-open-report", Path.Combine(temp.Path, "spending.yaml"), """{"kind": "invariant", "statement": "An expense is submitted only on an open report.", "governs": ["aggregate/spending.report"]}"""),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "consistency": {"mode": "atomic", "boundBy": ["rule/spending.expense-on-open-report"]}}"""));

        Assert.DoesNotContain("guard-undeclared", TestModel.Codes(model));
    }

    /// <summary>Query Reading Governed Aggregate Passes.</summary>
    /// <proves>litespec/core/use-cases#hold-every-cross-aggregate-guard-until-commit</proves>
    [Fact]
    public void QueryReadingGovernedAggregatePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/spending.expense-on-open-report", Path.Combine(temp.Path, "spending.yaml"), """{"kind": "invariant", "statement": "An expense is submitted only on an open report.", "governs": ["aggregate/spending.report"]}"""),
            TestModel.Record("use-case", "use-case/spending.list-expenses", Path.Combine(temp.Path, "list.yaml"), """{"kind": "query", "reads": ["aggregate/spending.report"], "consistency": {"mode": "atomic", "boundBy": ["rule/spending.expense-on-open-report"]}}"""));

        Assert.DoesNotContain("guard-undeclared", TestModel.Codes(model));
    }

    /// <summary>Two Unguarded Aggregates Report One Finding Each.</summary>
    /// <proves>litespec/core/use-cases#hold-every-cross-aggregate-guard-until-commit</proves>
    [Fact]
    public void TwoUnguardedAggregatesReportOneFindingEach()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/spending.expense-on-open-report", Path.Combine(temp.Path, "spending.yaml"), """{"kind": "invariant", "statement": "An expense is submitted only on an open report in an open period.", "governs": ["aggregate/spending.report", "aggregate/spending.period"]}"""),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "reads": ["aggregate/spending.report", "aggregate/spending.period"], "consistency": {"mode": "atomic", "boundBy": ["rule/spending.expense-on-open-report"], "guards": ["aggregate/spending.report"]}}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "guard-undeclared").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("aggregate/spending.period", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Guarded Command With No Scenario Reports.</summary>
    /// <proves>litespec/core/use-cases#prove-every-guard-against-a-competing-command</proves>
    [Fact]
    public void GuardedCommandWithNoScenarioReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "reads": ["aggregate/spending.report"], "consistency": {"mode": "atomic", "guards": ["aggregate/spending.report"]}}"""),
            TestModel.Record("use-case", "use-case/spending.close-report", Path.Combine(temp.Path, "close.yaml"), """{"kind": "command", "writes": ["aggregate/spending.report"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "guard-race-scenario-missing");

        Assert.Equal("G", finding.Tier);
        Assert.Contains("use-case/spending.submit-expense", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Guarded Command With Race Scenario Passes.</summary>
    /// <proves>litespec/core/use-cases#prove-every-guard-against-a-competing-command</proves>
    [Fact]
    public void GuardedCommandWithRaceScenarioPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "reads": ["aggregate/spending.report"], "consistency": {"mode": "atomic", "guards": ["aggregate/spending.report"]}}"""),
            TestModel.Record("use-case", "use-case/spending.close-report", Path.Combine(temp.Path, "close.yaml"), """{"kind": "command", "writes": ["aggregate/spending.report"]}"""),
            TestModel.Record("result", "result/submit-expense.submitted", Path.Combine(temp.Path, "submit.yaml"), """{"outcome": "success", "meaning": "The expense is submitted."}"""),
            TestModel.Record("scenario", "scenario/submit-expense.closing-while-submitting-serializes", Path.Combine(temp.Path, "submit.yaml"), """{"given": "An employee submits an expense on an open report.", "when": "The report is closed at the same moment.", "then": "One command wins and the other sees the new state.", "result": "result/submit-expense.submitted", "competesWith": "use-case/spending.close-report"}"""));

        Assert.DoesNotContain("guard-race-scenario-missing", TestModel.Codes(model));
    }

    /// <summary>Guarded Command With Scenario Naming No Competitor Reports.</summary>
    /// <proves>litespec/core/use-cases#prove-every-guard-against-a-competing-command</proves>
    [Fact]
    public void GuardedCommandWithScenarioNamingNoCompetitorReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "reads": ["aggregate/spending.report"], "consistency": {"mode": "atomic", "guards": ["aggregate/spending.report"]}}"""),
            TestModel.Record("use-case", "use-case/spending.close-report", Path.Combine(temp.Path, "close.yaml"), """{"kind": "command", "writes": ["aggregate/spending.report"]}"""),
            TestModel.Record("result", "result/submit-expense.submitted", Path.Combine(temp.Path, "submit.yaml"), """{"outcome": "success", "meaning": "The expense is submitted."}"""),
            TestModel.Record("scenario", "scenario/submit-expense.submitting-with-a-receipt-succeeds", Path.Combine(temp.Path, "submit.yaml"), """{"given": "An employee paid for a train ticket and kept the receipt.", "when": "The employee submits the expense with the receipt.", "then": "The expense is submitted.", "result": "result/submit-expense.submitted"}"""));

        Assert.Contains("guard-race-scenario-missing", TestModel.Codes(model));
    }

    /// <summary>Guarded Command At Small Size Passes.</summary>
    /// <proves>litespec/core/use-cases#prove-every-guard-against-a-competing-command</proves>
    [Fact]
    public void GuardedCommandAtSmallSizePasses()
    {
        using var temp = TestCli.Temp("checks");
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec/project.yaml"),
            """{"project": {"name": "Guard Fixture"}, "size": "small"}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"], "reads": ["aggregate/spending.report"], "consistency": {"mode": "atomic", "guards": ["aggregate/spending.report"]}}"""));

        Assert.DoesNotContain("guard-race-scenario-missing", TestModel.Codes(model));
    }

    /// <summary>Unguarded Command With No Scenario Passes.</summary>
    /// <proves>litespec/core/use-cases#prove-every-guard-against-a-competing-command</proves>
    [Fact]
    public void UnguardedCommandWithNoScenarioPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"]}"""));

        Assert.DoesNotContain("guard-race-scenario-missing", TestModel.Codes(model));
    }
}
