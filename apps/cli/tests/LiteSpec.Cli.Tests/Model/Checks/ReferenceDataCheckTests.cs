using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the reference-data period check: no two rows of one table overlap in time, and no row ends no later than it starts.</summary>
public sealed class ReferenceDataCheckTests
{
    /// <summary>Two disjoint rows pass the period rule.</summary>
    /// <proves>litespec/core/domain-model#keep-reference-data-rows-from-overlapping</proves>
    [Fact]
    public void DisjointRowsPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            DecisionTarget(temp.Path, Row("5", "2026-01-01", "2026-12-31"), Row("3", "2027-01-01", null)));

        Assert.Empty(PeriodFindings(model));
    }

    /// <summary>Two overlapping rows report reference-data-period-invalid at tier G.</summary>
    /// <proves>litespec/core/domain-model#keep-reference-data-rows-from-overlapping</proves>
    [Fact]
    public void OverlappingRowsReportPeriodInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            DecisionTarget(temp.Path, Row("5", "2026-01-01", "2026-12-31"), Row("3", "2026-06-01", "2027-06-30")));

        var finding = Assert.Single(PeriodFindings(model));
        Assert.Equal("G", finding.Tier);
        Assert.Equal("reference-data/spending.decision-target", finding.Subject);
        Assert.Contains("rows 0 and 1 overlapping in time", finding.Message);
    }

    /// <summary>A row without an end stays open, so it overlaps any later row.</summary>
    /// <proves>litespec/core/domain-model#keep-reference-data-rows-from-overlapping</proves>
    [Fact]
    public void OpenEndedRowOverlapsALaterRow()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            DecisionTarget(temp.Path, Row("5", "2026-01-01", null), Row("3", "2027-01-01", null)));

        var finding = Assert.Single(PeriodFindings(model));
        Assert.Contains("rows 0 and 1 overlapping in time", finding.Message);
    }

    /// <summary>A row ending on its start reports, because the end is no later than the start.</summary>
    /// <proves>litespec/core/domain-model#keep-reference-data-rows-from-overlapping</proves>
    [Fact]
    public void RowEndingOnItsStartReportsPeriodInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            DecisionTarget(temp.Path, Row("5", "2026-01-01", "2026-01-01")));

        var finding = Assert.Single(PeriodFindings(model));
        Assert.Contains("row 0 ending '2026-01-01' no later than its start '2026-01-01'", finding.Message);
    }

    /// <summary>An inverted row draws one finding: its own period finding, not an overlap finding beside it.</summary>
    /// <proves>litespec/core/domain-model#keep-reference-data-rows-from-overlapping</proves>
    [Fact]
    public void InvertedRowDrawsOneFinding()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            DecisionTarget(temp.Path, Row("5", "2026-06-01", "2026-02-01"), Row("3", "2026-01-01", "2026-12-31")));

        var finding = Assert.Single(PeriodFindings(model));
        Assert.Contains("row 0 ending '2026-02-01' no later than its start '2026-06-01'", finding.Message);
    }

    /// <summary>Two overlapping rows of different roles in one variant pass: each role runs its own timeline.</summary>
    /// <proves>litespec/core/domain-model#keep-reference-data-rows-from-overlapping</proves>
    [Fact]
    public void OverlappingRowsOfDifferentRolesInOneVariantPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            DecisionTarget(
                temp.Path,
                KeyedRow("28", "de-full-day-2026", "full-day", "germany", "2026-01-01", null),
                KeyedRow("15", "de-partial-day-2026", "partial-day", "germany", "2026-01-01", null)));

        Assert.Empty(PeriodFindings(model));
    }

    /// <summary>The reference-data period findings of a model.</summary>
    /// <param name="model">The model under test.</param>
    /// <returns>The findings whose code is reference-data-period-invalid.</returns>
    static List<Finding> PeriodFindings(ProjectModel model) =>
        Checker.Check(model).Findings.Where(found => found.Code == "reference-data-period-invalid").ToList();

    /// <summary>One reference-data row as JSON, with its value, dates and source.</summary>
    /// <param name="value">The row value.</param>
    /// <param name="from">The date the value takes effect.</param>
    /// <param name="to">The date the value stops applying, or null while it stays in force.</param>
    /// <returns>The row entry as JSON.</returns>
    static string Row(string value, string from, string? to) =>
        "{\"value\": \"" + value + "\", \"effectiveFrom\": \"" + from + "\""
        + (to is null ? "" : ", \"effectiveTo\": \"" + to + "\"")
        + ", \"source\": \"research/approval-time-benchmarks\"}";

    /// <summary>One reference-data row as JSON, with its key, role, variant, dates and source.</summary>
    /// <param name="value">The row value.</param>
    /// <param name="key">The row key.</param>
    /// <param name="role">The role the row plays.</param>
    /// <param name="variant">The variant the row belongs to.</param>
    /// <param name="from">The date the value takes effect.</param>
    /// <param name="to">The date the value stops applying, or null while it stays in force.</param>
    /// <returns>The row entry as JSON.</returns>
    static string KeyedRow(string value, string key, string role, string variant, string from, string? to) =>
        "{\"value\": \"" + value + "\", \"key\": \"" + key + "\", \"role\": \"" + role + "\", \"variant\": \"" + variant
        + "\", \"effectiveFrom\": \"" + from + "\""
        + (to is null ? "" : ", \"effectiveTo\": \"" + to + "\"")
        + ", \"source\": \"research/approval-time-benchmarks\"}";

    /// <summary>The decision-target reference-data table of the spending module.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <param name="rows">The row entries as JSON.</param>
    /// <returns>The reference-data record.</returns>
    static Record DecisionTarget(string root, params string[] rows) => TestModel.Record(
        "reference-data",
        "reference-data/spending.decision-target",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "reference-data", "decision-target", "decision-target.yaml"),
        "{\"summary\": \"How many business days an approval may take.\", \"rows\": [" + string.Join(", ", rows) + "]}");
}
