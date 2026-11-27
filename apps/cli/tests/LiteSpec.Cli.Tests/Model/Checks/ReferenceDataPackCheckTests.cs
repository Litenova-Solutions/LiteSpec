using LiteSpec.Cli.Model;
using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves that a reference-data table naming an external pack points at a file that exists.</summary>
public sealed class ReferenceDataPackCheckTests
{
    /// <summary>A pack naming no file reports.</summary>
    /// <proves>litespec/core/domain-model#bind-a-reference-data-table-to-an-external-pack</proves>
    [Fact]
    public void MissingPackReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Table(temp.Path, "data/country-pack.csv"));

        var finding = Assert.Single(Findings(model));
        Assert.Equal("G", finding.Tier);
        Assert.Equal("reference-data/spending.country", finding.Subject);
        Assert.Contains("names pack 'data/country-pack.csv'", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A pack whose file exists passes.</summary>
    /// <proves>litespec/core/domain-model#bind-a-reference-data-table-to-an-external-pack</proves>
    [Fact]
    public void ExistingPackPasses()
    {
        using var temp = TestCli.Temp("checks");
        Directory.CreateDirectory(Path.Combine(temp.Path, "data"));
        File.WriteAllText(Path.Combine(temp.Path, "data", "country-pack.csv"), "code,name\nGB,United Kingdom\n");
        var model = TestModel.Model(TestModel.Config(temp.Path), Table(temp.Path, "data/country-pack.csv"));

        Assert.Empty(Findings(model));
    }

    /// <summary>A table with no pack passes: the field is optional.</summary>
    /// <proves>litespec/core/domain-model#bind-a-reference-data-table-to-an-external-pack</proves>
    [Fact]
    public void TableWithoutPackPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Table(temp.Path, null));

        Assert.Empty(Findings(model));
    }

    /// <summary>The reference-data pack findings of a model.</summary>
    /// <param name="model">The model under test.</param>
    /// <returns>The findings whose code is reference-data-pack-missing.</returns>
    static List<Finding> Findings(ProjectModel model) =>
        Checker.Check(model).Findings.Where(found => found.Code == "reference-data-pack-missing").ToList();

    /// <summary>One reference-data table with one dated row and, when given, a pack path.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="pack">The pack path, or null for no pack.</param>
    /// <returns>The reference-data record.</returns>
    static Record Table(string root, string? pack)
    {
        var packLine = pack is null ? "" : ", \"pack\": \"" + pack + "\"";

        return TestModel.Record(
            "reference-data",
            "reference-data/spending.country",
            Path.Combine(root, ".litespec", "behavior", "modules", "spending", "reference-data", "country", "country.yaml"),
            "{\"summary\": \"Countries Claimwick serves.\", \"rows\": [{\"value\": \"GB\", \"effectiveFrom\": \"2026-01-01\", \"source\": \"decision/0001-use-countries\"}]" + packLine + "}");
    }
}
