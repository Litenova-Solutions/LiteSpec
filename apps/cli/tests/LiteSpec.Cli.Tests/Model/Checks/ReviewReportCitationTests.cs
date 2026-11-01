using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves a batch review report's citations are history, while a page's are checked.</summary>
public sealed class ReviewReportCitationTests
{
    /// <summary>A Review Report Citing A Removed Scenario Reports Nothing.</summary>
    /// <proves>litespec/core/release#write-a-manifest-for-every-batch-approval</proves>
    [Fact]
    public void A_Review_Report_Citing_A_Removed_Scenario_Reports_Nothing()
    {
        using var fixture = ModelFixture.Prepare("large");
        var reviews = Path.Combine(fixture.Path, "docs", "proof", "reviews");
        Directory.CreateDirectory(reviews);
        File.WriteAllText(
            Path.Combine(reviews, "2026-11-20-lending.md"),
            "# Lending Review\n\nRemoved scenario/borrow-copy.gone-since-the-review from the record.\n");

        var result = Cli.Run("model", "check", "--root", fixture.Path);

        Assert.DoesNotContain("scenario/borrow-copy.gone-since-the-review", result.Output);
    }

    /// <summary>A Page Citing A Removed Scenario Reports It.</summary>
    /// <proves>litespec/core/release#write-a-manifest-for-every-batch-approval</proves>
    [Fact]
    public void A_Page_Citing_A_Removed_Scenario_Reports_It()
    {
        using var fixture = ModelFixture.Prepare("large");
        var guides = Path.Combine(fixture.Path, "docs", "guides");
        Directory.CreateDirectory(guides);
        File.WriteAllText(
            Path.Combine(guides, "lending.md"),
            "# Lending\n\nSee scenario/borrow-copy.gone-since-the-review for the refusal.\n");

        var result = Cli.Run("model", "check", "--root", fixture.Path);

        Assert.Contains("Citation 'scenario/borrow-copy.gone-since-the-review' resolves to no record.", result.Output);
    }
}
