using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the column mapping check: row construction a table's rows cannot build reports reference-data-column-invalid.</summary>
public sealed class ReferenceDataColumnsCheckTests
{
    /// <summary>A knowledge table mapping each parameter from a column its rows hold passes.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ValidKnowledgeTablePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            Table(temp.Path, """{"title": "Per Diem Rates", "summary": "The daily allowance.", "unit": "EUR per day", "requiredRoles": ["full-day", "partial-day"], "variation": "country",""" + RowConstruction() + """, "rows": [""" + Row(30, "de-full-day-2026", "full-day", "germany") + ", " + Row(15, "de-partial-day-2026", "partial-day", "germany") + ", " + Row(25, "nl-full-day-2026", "full-day", "netherlands") + ", " + Row(12, "nl-partial-day-2026", "partial-day", "netherlands") + "]}"));

        Assert.Empty(Findings(model, "reference-data-column-invalid"));
    }

    /// <summary>A table stating row without columns reports reference-data-column-invalid at tier G.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void RowWithoutColumnsReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            Table(temp.Path, """{"title": "Per Diem Rates", "summary": "The daily allowance.", "row": {"type": "Claimwick.Travel.PerDiem.PerDiemRate"}, "rows": [""" + Row(30, "de-full-day-2026", "full-day", "germany") + "]}"));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Equal("G", finding.Tier);
        Assert.Equal("reference-data/travel.per-diem-rates", finding.Subject);
        Assert.Contains("states `row` without `columns`", finding.Message);
    }

    /// <summary>A table stating columns without row reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ColumnsWithoutRowReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            Table(temp.Path, """{"title": "Per Diem Rates", "summary": "The daily allowance.", "columns": [{"column": "value", "parameter": "dailyAmount", "as": "money", "type": "Claimwick.Kernel.Values.Money", "currency": "EUR"}], "rows": [""" + Row(30, "de-full-day-2026", "full-day", "germany") + "]}"));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("states `columns` without `row`", finding.Message);
    }

    /// <summary>A table no knowledge area owns reports reference-data-column-invalid when it states row construction.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ColumnsOnUnownedTableReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Table(temp.Path, """{"title": "Per Diem Rates", "summary": "The daily allowance.", """ + RowConstruction() + """, "rows": [""" + Row(30, "de-full-day-2026", "full-day", "germany") + "]}"));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("while no knowledge area owns it", finding.Message);
    }

    /// <summary>A column entry reading a column no row holds reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UnknownColumnReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(temp.Path, """{"column": "colour", "parameter": "dailyAmount", "as": "string"}""", Row(30, "de-full-day-2026", "full-day", "germany")));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("reading column 'colour', which no row holds", finding.Message);
    }

    /// <summary>A column entry with a rendering outside the eight reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UnknownRenderingReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(temp.Path, """{"column": "value", "parameter": "dailyAmount", "as": "furlong"}""", Row(30, "de-full-day-2026", "full-day", "germany")));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("with rendering 'furlong' outside", finding.Message);
    }

    /// <summary>Two column entries sharing one parameter report reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void DuplicateParameterReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "value", "parameter": "dailyAmount", "as": "number"}, {"column": "key", "parameter": "dailyAmount", "as": "string"}""",
                Row(30, "de-full-day-2026", "full-day", "germany")));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("holds parameter 'dailyAmount' on column entries 0 and 1", finding.Message);
    }

    /// <summary>A money column without a currency reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MoneyWithoutCurrencyReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(temp.Path, """{"column": "value", "parameter": "dailyAmount", "as": "money", "type": "Claimwick.Kernel.Values.Money"}""", Row(30, "de-full-day-2026", "full-day", "germany")));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("without a three-letter currency", finding.Message);
    }

    /// <summary>A money column with a currency outside three uppercase letters reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MalformedCurrencyReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(temp.Path, """{"column": "value", "parameter": "dailyAmount", "as": "money", "type": "Claimwick.Kernel.Values.Money", "currency": "eur"}""", Row(30, "de-full-day-2026", "full-day", "germany")));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("with currency 'eur' outside three uppercase letters", finding.Message);
    }

    /// <summary>A key column without a model type reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void KeyWithoutTypeReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(temp.Path, """{"column": "key", "parameter": "key", "as": "key"}""", Row(30, "de-full-day-2026", "full-day", "germany")));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("without a qualified model type", finding.Message);
    }

    /// <summary>A key column with a factory outside the five the analyzers allow reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UnknownKeyFactoryReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(temp.Path, """{"column": "key", "parameter": "key", "as": "key", "type": "Claimwick.Travel.PerDiem.PerDiemRateKey", "factory": "Build"}""", Row(30, "de-full-day-2026", "full-day", "germany")));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("with key factory 'Build' outside", finding.Message);
    }

    /// <summary>A map column without a value map reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MapWithoutLiteralsReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(temp.Path, """{"column": "variant", "parameter": "country", "as": "map"}""", Row(30, "de-full-day-2026", "full-day", "germany")));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("without a value map", finding.Message);
    }

    /// <summary>A row whose variant the value map skips reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MapSkippingVariantReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "variant", "parameter": "country", "as": "map", "literals": {"germany": "DE"}}""",
                Row(30, "de-full-day-2026", "full-day", "germany") + ", " + Row(25, "nl-full-day-2026", "full-day", "netherlands")));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("with variant 'netherlands' the value map of entry 0 skips", finding.Message);
    }

    /// <summary>A money column reading a text value reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MoneyOnTextValueReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "value", "parameter": "dailyAmount", "as": "money", "type": "Claimwick.Kernel.Values.Money", "currency": "EUR"}""",
                """{"value": "high", "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("with value 'high' of the wrong kind for a money column", finding.Message);
    }

    /// <summary>A row naming no variant in a varying table reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void RowWithoutVariantReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "value", "parameter": "dailyAmount", "as": "number"}""",
                """{"value": 30, "key": "de-full-day-2026", "role": "full-day", "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("naming no variant while the table varies by 'country'", finding.Message);
    }

    /// <summary>A row construction naming an unqualified row type reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UnqualifiedRowTypeReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            Table(temp.Path, """{"title": "Per Diem Rates", "summary": "The daily allowance.", "row": {"type": "PerDiemRate"}, "columns": [{"column": "value", "parameter": "dailyAmount", "as": "number"}], "rows": [""" + Row(30, "de-full-day-2026", "full-day", "germany") + "]}"));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("without a qualified model row type", finding.Message);
    }

    /// <summary>A knowledge table mapping extra fields through decimal, case and object columns passes.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ValidExtraAndObjectPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "key", "parameter": "key", "as": "key", "type": "Claimwick.Travel.PerDiem.PerDiemRateKey", "factory": "Parse"}, """
                + """{"column": "role", "parameter": "role", "as": "case", "type": "Claimwick.Travel.PerDiem.PerDiemRole"}, """
                + """{"column": "ratePercent", "parameter": "rate", "as": "decimal"}, """
                + """{"parameter": "window", "as": "object", "type": "Claimwick.Travel.PerDiem.RenewalWindow", "columns": ["""
                + """{"column": "windowDays", "parameter": "days", "as": "number"}, """
                + """{"column": "windowEndsOn", "parameter": "endsOn", "as": "date"}]}, """
                + """{"column": "source", "parameter": "source", "as": "string"}""",
                """{"value": 30, "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "ratePercent": 19, "windowDays": 3, "windowEndsOn": "2026-12-31", "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        Assert.Empty(Findings(model, "reference-data-column-invalid"));
    }

    /// <summary>A leaf entry without a column reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void LeafWithoutColumnReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(temp.Path, """{"parameter": "dailyAmount", "as": "number"}""", Row(30, "de-full-day-2026", "full-day", "germany")));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("without a column", finding.Message);
    }

    /// <summary>A case column without a model type reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void CaseWithoutTypeReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(temp.Path, """{"column": "role", "parameter": "role", "as": "case"}""", Row(30, "de-full-day-2026", "full-day", "germany")));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("without a qualified model type", finding.Message);
    }

    /// <summary>An object entry without a model type reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ObjectWithoutTypeReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"parameter": "window", "as": "object", "columns": [{"column": "value", "parameter": "days", "as": "number"}]}""",
                Row(30, "de-full-day-2026", "full-day", "germany")));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("name the object type with its namespace", finding.Message);
    }

    /// <summary>An object entry without sub-columns reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ObjectWithoutSubColumnsReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"parameter": "window", "as": "object", "type": "Claimwick.Travel.PerDiem.RenewalWindow"}""",
                Row(30, "de-full-day-2026", "full-day", "germany")));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("without sub-columns", finding.Message);
    }

    /// <summary>An object entry naming a row field reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ObjectNamingColumnReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "value", "parameter": "window", "as": "object", "type": "Claimwick.Travel.PerDiem.RenewalWindow", "columns": [{"column": "value", "parameter": "days", "as": "number"}]}""",
                Row(30, "de-full-day-2026", "full-day", "germany")));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("which no object reads", finding.Message);
    }

    /// <summary>An extra row field no column reads reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UnreadExtraFieldReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "value", "parameter": "dailyAmount", "as": "number"}""",
                """{"value": 30, "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "labelNl": "Dagvergoeding", "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("extra field 'labelNl' no column reads", finding.Message);
    }

    /// <summary>Two sub-columns sharing one parameter report reference-data-column-invalid with their dotted entries.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void NestedDuplicateParameterReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"parameter": "window", "as": "object", "type": "Claimwick.Travel.PerDiem.RenewalWindow", "columns": [{"column": "value", "parameter": "days", "as": "number"}, {"column": "key", "parameter": "days", "as": "string"}]}""",
                Row(30, "de-full-day-2026", "full-day", "germany")));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("on column entries 0.0 and 0.1", finding.Message);
    }

    /// <summary>A decimal column reading a text value reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void DecimalOnTextValueReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "value", "parameter": "rate", "as": "decimal"}""",
                """{"value": "high", "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("of the wrong kind for a decimal column", finding.Message);
    }

    /// <summary>A singleton table mapping scalar, list and object-list parameters from its one keyless row passes.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ValidListsAndSingletonPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableSingleton(
                temp.Path,
                """{"column": "value", "parameter": "dailyCap", "as": "number"}, """
                + """{"column": "channels", "parameter": "channels", "as": "list", "items": "string"}, """
                + """{"column": "bands", "parameter": "bands", "as": "list", "items": "object", "type": "Claimwick.Travel.PerDiem.FineSlice", "columns": ["""
                + """{"column": "minDays", "parameter": "minDays", "as": "number"}, """
                + """{"column": "maxDays", "parameter": "maxDays", "as": "number"}]}, """
                + """{"column": "source", "parameter": "source", "as": "string"}""",
                """{"value": 30, "variant": "germany", "channels": ["email"], "bands": [{"minDays": 1, "maxDays": 7}], "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        Assert.Empty(Findings(model, "reference-data-column-invalid"));
    }

    /// <summary>A table stating singleton without row and columns reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SingletonWithoutRowReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            Table(temp.Path, """{"title": "Per Diem Rates", "summary": "The daily allowance.", "singleton": true, "rows": [""" + Row(30, "de-full-day-2026", "full-day", "germany") + "]}"));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("states `singleton` without `row` and `columns`", finding.Message);
    }

    /// <summary>A singleton row stating a key reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SingletonRowWithKeyReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableSingleton(
                temp.Path,
                """{"column": "value", "parameter": "dailyCap", "as": "number"}""",
                Row(30, "de-full-day-2026", "full-day", "germany")));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("with key 'de-full-day-2026' while the table is a singleton", finding.Message);
    }

    /// <summary>A singleton variant holding two rows reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SingletonTwoRowsReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableSingleton(
                temp.Path,
                """{"column": "value", "parameter": "dailyCap", "as": "number"}""",
                """{"value": 30, "variant": "germany", "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}, """
                + """{"value": 28, "variant": "germany", "effectiveFrom": "2027-01-01", "source": "decision/0003-travel-rates"}"""));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("holds 2 rows of variant 'germany'", finding.Message);
    }

    /// <summary>A list entry without items reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ListWithoutItemsReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "channels", "parameter": "channels", "as": "list"}""",
                """{"value": 30, "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "channels": ["email"], "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("without items; name how each item renders", finding.Message);
    }

    /// <summary>A list entry with items outside the item renderings reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UnknownItemsReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "channels", "parameter": "channels", "as": "list", "items": "furlong"}""",
                """{"value": 30, "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "channels": ["email"], "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("with items 'furlong' outside", finding.Message);
    }

    /// <summary>A list entry reading a scalar value reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ListReadingScalarReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "channels", "parameter": "channels", "as": "list", "items": "string"}""",
                """{"value": 30, "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "channels": "email", "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("with channels 'email' of the wrong kind for a list column", finding.Message);
    }

    /// <summary>An item of the wrong kind reports reference-data-column-invalid with its row and item numbers.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ItemOfWrongKindReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "channels", "parameter": "channels", "as": "list", "items": "string"}""",
                """{"value": 30, "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "channels": ["email", 5], "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("holds row 0 item 1 with 5 of the wrong kind for a string item", finding.Message);
    }

    /// <summary>A mapped item the value map skips reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MapItemSkippedReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "branchCodes", "parameter": "branchCodes", "as": "list", "items": "map", "literals": {"central": "CT"}}""",
                """{"value": 30, "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "branchCodes": ["harbor"], "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("item 0 with 'harbor' the value map of entry 0 skips", finding.Message);
    }

    /// <summary>An item field no sub-column reads reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UnreadItemFieldReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "bands", "parameter": "bands", "as": "list", "items": "object", "type": "Claimwick.Travel.PerDiem.FineSlice", "columns": [{"column": "minDays", "parameter": "minDays", "as": "number"}, {"column": "maxDays", "parameter": "maxDays", "as": "number"}]}""",
                """{"value": 30, "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "bands": [{"minDays": 1, "maxDays": 7, "maxx": 2}], "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("holds row 0 item 0 with field 'maxx' no sub-column of entry 0 reads", finding.Message);
    }

    /// <summary>An item field of the wrong kind reports reference-data-column-invalid with its row and item numbers.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ItemFieldOfWrongKindReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "bands", "parameter": "bands", "as": "list", "items": "object", "type": "Claimwick.Travel.PerDiem.FineSlice", "columns": [{"column": "minDays", "parameter": "minDays", "as": "number"}]}""",
                """{"value": 30, "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "bands": [{"minDays": "soon"}], "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("holds row 0 item 0 with minDays 'soon' of the wrong kind for a number column", finding.Message);
    }

    /// <summary>A sub-column reading a field no item holds reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SubColumnUnknownFieldReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "bands", "parameter": "bands", "as": "list", "items": "object", "type": "Claimwick.Travel.PerDiem.FineSlice", "columns": [{"column": "minWeeks", "parameter": "minDays", "as": "number"}]}""",
                """{"value": 30, "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "bands": [{"minDays": 1}], "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("reading column 'minWeeks', which no item holds", finding.Message);
    }

    /// <summary>A list entry with object items but no sub-columns reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ListObjectWithoutSubColumnsReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "bands", "parameter": "bands", "as": "list", "items": "object", "type": "Claimwick.Travel.PerDiem.FineSlice"}""",
                """{"value": 30, "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "bands": [{"minDays": 1}], "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("without sub-columns; list the nested parameters building each 'bands' item", finding.Message);
    }

    /// <summary>A list entry with object items but no model type reports reference-data-column-invalid.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ListObjectWithoutTypeReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "bands", "parameter": "bands", "as": "list", "items": "object", "columns": [{"column": "minDays", "parameter": "minDays", "as": "number"}]}""",
                """{"value": 30, "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "bands": [{"minDays": 1}], "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("without a qualified model type; name the item type with its namespace", finding.Message);
    }

    /// <summary>A singleton table with no rows yet reports reference-data-column-invalid: a singleton holds one row per variant.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void EmptySingletonReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableSingleton(temp.Path, """{"column": "value", "parameter": "dailyCap", "as": "number"}""", ""));

        var finding = Assert.Single(Findings(model, "reference-data-column-invalid"));
        Assert.Contains("holds no rows; a singleton table holds one row per variant", finding.Message);
    }

    /// <summary>A table with no rows yet and extra-field columns passes: the declared mapping stands with nothing to judge it against.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void EmptyTableWithExtraColumnsPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "value", "parameter": "dailyAmount", "as": "number"}, {"column": "labelNl", "parameter": "dutchLabel", "as": "string"}""",
                ""));

        Assert.Empty(Findings(model, "reference-data-column-invalid"));
    }

    /// <summary>A column whose rows all state null passes: a stated null renders null.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void AllNullColumnPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "value", "parameter": "dailyAmount", "as": "number"}, {"column": "labelNl", "parameter": "dutchLabel", "as": "string"}""",
                """{"value": 30, "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "labelNl": null, "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        Assert.Empty(Findings(model, "reference-data-column-invalid"));
    }

    /// <summary>A list whose items state null passes: a stated null item renders null.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void NullListItemsPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "channels", "parameter": "channels", "as": "list", "items": "string"}""",
                """{"value": 30, "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "channels": [null], "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        Assert.Empty(Findings(model, "reference-data-column-invalid"));
    }

    /// <summary>An object list empty in every row passes: the sub-columns stand with no items to judge them against.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void AllEmptyObjectListsPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            TableWith(
                temp.Path,
                """{"column": "bands", "parameter": "bands", "as": "list", "items": "object", "type": "Claimwick.Travel.PerDiem.FineSlice", "columns": [{"column": "minDays", "parameter": "minDays", "as": "number"}, {"column": "maxDays", "parameter": "maxDays", "as": "number"}]}""",
                """{"value": 30, "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "bands": [], "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}"""));

        Assert.Empty(Findings(model, "reference-data-column-invalid"));
    }

    /// <summary>A row stating no start date reports record-invalid naming the earliest date the source covers.</summary>
    /// <proves>litespec/core/domain-model#date-every-reference-data-row</proves>
    [Fact]
    public void RowWithoutStartReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Area(temp.Path),
            Table(temp.Path, """{"title": "Per Diem Rates", "summary": "The daily allowance.", "rows": [""" + """{"value": 30, "key": "de-full-day-2026", "role": "full-day", "variant": "germany", "source": "decision/0003-travel-rates"}""" + "]}"));

        var finding = Assert.Single(Findings(model, "record-invalid"));
        Assert.Contains("holds row 0 with no start date; state in `effectiveFrom` the earliest date the source covers", finding.Message);
    }

    /// <summary>The findings of a model that carry one code.</summary>
    /// <param name="model">The model under test.</param>
    /// <param name="code">The finding code.</param>
    /// <returns>The findings carrying the code.</returns>
    static List<Finding> Findings(ProjectModel model, string code) =>
        Checker.Check(model).Findings.Where(found => found.Code == code).ToList();

    /// <summary>The travel knowledge area owning the per-diem table.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The knowledge-area record.</returns>
    static Record Area(string root) => TestModel.Record(
        "knowledge-area",
        "knowledge-area/travel",
        Path.Combine(root, ".litespec", "behavior", "knowledge", "travel", "knowledge-area.yaml"),
        """{"title": "Travel", "meaning": "The travel rates Claimwick looks up by country.", "variants": ["germany", "netherlands"]}""");

    /// <summary>One reference-data table of the travel area.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <param name="json">The table fields as JSON.</param>
    /// <returns>The reference-data record.</returns>
    static Record Table(string root, string json) => TestModel.Record(
        "reference-data",
        "reference-data/travel.per-diem-rates",
        Path.Combine(root, ".litespec", "behavior", "knowledge", "travel", "reference-data", "per-diem-rates", "per-diem-rates.yaml"),
        json);

    /// <summary>One reference-data table of the travel area with one row construction and the given columns and rows.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <param name="columns">The column entries as JSON.</param>
    /// <param name="rows">The row entries as JSON.</param>
    /// <returns>The reference-data record.</returns>
    static Record TableWith(string root, string columns, string rows) => Table(
        root,
        """{"title": "Per Diem Rates", "summary": "The daily allowance.", "variation": "country", "row": {"type": "Claimwick.Travel.PerDiem.PerDiemRate"}, "columns": [""" + columns + """], "rows": [""" + rows + "]}");

    /// <summary>One singleton reference-data table of the travel area with one row construction and the given columns and rows.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <param name="columns">The column entries as JSON.</param>
    /// <param name="rows">The row entries as JSON.</param>
    /// <returns>The reference-data record.</returns>
    static Record TableSingleton(string root, string columns, string rows) => Table(
        root,
        """{"title": "Per Diem Rates", "summary": "The daily allowance.", "variation": "country", "singleton": true, "row": {"type": "Claimwick.Travel.PerDiem.PerDiemRate"}, "columns": [""" + columns + """], "rows": [""" + rows + "]}");

    /// <summary>The row construction of the per-diem table: one rate per row with its country, key, role, amount, dates and source.</summary>
    /// <returns>The row and columns fields as JSON.</returns>
    static string RowConstruction() => """" "row": {"type": "Claimwick.Travel.PerDiem.PerDiemRate", "factory": "Create"}, "columns": [""""
        + """{"column": "variant", "parameter": "country", "as": "map", "literals": {"germany": "DE", "netherlands": "NL"}}, """
        + """{"column": "key", "parameter": "key", "as": "key", "type": "Claimwick.Travel.PerDiem.PerDiemRateKey"}, """
        + """{"column": "role", "parameter": "role", "as": "member", "type": "Claimwick.Travel.PerDiem.PerDiemRole"}, """
        + """{"column": "value", "parameter": "dailyAmount", "as": "money", "type": "Claimwick.Kernel.Values.Money", "currency": "EUR"}, """
        + """{"column": "effectiveFrom", "parameter": "validFrom", "as": "date"}, """
        + """{"column": "effectiveTo", "parameter": "validTo", "as": "date"}, """
        + """{"column": "source", "parameter": "source", "as": "string"}]""";

    /// <summary>One per-diem row as JSON, with its value, key, role, variant, start date and source.</summary>
    /// <param name="value">The row value.</param>
    /// <param name="key">The stable key of the row.</param>
    /// <param name="role">The role the row plays.</param>
    /// <param name="variant">The variant the row belongs to.</param>
    /// <returns>The row entry as JSON.</returns>
    static string Row(int value, string key, string role, string variant) =>
        $$"""{"value": {{value}}, "key": "{{key}}", "role": "{{role}}", "variant": "{{variant}}", "effectiveFrom": "2026-01-01", "source": "decision/0003-travel-rates"}""";
}
