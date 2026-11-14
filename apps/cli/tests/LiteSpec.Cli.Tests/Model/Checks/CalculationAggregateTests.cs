using LiteSpec.Cli.Tests.Support;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the calculation-table-missing check resolves use cases under an aggregate use-cases folder.</summary>
public sealed class CalculationAggregateTests
{
    /// <summary>Calculation Governing An Aggregate Under Its Use Cases Folder Reads Scenario Examples.</summary>
    /// <proves>litespec/core/domain-model#tabulate-every-calculation-with-examples</proves>
    [Fact]
    public void CalculationGoverningAnAggregateUnderItsUseCasesFolderReadsScenarioExamples()
    {
        using var temp = TestCli.Temp("checks");
        var aggregateFolder = Path.Combine(temp.Path, "behavior", "modules", "lending", "aggregates", "loan");
        var useCaseFolder = Path.Combine(aggregateFolder, "use-cases", "borrow-copy");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Rule(Path.Combine(aggregateFolder, "rules", "late-fee-totals.md")),
            Aggregate(aggregateFolder),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(useCaseFolder, "borrow-copy.yaml"), """{"kind": "command"}"""),
            TestModel.Record(
                "scenario",
                "scenario/borrow-copy.late-fee-examples-hold",
                Path.Combine(useCaseFolder, "borrow-copy.md"),
                """{"given": "A loan stands overdue.", "when": "The fee is totaled.", "then": "The fee matches the table.", "result": "result/borrow-copy.borrowed", "examples": [{"days": 3, "fee": 1.5}]}"""));

        Assert.DoesNotContain("calculation-table-missing", TestModel.Codes(model));
    }

    /// <summary>Calculation Governing An Aggregate Under Its Use Cases Folder Reports Without An Examples Table.</summary>
    /// <proves>litespec/core/domain-model#tabulate-every-calculation-with-examples</proves>
    [Fact]
    public void CalculationGoverningAnAggregateUnderItsUseCasesFolderReportsWithoutAnExamplesTable()
    {
        using var temp = TestCli.Temp("checks");
        var aggregateFolder = Path.Combine(temp.Path, "behavior", "modules", "lending", "aggregates", "loan");
        var useCaseFolder = Path.Combine(aggregateFolder, "use-cases", "borrow-copy");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Rule(Path.Combine(aggregateFolder, "rules", "late-fee-totals.md")),
            Aggregate(aggregateFolder),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(useCaseFolder, "borrow-copy.yaml"), """{"kind": "command"}"""),
            TestModel.Record(
                "scenario",
                "scenario/borrow-copy.borrowing-an-available-copy-succeeds",
                Path.Combine(useCaseFolder, "borrow-copy.md"),
                """{"given": "A copy sits available.", "when": "The reader borrows it.", "then": "The loan is active.", "result": "result/borrow-copy.borrowed"}"""));

        Assert.Contains("calculation-table-missing", TestModel.Codes(model));
    }

    /// <summary>Calculation Governing An Aggregate Ignores A Module Level Use Case.</summary>
    /// <proves>litespec/core/domain-model#tabulate-every-calculation-with-examples</proves>
    [Fact]
    public void CalculationGoverningAnAggregateIgnoresAModuleLevelUseCase()
    {
        using var temp = TestCli.Temp("checks");
        var aggregateFolder = Path.Combine(temp.Path, "behavior", "modules", "lending", "aggregates", "loan");
        var useCaseFolder = Path.Combine(temp.Path, "behavior", "modules", "lending", "use-cases", "borrow-copy");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Rule(Path.Combine(aggregateFolder, "rules", "late-fee-totals.md")),
            Aggregate(aggregateFolder),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(useCaseFolder, "borrow-copy.yaml"), """{"kind": "command"}"""),
            TestModel.Record(
                "scenario",
                "scenario/borrow-copy.late-fee-examples-hold",
                Path.Combine(useCaseFolder, "borrow-copy.md"),
                """{"given": "A loan stands overdue.", "when": "The fee is totaled.", "then": "The fee matches the table.", "result": "result/borrow-copy.borrowed", "examples": [{"days": 3, "fee": 1.5}]}"""));

        Assert.Contains("calculation-table-missing", TestModel.Codes(model));
    }

    static Record Rule(string file) =>
        TestModel.Record(
            "rule",
            "rule/loan.late-fee-totals",
            file,
            """{"kind": "calculation", "statement": "A late fee totals the days overdue times the daily rate.", "governs": ["aggregate/lending.loan"]}""");

    static Record Aggregate(string folder) =>
        TestModel.Record(
            "aggregate",
            "aggregate/lending.loan",
            Path.Combine(folder, "aggregate.yaml"),
            """{"states": [{"name": "Active", "meaning": "The loan is open."}]}""");
}
