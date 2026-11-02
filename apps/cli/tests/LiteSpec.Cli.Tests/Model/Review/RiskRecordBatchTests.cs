using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Review;

/// <summary>Proves a record carrying a risk class that no use case or decision holds gets a batch of its own.</summary>
public sealed class RiskRecordBatchTests
{
    static string Borrowing(string root) => ReviewPages.Page(
        root, ".litespec", "product", "lending", "borrowing.yaml");

    /// <summary>A Requirement Carrying Risk Is Queued And Approved On Its Own.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void A_Requirement_Carrying_Risk_Is_Queued_And_Approved_On_Its_Own()
    {
        // The large fixture's requirement cites research, so no decision holds it.
        using var fixture = ModelFixture.Prepare("large");
        ReviewPages.DraftEntry(Borrowing(fixture.Path), "requirement/borrowing.max-active-loans");
        ReviewPages.PatchEntry(Borrowing(fixture.Path), "requirement/borrowing.max-active-loans",
            front => front["risk"] = new JsonArray("money"));

        var queue = Cli.Run("model", "review", "--root", fixture.Path);

        Assert.Contains("requirement/borrowing.max-active-loans  risk money  1 record", queue.Output);
        Assert.DoesNotContain("borrowing/requirement", queue.Output);

        var report = Path.Combine(fixture.Path, "review.md");
        File.WriteAllText(report, "# Review\n\nRead.\n");

        var approved = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "requirement/borrowing.max-active-loans", "--by", "lending-team",
            "--second-reviewer", "agent", "--review-report", report, "--on", "2026-11-20");

        Assert.Equal(0, approved.Status);
        Assert.Contains("status: approved", File.ReadAllText(Borrowing(fixture.Path)));
    }
}
