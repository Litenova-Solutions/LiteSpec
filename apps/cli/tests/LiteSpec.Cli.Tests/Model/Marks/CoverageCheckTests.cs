namespace LiteSpec.Tests.Model.Marks;

/// <summary>Proves the record-side traceability findings against the marks fixtures.</summary>
public sealed class CoverageCheckTests
{
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void Aggregate_Without_A_Marked_Root_Reports_Aggregate_Unimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "aggregate-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("aggregate/lending.loan", finding.Subject);
    }

    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void Reaction_Without_A_Marked_Handler_Reports_Reaction_Unimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "reaction-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("reaction/lending.send-due-notice-on-loan-borrowed", finding.Subject);
    }

    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void Job_Without_A_Marked_Host_Reports_Job_Unimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "job-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("job/lending.nightly-due-scan", finding.Subject);
    }

    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void System_Without_A_Marked_Port_Reports_External_System_Unimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "external-system-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("external-system/text-gateway", finding.Subject);
    }

    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void Browser_Use_Case_Without_An_Endpoint_Reports_Use_Case_Unexposed()
    {
        using var fixture = MarksFixture.Prepare("exposed");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "use-case-unexposed");

        var finding = Assert.Single(findings);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
        Assert.Contains("entry-point/library-app", finding.Message);
    }

    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void Browser_Use_Case_Without_A_Screen_Reports_Use_Case_Unreached()
    {
        using var fixture = MarksFixture.Prepare("exposed");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "use-case-unreached");

        var finding = Assert.Single(findings);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
        Assert.Contains("entry-point/library-app", finding.Message);
    }

    /// <proves>litespec/core/system#declare-every-edge-between-records</proves>
    [Fact]
    public void Declared_Edge_No_Code_Takes_Reports_Effect_Unimplemented()
    {
        using var fixture = MarksFixture.Prepare("effects");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "effect-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
        Assert.Contains("issues use-case/lending.register-copy", finding.Message);
    }
}
