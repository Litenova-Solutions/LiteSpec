using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves a link to a missing file reports link-unknown, a link naming an unknown record reports reference-unknown, and resolving links and links inside fenced code pass.</summary>
public sealed class LinkCheckTests
{
    /// <summary>A link to a file that exists nowhere reports it with its file and line.</summary>
    /// <proves>litespec/core/model#resolve-every-page-link</proves>
    [Fact]
    public void MissingFileLinkReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));
        model.Bodies.Add(TestModel.Body(
            Path.Combine(temp.Path, "borrowing.md"),
            "# Max Active Loans\n\nSee [the lending guide](./no-such-guide.md).\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "link-unknown");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Link './no-such-guide.md' resolves to no file.", finding.Message);
        Assert.Equal(Path.Combine(temp.Path, "borrowing.md"), finding.Path);
        Assert.Equal(3, finding.Line);
    }

    /// <summary>A link naming a record no record holds reports reference-unknown and no link-unknown.</summary>
    /// <proves>litespec/core/model#resolve-every-page-link</proves>
    [Fact]
    public void UnknownRecordLinkReportsReferenceUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));
        model.Bodies.Add(TestModel.Body(
            Path.Combine(temp.Path, "borrowing.md"),
            "# Max Active Loans\n\nSee [the lending use case](use-case/lending.no-such-use-case).\n"));

        var findings = Checker.Check(model).Findings;
        var finding = Assert.Single(findings, found => found.Code == "reference-unknown" && found.Message.StartsWith("Link ", StringComparison.Ordinal));
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Link 'use-case/lending.no-such-use-case' resolves to no record.", finding.Message);
        Assert.Equal(Path.Combine(temp.Path, "borrowing.md"), finding.Path);
        Assert.Equal(3, finding.Line);
        Assert.DoesNotContain(findings, found => found.Code == "link-unknown");
    }

    /// <summary>Links to an existing file and a written record pass.</summary>
    /// <proves>litespec/core/model#resolve-every-page-link</proves>
    [Fact]
    public void ResolvingLinksPass()
    {
        using var temp = TestCli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "lending-guide.md"), "# Lending Guide\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.yaml"), """{"title": "Reader", "kind": "person"}"""));
        model.Bodies.Add(TestModel.Body(
            Path.Combine(temp.Path, "borrowing.md"),
            "# Max Active Loans\n\nSee [the lending guide](./lending-guide.md) and [the reader](actor/reader).\n"));

        Assert.DoesNotContain("link-unknown", TestModel.Codes(model));
        Assert.DoesNotContain("reference-unknown", TestModel.Codes(model));
    }

    /// <summary>A link inside a fenced code block passes.</summary>
    /// <proves>litespec/core/model#resolve-every-page-link</proves>
    [Fact]
    public void LinkInsideFencedCodeBlockPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));
        model.Bodies.Add(TestModel.Body(
            Path.Combine(temp.Path, "borrowing.md"),
            "# Max Active Loans\n\n```markdown\nSee [the guide](./no-such-guide.md).\n```\n"));

        Assert.DoesNotContain("link-unknown", TestModel.Codes(model));
        Assert.DoesNotContain("reference-unknown", TestModel.Codes(model));
    }
}
