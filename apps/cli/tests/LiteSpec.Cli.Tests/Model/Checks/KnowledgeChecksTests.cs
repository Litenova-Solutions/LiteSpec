using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the knowledge variation checks: a table whose rows name a variant states its dimension, and every variant its knowledge area names holds rows in each varying table.</summary>
public sealed class KnowledgeChecksTests
{
    /// <summary>A varying table holding rows for every variant of its area passes.</summary>
    /// <proves>litespec/backend/knowledge#give-each-knowledge-area-one-model</proves>
    [Fact]
    public void VaryingTableWithEveryVariantPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TravelArea(temp.Path),
            PerDiemRates(temp.Path, true, VariantRow("50", "germany", "2026-01-01"), VariantRow("60", "netherlands", "2026-01-01")));

        Assert.Empty(SetFindings(model));
        Assert.Empty(VariationFindings(model));
    }

    /// <summary>A listed variant holding no row in a varying table reports knowledge-set-missing at tier G.</summary>
    /// <proves>litespec/backend/knowledge#give-each-knowledge-area-one-model</proves>
    [Fact]
    public void MissingVariantReportsSetMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TravelArea(temp.Path),
            PerDiemRates(temp.Path, true, VariantRow("50", "germany", "2026-01-01")));

        var finding = Assert.Single(SetFindings(model));
        Assert.Equal("G", finding.Tier);
        Assert.Equal("reference-data/travel.per-diem-rates", finding.Subject);
        Assert.Contains("holds no row of variant 'netherlands' named by knowledge area 'knowledge-area/travel'", finding.Message);
    }

    /// <summary>Rows naming a variant on a table stating no variation report knowledge-variation-undeclared at tier G.</summary>
    /// <proves>litespec/backend/knowledge#give-each-knowledge-area-one-model</proves>
    [Fact]
    public void UndeclaredVariationReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TravelArea(temp.Path),
            PerDiemRates(temp.Path, false, VariantRow("50", "germany", "2026-01-01"), VariantRow("60", "netherlands", "2026-01-01")));

        var finding = Assert.Single(VariationFindings(model));
        Assert.Equal("G", finding.Tier);
        Assert.Equal("reference-data/travel.per-diem-rates", finding.Subject);
        Assert.Contains("holds rows naming a variant while the table states no variation dimension", finding.Message);
    }

    /// <summary>A shared table under a varying area, with no dimension and no variant rows, passes.</summary>
    /// <proves>litespec/backend/knowledge#give-each-knowledge-area-one-model</proves>
    [Fact]
    public void SharedTableUnderVaryingAreaPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TravelArea(temp.Path),
            PerDiemRates(temp.Path, false, Row("50", "2026-01-01")));

        Assert.Empty(SetFindings(model));
        Assert.Empty(VariationFindings(model));
    }

    /// <summary>A module-owned table with no dimension and no variant rows passes untouched.</summary>
    /// <proves>litespec/backend/knowledge#give-each-knowledge-area-one-model</proves>
    [Fact]
    public void ModuleTablePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "module",
                "module/spending",
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "module.yaml"),
                "{\"summary\": \"Spending covers work expenses.\", \"dependsOn\": [], \"covers\": [\"Spending covers work expenses.\"], \"excludes\": [\"Spending covers nothing else.\"]}"),
            TestModel.Record(
                "reference-data",
                "reference-data/spending.decision-target",
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "reference-data", "decision-target", "decision-target.yaml"),
                "{\"summary\": \"How many business days an approval may take.\", \"rows\": [" + Row("5", "2026-01-01") + "]}"));

        Assert.Empty(SetFindings(model));
        Assert.Empty(VariationFindings(model));
    }

    /// <summary>A module-owned table stating variation reports record-invalid: only knowledge tables vary.</summary>
    /// <proves>litespec/backend/knowledge#give-each-knowledge-area-one-model</proves>
    [Fact]
    public void ModuleTableWithVariationReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "module",
                "module/spending",
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "module.yaml"),
                "{\"summary\": \"Spending covers work expenses.\", \"dependsOn\": [], \"covers\": [\"Spending covers work expenses.\"], \"excludes\": [\"Spending covers nothing else.\"]}"),
            TestModel.Record(
                "reference-data",
                "reference-data/spending.decision-target",
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "reference-data", "decision-target", "decision-target.yaml"),
                "{\"summary\": \"How many business days an approval may take.\", \"variation\": \"country\", \"rows\": [" + Row("5", "2026-01-01") + "]}"));

        var findings = Checker.Check(model).Findings
            .Where(found => found.Code == "record-invalid" && found.Subject == "reference-data/spending.decision-target")
            .ToList();
        var finding = Assert.Single(findings);
        Assert.Contains("is owned by module 'spending'", finding.Message);
    }

    /// <summary>Rows with and rows without variant in one table report record-invalid.</summary>
    /// <proves>litespec/backend/knowledge#give-each-knowledge-area-one-model</proves>
    [Fact]
    public void MixedRowsReportInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TravelArea(temp.Path),
            PerDiemRates(temp.Path, true, VariantRow("50", "germany", "2026-01-01"), Row("60", "2026-01-01")));

        var findings = Checker.Check(model).Findings
            .Where(found => found.Code == "record-invalid" && found.Subject == "reference-data/travel.per-diem-rates")
            .ToList();
        var finding = Assert.Single(findings);
        Assert.Contains("holds rows with and rows without variant", finding.Message);
    }

    /// <summary>A row naming a variant outside its area's list reports record-invalid.</summary>
    /// <proves>litespec/backend/knowledge#give-each-knowledge-area-one-model</proves>
    [Fact]
    public void StrayVariantReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TravelArea(temp.Path),
            PerDiemRates(temp.Path, true, VariantRow("50", "germany", "2026-01-01"), VariantRow("55", "france", "2026-01-01")));

        var findings = Checker.Check(model).Findings
            .Where(found => found.Code == "record-invalid" && found.Subject == "reference-data/travel.per-diem-rates")
            .ToList();
        var finding = Assert.Single(findings);
        Assert.Contains("naming variant 'france' outside the variants of knowledge area 'knowledge-area/travel'", finding.Message);
    }

    /// <summary>A table whose home names no knowledge area draws no set finding: the parent check owns the missing home.</summary>
    /// <proves>litespec/backend/knowledge#give-each-knowledge-area-one-model</proves>
    [Fact]
    public void MissingAreaDrawsNoSetFinding()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            PerDiemRates(temp.Path, true, VariantRow("50", "germany", "2026-01-01"), VariantRow("60", "netherlands", "2026-01-01")));

        Assert.Empty(SetFindings(model));
    }

    /// <summary>The knowledge-set-missing findings of a model.</summary>
    /// <param name="model">The model under test.</param>
    /// <returns>The findings whose code is knowledge-set-missing.</returns>
    static List<Finding> SetFindings(ProjectModel model) =>
        Checker.Check(model).Findings.Where(found => found.Code == "knowledge-set-missing").ToList();

    /// <summary>The knowledge-variation-undeclared findings of a model.</summary>
    /// <param name="model">The model under test.</param>
    /// <returns>The findings whose code is knowledge-variation-undeclared.</returns>
    static List<Finding> VariationFindings(ProjectModel model) =>
        Checker.Check(model).Findings.Where(found => found.Code == "knowledge-variation-undeclared").ToList();

    /// <summary>One shared reference-data row as JSON, with its value, dates and source.</summary>
    /// <param name="value">The row value.</param>
    /// <param name="from">The date the value takes effect.</param>
    /// <returns>The row entry as JSON.</returns>
    static string Row(string value, string from) =>
        "{\"value\": \"" + value + "\", \"effectiveFrom\": \"" + from + "\", \"source\": \"decision/0003-travel-rates\"}";

    /// <summary>One variant reference-data row as JSON, with its value, variant, dates and source.</summary>
    /// <param name="value">The row value.</param>
    /// <param name="variant">The variant the row belongs to.</param>
    /// <param name="from">The date the value takes effect.</param>
    /// <returns>The row entry as JSON.</returns>
    static string VariantRow(string value, string variant, string from) =>
        "{\"value\": \"" + value + "\", \"variant\": \"" + variant + "\", \"effectiveFrom\": \"" + from + "\", \"source\": \"decision/0003-travel-rates\"}";

    /// <summary>The travel knowledge area with its Germany and Netherlands variants.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The knowledge area record.</returns>
    static Record TravelArea(string root) => TestModel.Record(
        "knowledge-area",
        "knowledge-area/travel",
        Path.Combine(root, ".litespec", "behavior", "knowledge", "travel", "knowledge-area.yaml"),
        "{\"meaning\": \"The travel rates Claimwick looks up by country.\", \"variants\": [\"germany\", \"netherlands\"]}");

    /// <summary>The per-diem reference-data table of the travel area.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <param name="varies">Whether the table states its country dimension.</param>
    /// <param name="rows">The row entries as JSON.</param>
    /// <returns>The reference-data record.</returns>
    static Record PerDiemRates(string root, bool varies, params string[] rows) => TestModel.Record(
        "reference-data",
        "reference-data/travel.per-diem-rates",
        Path.Combine(root, ".litespec", "behavior", "knowledge", "travel", "reference-data", "per-diem-rates", "per-diem-rates.yaml"),
        "{\"summary\": \"The daily allowance per country.\", \"unit\": \"EUR per day\""
        + (varies ? ", \"variation\": \"country\"" : "")
        + ", \"rows\": [" + string.Join(", ", rows) + "]}");
}
