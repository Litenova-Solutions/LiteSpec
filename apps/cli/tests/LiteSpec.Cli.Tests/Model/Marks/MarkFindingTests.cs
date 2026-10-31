namespace LiteSpec.Tests.Model.Marks;

/// <summary>Proves the mark presence, link, style and drift findings against the marks fixtures.</summary>
public sealed class MarkFindingTests
{
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Handler_Without_A_Mark_Reports_Mark_Missing()
    {
        using var fixture = MarksFixture.Prepare("missing");
        var missing = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "mark-missing");

        var finding = Assert.Single(missing);
        Assert.Contains("Handler", finding.Message);
        Assert.Contains("BorrowCopyCommandHandler", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Mark_Naming_No_Record_Reports_Mark_Unresolved()
    {
        using var fixture = MarksFixture.Prepare("unresolved");
        var unresolved = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "mark-unknown");

        var finding = Assert.Single(unresolved, found => found.Message.Contains("matches no record"));
        Assert.Contains("use-case/lending.ghost", finding.Message);
        Assert.Equal("use-case/lending.ghost", finding.Subject);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Mark_Naming_A_Record_Of_Another_Type_Reports_Mark_Unresolved()
    {
        using var fixture = MarksFixture.Prepare("unresolved");
        var unresolved = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "mark-unknown");

        var finding = Assert.Single(unresolved, found => found.Message.Contains("not a use-case record"));
        Assert.Contains("rule/loan.copy-available", finding.Message);
        Assert.Contains("rule record", finding.Message);
        Assert.Equal("rule/loan.copy-available", finding.Subject);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Handler_With_Two_Marks_Reports_Mark_Count()
    {
        using var fixture = MarksFixture.Prepare("count");
        var counts = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "mark-count-invalid");

        var finding = Assert.Single(counts, found => found.Message.Contains("carries 2 marks"));
        Assert.Contains("BorrowCopyCommandHandler", finding.Message);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Port_Naming_One_System_Twice_Reports_Mark_Count()
    {
        using var fixture = MarksFixture.Prepare("count");
        var counts = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "mark-count-invalid");

        var finding = Assert.Single(counts, found => found.Message.Contains("twice"));
        Assert.Contains("external-system/text-gateway", finding.Message);
    }

    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact]
    public void Mark_In_The_Undeclared_Style_Reports_Mark_Style_Mixed()
    {
        using var fixture = MarksFixture.Prepare("style");
        var mixed = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "mark-style-invalid");

        var finding = Assert.Single(mixed);
        Assert.Contains("BorrowCopyCommandHandler", finding.Message);
        Assert.Contains("in the xml style", finding.Message);
        Assert.Contains("declares attribute", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact]
    public void Event_Classification_Drifting_From_Its_Record_Reports_Runtime_Value_Drift()
    {
        using var fixture = MarksFixture.Prepare("drift");
        var drifts = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "runtime-value-drift");

        var finding = Assert.Single(drifts, found => found.Subject == "event/loan.loan-borrowed");
        Assert.Contains("Classification", finding.Message);
        Assert.Contains("event/loan.placed", finding.Message);
    }

    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact]
    public void Exception_Code_Drifting_From_Its_Result_Reports_Runtime_Value_Drift()
    {
        using var fixture = MarksFixture.Prepare("drift");
        var drifts = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "runtime-value-drift");

        var finding = Assert.Single(drifts, found => found.Subject == "rule/loan.copy-available");
        Assert.Contains("Code", finding.Message);
        Assert.Contains("lending.wrong-code", finding.Message);
        Assert.Contains("lending.copy-not-available", finding.Message);
    }
}
