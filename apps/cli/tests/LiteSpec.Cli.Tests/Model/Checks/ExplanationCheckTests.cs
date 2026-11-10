using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the explanation checks against hand-built models and files.</summary>
public sealed class ExplanationCheckTests
{
    /// <summary>An explanation with no record file beside it reports.</summary>
    /// <proves>litespec/core/model#explain-each-record-beside-its-record-file</proves>
    [Fact]
    public void OrphanedExplanationReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, ".litespec", "stray.md");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "# Stray\n\nNo record stands beside this file.\n");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-orphaned");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Explanation file '.litespec/stray.md' stands beside no record file.", finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Null(finding.Subject);
    }

    /// <summary>A README without frontmatter is a document, not an orphaned explanation.</summary>
    /// <proves>litespec/core/model#explain-each-record-beside-its-record-file</proves>
    [Fact]
    public void ReadmeWithoutFrontmatterPasses()
    {
        using var temp = TestCli.Temp("checks");
        var folder = Path.Combine(temp.Path, ".litespec");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "README.md"), "# Lending\n\nThe lending module.\n");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        Assert.DoesNotContain("explanation-orphaned", TestModel.Codes(model));
    }

    /// <summary>A Markdown file opening with a frontmatter fence is a document.</summary>
    /// <proves>litespec/core/model#explain-each-record-beside-its-record-file</proves>
    [Fact]
    public void DocumentWithFrontmatterPasses()
    {
        using var temp = TestCli.Temp("checks");
        var folder = Path.Combine(temp.Path, ".litespec");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "guide.md"), "---\ntype: reference\nid: guide/lending\n---\n\n# Guide\n");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        Assert.DoesNotContain("explanation-orphaned", TestModel.Codes(model));
    }

    /// <summary>An explanation with its record file beside it passes.</summary>
    /// <proves>litespec/core/model#explain-each-record-beside-its-record-file</proves>
    [Fact]
    public void ExplanationBesideItsRecordPasses()
    {
        using var temp = TestCli.Temp("checks");
        var folder = Path.Combine(temp.Path, ".litespec");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "loan.yaml"), "type: aggregate\n");
        File.WriteAllText(Path.Combine(folder, "loan.md"), "# Loan\n\nA loan lends one copy to one reader.\n");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        Assert.DoesNotContain("explanation-orphaned", TestModel.Codes(model));
    }

    /// <summary>A decision with no explanation file reports the missing file.</summary>
    /// <proves>litespec/core/model#require-an-explanation-file-for-article-types</proves>
    [Fact]
    public void DecisionWithoutAnExplanationReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, ".litespec", "decisions", "0001-loan-period.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-loan-period", file, """{"options": [{"name": "Fourteen days", "pros": "Copies turn over fast.", "cons": "Readers rush."}], "reopenedWhen": "Demand shifts."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Decision 'decision/0001-loan-period' holds no explanation file '.litespec/decisions/0001-loan-period.md'.", finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Equal("decision/0001-loan-period", finding.Subject);
    }

    /// <summary>A decision explanation without Context reports the missing section.</summary>
    /// <proves>litespec/core/model#require-an-explanation-file-for-article-types</proves>
    [Fact]
    public void DecisionExplanationWithoutContextReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "0001-loan-period.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "decision",
                "decision/0001-loan-period",
                file,
                1,
                """{"options": [{"name": "Fourteen days", "pros": "Copies turn over fast.", "cons": "Readers rush."}], "reopenedWhen": "Demand shifts."}""",
                Path.Combine(temp.Path, "0001-loan-period.md"),
                "# Loan period\n\nNo sections here.\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Decision 'decision/0001-loan-period' holds no Context section in its explanation.", finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Equal("decision/0001-loan-period", finding.Subject);
    }

    /// <summary>A decision explanation with Context passes.</summary>
    /// <proves>litespec/core/model#require-an-explanation-file-for-article-types</proves>
    [Fact]
    public void DecisionExplanationWithContextPasses()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "0001-loan-period.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "decision",
                "decision/0001-loan-period",
                file,
                1,
                """{"options": [{"name": "Fourteen days", "pros": "Copies turn over fast.", "cons": "Readers rush."}], "reopenedWhen": "Demand shifts."}""",
                Path.Combine(temp.Path, "0001-loan-period.md"),
                "# Loan period\n\n## Context\n\nReaders asked for a longer loan.\n"));

        Assert.DoesNotContain("explanation-missing", TestModel.Codes(model));
    }

    /// <summary>A research explanation without Answer reports the missing section.</summary>
    /// <proves>litespec/core/model#require-an-explanation-file-for-article-types</proves>
    [Fact]
    public void ResearchExplanationWithoutAnswerReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                1,
                """{"sources": [{"key": "benchmark-report"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                Path.Combine(temp.Path, "benchmarks.md"),
                "# Benchmarks\n\n## Notes on Sources\n\nOne source covers the answer.\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Research 'research/loan-period-benchmarks' holds no Answer section in its explanation.", finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Equal("research/loan-period-benchmarks", finding.Subject);
    }

    /// <summary>A research explanation with Answer passes.</summary>
    /// <proves>litespec/core/model#require-an-explanation-file-for-article-types</proves>
    [Fact]
    public void ResearchExplanationWithAnswerPasses()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                1,
                """{"sources": [{"key": "benchmark-report"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                Path.Combine(temp.Path, "benchmarks.md"),
                "# Benchmarks\n\n## Answer\n\nOthers lend for three weeks [per the report][benchmark-report].\n"));

        Assert.DoesNotContain("explanation-missing", TestModel.Codes(model));
    }

    /// <summary>A research record with no explanation file reports the missing file.</summary>
    /// <proves>litespec/core/model#require-an-explanation-file-for-article-types</proves>
    [Fact]
    public void ResearchWithoutAnExplanationReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, ".litespec", "benchmarks.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("research", "research/loan-period-benchmarks", file, """{"sources": [{"key": "benchmark-report"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Research 'research/loan-period-benchmarks' holds no explanation file '.litespec/benchmarks.md'.", finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Equal("research/loan-period-benchmarks", finding.Subject);
    }

    /// <summary>An entry of an article type needs no explanation file.</summary>
    /// <proves>litespec/core/model#require-an-explanation-file-for-article-types</proves>
    [Fact]
    public void EntryOfAnArticleTypeNeedsNoExplanation()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "0001-loan-period.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "decision",
                "decision/0001-loan-period",
                file,
                4,
                """{"options": [{"name": "Fourteen days", "pros": "Copies turn over fast.", "cons": "Readers rush."}], "reopenedWhen": "Demand shifts."}""",
                isEntry: true));

        Assert.DoesNotContain("explanation-missing", TestModel.Codes(model));
    }

    /// <summary>A page outside the article types needs no explanation file.</summary>
    /// <proves>litespec/core/model#require-an-explanation-file-for-article-types</proves>
    [Fact]
    public void NonArticlePageNeedsNoExplanation()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command"}"""));

        Assert.DoesNotContain("explanation-missing", TestModel.Codes(model));
    }
}
