namespace LiteSpec.Cli.Tests.Model.Marks;

/// <summary>Proves a Kernel exception marking its product rule carries its mark, and a follows mark alone does not count.</summary>
public sealed class SharedRuleExceptionTests
{
    /// <summary>Kernel Exception Marking Its Product Rule Carries Its Mark.</summary>
    /// <proves>litespec/backend/dotnet/domain#reject-business-violations-with-domain-exceptions</proves>
    [Fact]
    public void KernelExceptionMarkingItsProductRuleCarriesItsMark()
    {
        using var fixture = MarksFixture.Prepare("shared-exception");
        var findings = MarksFixture.Check(fixture.Path);

        Assert.DoesNotContain(findings, finding => finding.Code == "mark-missing");
        Assert.DoesNotContain(findings, finding => finding.Code == "mark-unknown");
        Assert.DoesNotContain(findings, finding => finding.Code == "code-placement-drift" && finding.Path.Contains("Library.Kernel", StringComparison.Ordinal));
        Assert.DoesNotContain(findings, finding => finding.Code == "behavior-unrecorded");
    }

    /// <summary>Kernel Exception With Only A Follows Mark Still Reports Missing.</summary>
    /// <proves>litespec/backend/dotnet/domain#reject-business-violations-with-domain-exceptions</proves>
    [Fact]
    public void KernelExceptionWithOnlyAFollowsMarkStillReportsMissing()
    {
        using var fixture = MarksFixture.Prepare("shared-exception");
        var file = Path.Combine(fixture.Path, "apps", "api", "src", "Library.Kernel", "Domain", "UnknownRecordException.cs");
        File.WriteAllText(file, File.ReadAllText(file).Replace(
            "/// <implements>rule/library.unknown-record</implements>",
            "/// <follows>litespec/backend/dotnet/domain#reject-business-violations-with-domain-exceptions</follows>",
            StringComparison.Ordinal));

        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "mark-missing");

        var finding = Assert.Single(findings);
        Assert.Contains("UnknownRecordException", finding.Message, StringComparison.Ordinal);
    }
}
