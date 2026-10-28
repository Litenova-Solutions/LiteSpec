using LiteSpec.Model;

namespace LiteSpec.Tests.Model.Frontend;

/// <summary>Proves the mark checks over frontend modules: missing, unresolved, mirrored and styled.</summary>
public sealed class FrontendMarkCheckTests
{
    [Fact]
    public void Marked_Modules_Report_No_Mark_Findings()
    {
        using var fixture = FrontendMarksFixture.Prepare("clean");
        var findings = FrontendMarksFixture.Check(fixture.Path);

        Assert.Empty(FrontendMarksFixture.WithCode(findings, "mark-missing"));
        Assert.Empty(FrontendMarksFixture.WithCode(findings, "mark-unresolved"));
        Assert.Empty(FrontendMarksFixture.WithCode(findings, "mark-count"));
        Assert.Empty(FrontendMarksFixture.WithCode(findings, "mark-style-mixed"));
    }

    [Fact]
    public void Unmarked_Module_Reports_Mark_Missing()
    {
        using var fixture = FrontendMarksFixture.Prepare("missing");
        var missing = FrontendMarksFixture.WithCode(FrontendMarksFixture.Check(fixture.Path), "mark-missing");

        var finding = Assert.Single(missing);
        Assert.Contains("Frontend module", finding.Message);
        Assert.Contains("lending/loans/borrow-copy", finding.Message);
        Assert.Null(finding.Subject);
    }

    [Fact]
    public void Module_Marking_Another_Use_Case_Reports_Mark_Unresolved()
    {
        using var fixture = FrontendMarksFixture.Prepare("mismatch");
        var unresolved = FrontendMarksFixture.WithCode(FrontendMarksFixture.Check(fixture.Path), "mark-unresolved");

        var finding = Assert.Single(unresolved, found => found.Message.Contains("mirrors"));
        Assert.Contains("use-case/lending.list-loans", finding.Message);
        Assert.Contains("use-case/lending.borrow-copy", finding.Message);
        Assert.Equal("use-case/lending.list-loans", finding.Subject);
    }

    [Fact]
    public void Module_Marking_No_Use_Case_Reports_Mark_Unresolved()
    {
        using var fixture = FrontendMarksFixture.Prepare("mismatch");
        var unresolved = FrontendMarksFixture.WithCode(FrontendMarksFixture.Check(fixture.Path), "mark-unresolved");

        var finding = Assert.Single(unresolved, found => found.Message.Contains("matches no record"));
        Assert.Contains("use-case/lending.ghost", finding.Message);
        Assert.Equal("use-case/lending.ghost", finding.Subject);
    }

    [Fact]
    public void Frontend_Mark_Reports_No_Mark_Style_Mixed_Where_A_Backend_Mark_Does()
    {
        using var fixture = FrontendMarksFixture.Prepare("style");
        var findings = FrontendMarksFixture.Check(fixture.Path);
        var mixed = FrontendMarksFixture.WithCode(findings, "mark-style-mixed");

        var finding = Assert.Single(mixed);
        Assert.Contains("BorrowCopyCommandHandler", finding.Message);
        Assert.DoesNotContain("Frontend module", finding.Message);
        Assert.Empty(FrontendMarksFixture.WithCode(findings, "mark-missing"));
        Assert.Empty(FrontendMarksFixture.WithCode(findings, "mark-unresolved"));
    }
}
