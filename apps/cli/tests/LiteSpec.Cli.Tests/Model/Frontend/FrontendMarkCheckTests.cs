namespace LiteSpec.Cli.Tests.Model.Frontend;

/// <summary>Proves the mark checks over frontend modules: missing, unresolved, mirrored and styled.</summary>
public sealed class FrontendMarkCheckTests
{
    /// <summary>Marked Modules Report No Mark Findings.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void MarkedModulesReportNoMarkFindings()
    {
        using var fixture = FrontendMarksFixture.Prepare("clean");
        var findings = FrontendMarksFixture.Check(fixture.Path);

        Assert.Empty(FrontendMarksFixture.WithCode(findings, "mark-missing"));
        Assert.Empty(FrontendMarksFixture.WithCode(findings, "mark-unknown"));
        Assert.Empty(FrontendMarksFixture.WithCode(findings, "mark-count-invalid"));
        Assert.Empty(FrontendMarksFixture.WithCode(findings, "mark-style-invalid"));
    }

    /// <summary>Unmarked Module Reports Mark Missing.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void UnmarkedModuleReportsMarkMissing()
    {
        using var fixture = FrontendMarksFixture.Prepare("missing");
        var missing = FrontendMarksFixture.WithCode(FrontendMarksFixture.Check(fixture.Path), "mark-missing");

        var finding = Assert.Single(missing);
        Assert.Contains("Frontend module", finding.Message);
        Assert.Contains("lending/loans/borrow-copy", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Module Marking Another Use Case Reports Mark Unresolved.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void ModuleMarkingAnotherUseCaseReportsMarkUnresolved()
    {
        using var fixture = FrontendMarksFixture.Prepare("mismatch");
        var unresolved = FrontendMarksFixture.WithCode(FrontendMarksFixture.Check(fixture.Path), "mark-unknown");

        var finding = Assert.Single(unresolved, found => found.Message.Contains("mirrors"));
        Assert.Contains("use-case/lending.list-loans", finding.Message);
        Assert.Contains("use-case/lending.borrow-copy", finding.Message);
        Assert.Equal("use-case/lending.list-loans", finding.Subject);
    }

    /// <summary>Module Marking No Use Case Reports Mark Unresolved.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void ModuleMarkingNoUseCaseReportsMarkUnresolved()
    {
        using var fixture = FrontendMarksFixture.Prepare("mismatch");
        var unresolved = FrontendMarksFixture.WithCode(FrontendMarksFixture.Check(fixture.Path), "mark-unknown");

        var finding = Assert.Single(unresolved, found => found.Message.Contains("matches no record"));
        Assert.Contains("use-case/lending.ghost", finding.Message);
        Assert.Equal("use-case/lending.ghost", finding.Subject);
    }

    /// <summary>Frontend Mark Reports No Mark Style Mixed Where A Backend Mark Does.</summary>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact]
    public void FrontendMarkReportsNoMarkStyleMixedWhereABackendMarkDoes()
    {
        using var fixture = FrontendMarksFixture.Prepare("style");
        var findings = FrontendMarksFixture.Check(fixture.Path);
        var mixed = FrontendMarksFixture.WithCode(findings, "mark-style-invalid");

        var finding = Assert.Single(mixed);
        Assert.Contains("BorrowCopyCommandHandler", finding.Message);
        Assert.DoesNotContain("Frontend module", finding.Message);
        Assert.Empty(FrontendMarksFixture.WithCode(findings, "mark-missing"));
        Assert.Empty(FrontendMarksFixture.WithCode(findings, "mark-unknown"));
    }
}
