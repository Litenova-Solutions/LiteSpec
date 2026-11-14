using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves finding-source-missing: each Findings paragraph of a research explanation cites a source key with a reference link.</summary>
public sealed class FindingSourceCheckTests
{
    /// <summary>The research fields the cases share, with one source.</summary>
    const string ResearchFields = """{"sources": [{"key": "benchmark-report", "url": "https://example.com/benchmark-report", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""";

    /// <summary>A Findings paragraph with no source link reports, naming the paragraph.</summary>
    /// <proves>litespec/core/intent#state-each-research-finding-with-its-source</proves>
    [Fact]
    public void FindingWithoutSourceLinkReports()
    {
        using var temp = TestCli.Temp("checks");
        var explanationFile = Path.Combine(temp.Path, "benchmarks.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                Path.Combine(temp.Path, "benchmarks.yaml"),
                1,
                ResearchFields,
                explanationFile,
                "# Benchmarks\n\nThe benchmarks ask how long others lend.\n\n## Findings\n\nOthers lend for three weeks.\n\nThe report states it [benchmark report][benchmark-report].\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "finding-source-missing");

        Assert.Equal("A", finding.Tier);
        Assert.Equal("Research 'research/loan-period-benchmarks' states a finding with no source link: 'Others lend for three weeks.'.", finding.Message);
        Assert.Equal(explanationFile, finding.Path);
        Assert.Equal(7, finding.Line);
    }

    /// <summary>Findings whose every paragraph cites a source key report nothing, and a key in Open Questions is not a finding.</summary>
    /// <proves>litespec/core/intent#state-each-research-finding-with-its-source</proves>
    [Fact]
    public void CitedFindingsReportNothing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                Path.Combine(temp.Path, "benchmarks.yaml"),
                1,
                ResearchFields,
                Path.Combine(temp.Path, "benchmarks.md"),
                "# Benchmarks\n\nThe benchmarks ask how long others lend.\n\n## Findings\n\nThe report states three weeks [benchmark report][benchmark-report].\n\nThis suggests two weeks, but the report does not address it [benchmark report][benchmark-report].\n\n## Open Questions\n\nDo other libraries lend for longer?\n"));

        Assert.DoesNotContain(Checker.Check(model).Findings, found => found.Code == "finding-source-missing");
    }
}
