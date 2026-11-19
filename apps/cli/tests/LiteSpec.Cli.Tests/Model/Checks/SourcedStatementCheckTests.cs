using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the sourced-statement checks against hand-built models.</summary>
public sealed class SourcedStatementCheckTests
{
    /// <summary>An obligation naming an unknown source name reports.</summary>
    /// <proves>litespec/core/intent#cite-a-source-for-each-constraint-rule</proves>
    [Fact]
    public void ObligationWithUnknownNameReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "privacy.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("constraint", "constraint/privacy-law", file, Constraint("""{"statement": "Do not share reader data.", "sources": ["unknown-law"]}""")));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "constraint-obligation-source-unknown");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Constraint 'constraint/privacy-law' names source name 'unknown-law' in obligations that its sources do not hold.", finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Equal("constraint/privacy-law", finding.Subject);
    }

    /// <summary>An obligation naming a known source name reports nothing.</summary>
    /// <proves>litespec/core/intent#cite-a-source-for-each-constraint-rule</proves>
    [Fact]
    public void ObligationWithKnownNamePasses()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "privacy.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("constraint", "constraint/privacy-law", file, Constraint("""{"statement": "Do not share reader data.", "sources": ["privacy-law"]}""")));

        Assert.DoesNotContain("constraint-obligation-source-unknown", TestModel.Codes(model));
    }

    /// <summary>A research finding naming an unknown source name reports.</summary>
    /// <proves>litespec/core/intent#state-each-research-finding-with-its-source</proves>
    [Fact]
    public void FindingWithUnknownNameReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("research", "research/loan-period-benchmarks", file, Research("""{"statement": "Others lend for three weeks.", "sources": ["unknown-report"]}""")));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "research-finding-source-unknown");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Research 'research/loan-period-benchmarks' names source name 'unknown-report' in findings that its sources do not hold.", finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Equal("research/loan-period-benchmarks", finding.Subject);
    }

    /// <summary>A research finding naming a known source name reports nothing.</summary>
    /// <proves>litespec/core/intent#state-each-research-finding-with-its-source</proves>
    [Fact]
    public void FindingWithKnownNamePasses()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("research", "research/loan-period-benchmarks", file, Research("""{"statement": "Others lend for three weeks.", "sources": ["benchmark-report"]}""")));

        Assert.DoesNotContain("research-finding-source-unknown", TestModel.Codes(model));
    }

    /// <summary>The constraint fields carrying one obligation.</summary>
    /// <param name="obligation">The obligation as JSON.</param>
    /// <returns>The record fields as JSON.</returns>
    static string Constraint(string obligation) =>
        "{\"summary\": \"Reader data is personal.\", \"authority\": \"Privacy law.\", \"sources\": [{\"name\": \"privacy-law\", \"url\": \"https://example.com/privacy-law\", \"readOn\": \"2026-01-02\"}], \"kind\": \"legal\", \"applies\": true, \"appliesBecause\": \"We hold reader data.\", \"scope\": \"Reader data.\", \"obligations\": [" + obligation + "], \"productEffect\": \"Reader data stays private.\"}";

    /// <summary>The research fields carrying one finding.</summary>
    /// <param name="finding">The finding as JSON.</param>
    /// <returns>The record fields as JSON.</returns>
    static string Research(string finding) =>
        "{\"summary\": \"Loan terms.\", \"sources\": [{\"name\": \"benchmark-report\", \"url\": \"https://example.com/report\", \"readOn\": \"2026-01-02\"}], \"question\": \"How long do others lend?\", \"conclusion\": \"Others lend for three weeks.\", \"findings\": [" + finding + "]}";
}
