using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the explanation-section backing check against hand-built models and files.</summary>
public sealed class ExplanationBackingCheckTests
{
    /// <summary>A problem holding a Failure Cases section but no failureCases reports the unbacked section.</summary>
    /// <proves>litespec/core/model#keep-the-whole-specification-in-the-records</proves>
    [Fact]
    public void FailureCasesWithoutFailureCasesReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "lost-copies.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "problem",
                "problem/lost-copies",
                file,
                1,
                """{"summary": "Copies go missing.", "statement": "Copies go missing on the shelf.", "affects": ["actor/reader"]}""",
                Path.Combine(temp.Path, "lost-copies.md"),
                "# Lost Copies\n\nCopies go missing on the shelf, and readers wait.\n\n## Affected Users\n\nReaders.\n\n## Failure Cases\n\nCopies go missing on the shelf.\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-section-unbacked");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Problem 'problem/lost-copies' explains Failure Cases, but its record holds no failureCases.", finding.Message);
        Assert.Equal(Path.ChangeExtension(file, ".md"), finding.Path);
        Assert.Equal("problem/lost-copies", finding.Subject);
    }

    /// <summary>A problem holding failureCases reports no unbacked section.</summary>
    /// <proves>litespec/core/model#keep-the-whole-specification-in-the-records</proves>
    [Fact]
    public void FailureCasesWithFailureCasesPasses()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "lost-copies.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "problem",
                "problem/lost-copies",
                file,
                1,
                """{"summary": "Copies go missing.", "statement": "Copies go missing on the shelf.", "affects": ["actor/reader"], "failureCases": ["Copies go missing on the shelf."]}""",
                Path.Combine(temp.Path, "lost-copies.md"),
                "# Lost Copies\n\nCopies go missing on the shelf, and readers wait.\n\n## Affected Users\n\nReaders.\n\n## Failure Cases\n\nCopies go missing on the shelf.\n"));

        Assert.DoesNotContain("explanation-section-unbacked", TestModel.Codes(model));
    }

    /// <summary>An Example section never reports an unbacked section.</summary>
    /// <proves>litespec/core/model#keep-the-whole-specification-in-the-records</proves>
    [Fact]
    public void ExampleSectionNeverReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "lost-copies.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "problem",
                "problem/lost-copies",
                file,
                1,
                """{"summary": "Copies go missing.", "statement": "Copies go missing on the shelf.", "affects": ["actor/reader"], "failureCases": ["Copies go missing on the shelf."], "workaround": "Readers ask at the desk.", "cost": "Readers wait for copies.", "resolutionCriteria": ["Copies stay on the shelf."]}""",
                Path.Combine(temp.Path, "lost-copies.md"),
                "# Lost Copies\n\nCopies go missing on the shelf, and readers wait.\n\n## Affected Users\n\nReaders.\n\n## Failure Cases\n\nCopies go missing on the shelf.\n\n## Example\n\nOne reader waits for a copy.\n"));

        Assert.DoesNotContain("explanation-section-unbacked", TestModel.Codes(model));
    }
}
