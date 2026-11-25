using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Marks;

/// <summary>Proves a partial type carries the marks of every declaration, generated or hand-written.</summary>
public sealed class PartialMarkTests
{
    /// <summary>A partial exception with its mark only on the generated half reports no mark finding.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A partial exception with its mark only on the generated half reports no mark finding")]
    public void PartialExceptionWithMarkOnlyOnGeneratedHalfReportsNothing()
    {
        using var temp = MarksFixture.Prepare("partial");

        var findings = MarksFixture.Check(temp.Path);

        Assert.DoesNotContain(findings, finding => finding.Code == "mark-missing" && OnException(finding));
        Assert.DoesNotContain(findings, finding => finding.Code == "mark-unknown" && OnException(finding));
        Assert.DoesNotContain(findings, finding => finding.Code == "mark-count-invalid" && OnException(finding));
    }

    /// <summary>A partial exception with the same mark on both halves reports the record named twice.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A partial exception with the same mark on both halves reports the record named twice")]
    public void PartialExceptionWithMarkOnBothHalvesReportsCountInvalid()
    {
        using var temp = MarksFixture.Prepare("partial");
        var file = Path.Combine(temp.Path, "apps", "api", "src", "Library.Kernel", "Domain", "UnknownRecordException.cs");
        File.WriteAllText(file, File.ReadAllText(file).Replace(
            "public sealed partial class UnknownRecordException",
            "/// <implements>rule/library.unknown-record</implements>\npublic sealed partial class UnknownRecordException",
            StringComparison.Ordinal));

        var findings = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-count-invalid")
            .Where(OnException)
            .ToList();

        var finding = Assert.Single(findings);
        Assert.Equal("Rule exception 'UnknownRecordException' names 'rule/library.unknown-record' twice.", finding.Message);
        Assert.Equal("UnknownRecordException.cs", Path.GetFileName(finding.Path));
    }

    /// <summary>Whether the finding sits on the partial exception under test.</summary>
    /// <param name="finding">The finding under test.</param>
    /// <returns>True when the finding sits on either half of the partial exception.</returns>
    static bool OnException(Finding finding) =>
        Path.GetFileName(finding.Path).StartsWith("UnknownRecordException", StringComparison.Ordinal);
}
