using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the research source checks against hand-built models.</summary>
public sealed class ResearchSourceCheckTests
{
    /// <summary>A research explanation linking a URL directly reports.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact]
    public void Direct_Link_Reports_Source_Link_Bare()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var explanationFile = Path.Combine(temp.Path, "benchmarks.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                1,
                """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "sources": [{"key": "benchmark-report", "url": "https://example.com/benchmark-report", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                explanationFile,
                "# Benchmarks\n\n## Answer\n\nSee [the report](https://example.com/benchmark-report) for numbers.\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "source-link-bare");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Research explanation links 'https://example.com/benchmark-report' directly; cite a source key instead.", finding.Message);
        Assert.Equal(explanationFile, finding.Path);
        Assert.Equal(5, finding.Line);
        Assert.Equal("research/loan-period-benchmarks", finding.Subject);
    }

    /// <summary>A research explanation naming a bare URL reports.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact]
    public void Bare_Url_Reports_Source_Link_Bare()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var explanationFile = Path.Combine(temp.Path, "benchmarks.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                1,
                """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "sources": [{"key": "benchmark-report", "url": "https://example.com/benchmark-report", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                explanationFile,
                "# Benchmarks\n\n## Answer\n\nSee https://example.com/benchmark-report for numbers.\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "source-link-bare");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Research explanation links 'https://example.com/benchmark-report' directly; cite a source key instead.", finding.Message);
        Assert.Equal(explanationFile, finding.Path);
        Assert.Equal(5, finding.Line);
        Assert.Equal("research/loan-period-benchmarks", finding.Subject);
    }

    /// <summary>A research explanation naming an angle-bracket URL reports.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact]
    public void Angle_Url_Reports_Source_Link_Bare()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var explanationFile = Path.Combine(temp.Path, "benchmarks.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                1,
                """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "sources": [{"key": "benchmark-report", "url": "https://example.com/benchmark-report", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                explanationFile,
                "# Benchmarks\n\n## Answer\n\nSee <https://example.com/benchmark-report> for numbers.\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "source-link-bare");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Research explanation links 'https://example.com/benchmark-report' directly; cite a source key instead.", finding.Message);
        Assert.Equal(explanationFile, finding.Path);
        Assert.Equal(5, finding.Line);
        Assert.Equal("research/loan-period-benchmarks", finding.Subject);
    }

    /// <summary>A URL inside code reports no bare link.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact]
    public void Url_Inside_Code_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var explanationFile = Path.Combine(temp.Path, "benchmarks.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                1,
                """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "sources": [{"key": "benchmark-report", "url": "https://example.com/benchmark-report", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                explanationFile,
                "# Benchmarks\n\n## Answer\n\nSee `https://example.com/benchmark-report` for numbers [per the report][benchmark-report].\n"));

        Assert.DoesNotContain("source-link-bare", TestModel.Codes(model));
    }

    /// <summary>A research explanation citing an unknown source key reports.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact]
    public void Unknown_Source_Key_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var explanationFile = Path.Combine(temp.Path, "benchmarks.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                1,
                """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "sources": [{"key": "benchmark-report", "url": "https://example.com/benchmark-report", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                explanationFile,
                "# Benchmarks\n\n## Answer\n\nOthers lend for three weeks [per the report][no-such-key].\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "source-key-unknown");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Research explanation cites source key 'no-such-key', which the record's sources do not name.", finding.Message);
        Assert.Equal(explanationFile, finding.Path);
        Assert.Equal(5, finding.Line);
        Assert.Equal("research/loan-period-benchmarks", finding.Subject);
    }

    /// <summary>A research explanation citing a declared source key passes.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact]
    public void Known_Source_Key_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var explanationFile = Path.Combine(temp.Path, "benchmarks.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                1,
                """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "sources": [{"key": "benchmark-report", "url": "https://example.com/benchmark-report", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                explanationFile,
                "# Benchmarks\n\n## Answer\n\nOthers lend for three weeks [per the report][benchmark-report].\n"));

        Assert.DoesNotContain("source-link-bare", TestModel.Codes(model));
        Assert.DoesNotContain("source-key-unknown", TestModel.Codes(model));
        Assert.DoesNotContain("source-unused", TestModel.Codes(model));
    }

    /// <summary>A research source cited nowhere in its explanation reports.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact]
    public void Uncited_Source_Reports_Source_Unused()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var explanationFile = Path.Combine(temp.Path, "benchmarks.md");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                7,
                """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "sources": [{"key": "used-key", "url": "https://example.com/used", "readOn": "2026-10-12"}, {"key": "unused-key", "url": "https://example.com/unused", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                explanationFile,
                "# Benchmarks\n\n## Answer\n\nOthers lend for three weeks [per the report][used-key].\n"));

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
    public void Research_Without_An_Explanation_Skips_Source_Checks()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                Path.Combine(temp.Path, "benchmarks.yaml"),
                """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "sources": [{"key": "benchmark-report", "url": "https://example.com/benchmark-report", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}"""));

        Assert.DoesNotContain("source-link-bare", TestModel.Codes(model));
        Assert.DoesNotContain("source-key-unknown", TestModel.Codes(model));
        Assert.DoesNotContain("source-unused", TestModel.Codes(model));
    }
}
