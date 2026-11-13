using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the research source checks against hand-built models.</summary>
public sealed class ResearchSourceCheckTests
{
    /// <summary>A research explanation linking a URL directly reports.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact]
    public void DirectLinkReportsSourceLinkBare()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var explanationFile = Path.Combine(temp.Path, "benchmarks.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                1,
                """{"sources": [{"key": "benchmark-report", "url": "https://example.com/benchmark-report", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                explanationFile,
                "# Benchmarks\n\nThe benchmarks ask how long others lend.\n\n## Findings\n\nSee [the report](https://example.com/benchmark-report) for numbers.\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "source-link-bare");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Research explanation links 'https://example.com/benchmark-report' directly; cite a source key instead.", finding.Message);
        Assert.Equal(explanationFile, finding.Path);
        Assert.Equal(7, finding.Line);
        Assert.Equal("research/loan-period-benchmarks", finding.Subject);
    }

    /// <summary>A research explanation naming a bare URL reports.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact]
    public void BareUrlReportsSourceLinkBare()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var explanationFile = Path.Combine(temp.Path, "benchmarks.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                1,
                """{"sources": [{"key": "benchmark-report", "url": "https://example.com/benchmark-report", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                explanationFile,
                "# Benchmarks\n\nThe benchmarks ask how long others lend.\n\n## Findings\n\nSee https://example.com/benchmark-report for numbers.\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "source-link-bare");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Research explanation links 'https://example.com/benchmark-report' directly; cite a source key instead.", finding.Message);
        Assert.Equal(explanationFile, finding.Path);
        Assert.Equal(7, finding.Line);
        Assert.Equal("research/loan-period-benchmarks", finding.Subject);
    }

    /// <summary>A research explanation naming an angle-bracket URL reports.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact]
    public void AngleUrlReportsSourceLinkBare()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var explanationFile = Path.Combine(temp.Path, "benchmarks.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                1,
                """{"sources": [{"key": "benchmark-report", "url": "https://example.com/benchmark-report", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                explanationFile,
                "# Benchmarks\n\nThe benchmarks ask how long others lend.\n\n## Findings\n\nSee <https://example.com/benchmark-report> for numbers.\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "source-link-bare");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Research explanation links 'https://example.com/benchmark-report' directly; cite a source key instead.", finding.Message);
        Assert.Equal(explanationFile, finding.Path);
        Assert.Equal(7, finding.Line);
        Assert.Equal("research/loan-period-benchmarks", finding.Subject);
    }

    /// <summary>A URL inside code reports no bare link.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact]
    public void UrlInsideCodePasses()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var explanationFile = Path.Combine(temp.Path, "benchmarks.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                1,
                """{"sources": [{"key": "benchmark-report", "url": "https://example.com/benchmark-report", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                explanationFile,
                "# Benchmarks\n\nThe benchmarks ask how long others lend.\n\n## Findings\n\nSee `https://example.com/benchmark-report` for numbers [per the report][benchmark-report].\n"));

        Assert.DoesNotContain("source-link-bare", TestModel.Codes(model));
    }

    /// <summary>A reference link whose label shows the URL cites by key and reports no bare link.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact]
    public void UrlAsReferenceLinkLabelPasses()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var explanationFile = Path.Combine(temp.Path, "benchmarks.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                1,
                """{"sources": [{"key": "benchmark-report", "url": "https://example.com/benchmark-report", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                explanationFile,
                "# Benchmarks\n\nThe benchmarks ask how long others lend.\n\n## Findings\n\nOthers lend for three weeks [https://example.com/benchmark-report][benchmark-report].\n"));

        var codes = TestModel.Codes(model);
        Assert.DoesNotContain("source-link-bare", codes);
        Assert.DoesNotContain("source-unused", codes);
    }

    /// <summary>A research explanation citing an unknown source key reports.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact]
    public void UnknownSourceKeyReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var explanationFile = Path.Combine(temp.Path, "benchmarks.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                1,
                """{"sources": [{"key": "benchmark-report", "url": "https://example.com/benchmark-report", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                explanationFile,
                "# Benchmarks\n\nThe benchmarks ask how long others lend.\n\n## Findings\n\nOthers lend for three weeks [per the report][no-such-key].\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "source-key-unknown");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Research explanation cites source key 'no-such-key', which the record's sources do not name.", finding.Message);
        Assert.Equal(explanationFile, finding.Path);
        Assert.Equal(7, finding.Line);
        Assert.Equal("research/loan-period-benchmarks", finding.Subject);
    }

    /// <summary>A research explanation citing a declared source key passes.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact]
    public void KnownSourceKeyPasses()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var explanationFile = Path.Combine(temp.Path, "benchmarks.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                1,
                """{"sources": [{"key": "benchmark-report", "url": "https://example.com/benchmark-report", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                explanationFile,
                "# Benchmarks\n\nThe benchmarks ask how long others lend.\n\n## Findings\n\nOthers lend for three weeks [per the report][benchmark-report].\n"));

        Assert.DoesNotContain("source-link-bare", TestModel.Codes(model));
        Assert.DoesNotContain("source-key-unknown", TestModel.Codes(model));
        Assert.DoesNotContain("source-unused", TestModel.Codes(model));
    }

    /// <summary>A research source cited nowhere in its explanation reports.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact]
    public void UncitedSourceReportsSourceUnused()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var explanationFile = Path.Combine(temp.Path, "benchmarks.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                7,
                """{"sources": [{"key": "used-key", "url": "https://example.com/used", "readOn": "2026-10-12"}, {"key": "unused-key", "url": "https://example.com/unused", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                explanationFile,
                "# Benchmarks\n\nThe benchmarks ask how long others lend.\n\n## Findings\n\nOthers lend for three weeks [per the report][used-key].\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "source-unused");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Research 'research/loan-period-benchmarks' names source key 'unused-key', which its explanation cites nowhere.", finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Equal(7, finding.Line);
        Assert.Equal("research/loan-period-benchmarks", finding.Subject);
    }

    /// <summary>A research record with no explanation skips the source checks.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact]
    public void ResearchWithoutAnExplanationSkipsSourceChecks()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                Path.Combine(temp.Path, "benchmarks.yaml"),
                """{"sources": [{"key": "benchmark-report", "url": "https://example.com/benchmark-report", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}"""));

        Assert.DoesNotContain("source-link-bare", TestModel.Codes(model));
        Assert.DoesNotContain("source-key-unknown", TestModel.Codes(model));
        Assert.DoesNotContain("source-unused", TestModel.Codes(model));
    }
}
