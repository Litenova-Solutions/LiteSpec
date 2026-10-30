using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Review;

/// <summary>Proves <c>litespec model review</c> computes the review queue from the model.</summary>
/// <follows>litespec/core/release#compute-the-review-queue-from-the-model</follows>
public sealed class ReviewQueueTests
{
    static string BorrowCopy(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md");

    static string ExtendLoan(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "loans", "extend-loan.md");

    static string ReturnCopy(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "loans", "return-copy.md");

    static string Borrowing(string root) => ReviewPages.Page(
        root, "docs", "product", "lending", "borrowing.md");

    /// <summary>A draft use case lists its batch with its results.</summary>
    /// <proves>litespec/core/release#compute-the-review-queue-from-the-model</proves>
    [Fact]
    public void Queue_Lists_Draft_Use_Case_Batch_With_Results()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftPage(BorrowCopy(fixture.Path));

        var result = Cli.Run("model", "review", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("use-case/lending.borrow-copy  tier 3  3 records  awaiting 0", result.Output);
        Assert.Contains("  use-case/lending.borrow-copy  draft  -", result.Output);
        Assert.Contains("  result/borrow-copy.borrowed  draft  -", result.Output);
        Assert.Contains("  result/borrow-copy.copy-unavailable  draft  -", result.Output);
    }

    /// <summary>Batches order by full re-read, reopened, then awaiting count.</summary>
    /// <proves>litespec/core/release#compute-the-review-queue-from-the-model</proves>
    [Fact]
    public void Queue_Orders_FullReRead_Reopened_Awaiting()
    {
        using var fixture = ModelFixture.Prepare("small");

        // A reopened use case carrying risk needs a full re-read.
        ReviewPages.AddPageRisk(ExtendLoan(fixture.Path));

        // A reopened result with no risk reopens its batch without a full re-read.
        ReviewPages.PatchEntry(
            ReturnCopy(fixture.Path), "result/return-copy.returned",
            entry => entry["meaning"] = "The copy came back to its shelf.");

        // A decision batch holding a draft requirement awaited by a draft use case sorts above one awaited by none.
        ReviewPages.DraftEntry(Borrowing(fixture.Path), "requirement/borrowing.max-active-loans");
        ReviewPages.DraftPage(BorrowCopy(fixture.Path));

        var result = Cli.Run("model", "review", "--root", fixture.Path);

        Assert.Equal(0, result.Status);

        var fullReRead = result.Output.IndexOf("use-case/lending.extend-loan", StringComparison.Ordinal);
        var reopened = result.Output.IndexOf("use-case/lending.return-copy", StringComparison.Ordinal);
        var awaited = result.Output.IndexOf("decision/decision/0001-loan-period", StringComparison.Ordinal);
        var plain = result.Output.IndexOf("use-case/lending.borrow-copy", StringComparison.Ordinal);

        Assert.True(0 <= fullReRead && fullReRead < reopened, result.Output);
        Assert.True(reopened < awaited, result.Output);
        Assert.True(awaited < plain, result.Output);
        Assert.Contains("decision/decision/0001-loan-period  tier 3  1 record  awaiting 1", result.Output);
        Assert.Contains("  result/return-copy.returned  reviewed  reopened", result.Output);
        Assert.Contains("fullReRead", result.Output);
    }

    /// <summary>The JSON queue writes each batch and record with its documented fields.</summary>
    /// <proves>litespec/core/release#compute-the-review-queue-from-the-model</proves>
    [Fact]
    public void Review_Json_Writes_Batch_And_Record_Fields()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftPage(BorrowCopy(fixture.Path));

        var result = Cli.Run("model", "review", "--root", fixture.Path, "--format", "json");

        Assert.Equal(0, result.Status);

        var batches = ReviewPages.OutputJson(result)["batches"]!.AsArray();

        Assert.Single(batches);

        var batch = batches[0]!;

        Assert.Equal("use-case/lending.borrow-copy", batch["selector"]!.GetValue<string>());
        Assert.Equal("use-case", batch["kind"]!.GetValue<string>());
        Assert.Equal(3, batch["tier"]!.GetValue<int>());
        Assert.Equal("lending-team", batch["deciders"]!.AsArray()[0]!.GetValue<string>());
        Assert.Equal("2027-01-01", batch["decideBy"]!.GetValue<string>());
        Assert.Equal(0, batch["awaiting"]!.GetValue<int>());

        var records = batch["records"]!.AsArray();

        Assert.Equal(3, records.Count);
        Assert.Equal("use-case/lending.borrow-copy", records[0]!["id"]!.GetValue<string>());
        Assert.Equal("use-case", records[0]!["type"]!.GetValue<string>());
        Assert.Equal("behavior", records[0]!["layer"]!.GetValue<string>());
        Assert.Equal(3, records[0]!["tier"]!.GetValue<int>());
        Assert.Equal("draft", records[0]!["status"]!.GetValue<string>());
        Assert.Equal("draft", records[0]!["effectiveStatus"]!.GetValue<string>());
        Assert.False(records[0]!["reopened"]!.GetValue<bool>());
        Assert.False(records[0]!["fullReRead"]!.GetValue<bool>());
        Assert.Equal(0, records[0]!["openQuestions"]!.GetValue<int>());
        Assert.Equal(0, records[0]!["scenarios"]!["total"]!.GetValue<int>());
        Assert.Equal(0, records[0]!["scenarios"]!["confirmed"]!.GetValue<int>());
        Assert.Equal(0, records[0]!["scenarios"]!["passing"]!.GetValue<int>());
        Assert.Equal(0, records[0]!["scenarios"]!["stale"]!.GetValue<int>());
        Assert.True(records[0]!["findings"]!.GetValue<int>() >= 0);
    }

    /// <summary>Filters keep the batches holding a record that matches every filter.</summary>
    /// <proves>litespec/core/release#compute-the-review-queue-from-the-model</proves>
    [Fact]
    public void Review_Filters_Keep_Matching_Batches()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftPage(BorrowCopy(fixture.Path));
        ReviewPages.DraftPage(ExtendLoan(fixture.Path));
        ReviewPages.AddPageRisk(ExtendLoan(fixture.Path));

        var tier = Cli.Run("model", "review", "--root", fixture.Path, "--tier", "1");

        Assert.Equal(0, tier.Status);
        Assert.Contains("use-case/lending.extend-loan", tier.Output);
        Assert.DoesNotContain("use-case/lending.borrow-copy", tier.Output);

        var type = Cli.Run("model", "review", "--root", fixture.Path, "--type", "result");

        Assert.Equal(0, type.Status);
        Assert.Contains("use-case/lending.extend-loan", type.Output);
        Assert.Contains("use-case/lending.borrow-copy", type.Output);

        var module = Cli.Run("model", "review", "--root", fixture.Path, "--module", "intent");

        Assert.Equal(0, module.Status);
        Assert.Contains("No drafts or reopened records.", module.Output);

        var decider = Cli.Run("model", "review", "--root", fixture.Path, "--decider", "lending-team");

        Assert.Equal(0, decider.Status);
        Assert.Contains("use-case/lending.borrow-copy", decider.Output);

        var both = Cli.Run(
            "model", "review", "--root", fixture.Path, "--tier", "1", "--type", "use-case");

        Assert.Equal(0, both.Status);
        Assert.Contains("use-case/lending.extend-loan", both.Output);
        Assert.DoesNotContain("use-case/lending.borrow-copy", both.Output);
    }

    /// <summary>A clean fixture prints no queue.</summary>
    /// <proves>litespec/core/release#compute-the-review-queue-from-the-model</proves>
    [Fact]
    public void Review_Empty_Queue_Prints_None()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = Cli.Run("model", "review", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("No drafts or reopened records.", result.Output);
    }

    /// <summary>A tier outside 1 and 3 is a usage error.</summary>
    /// <proves>litespec/core/release#compute-the-review-queue-from-the-model</proves>
    [Fact]
    public void Review_Rejects_Unknown_Tier()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = Cli.Run("model", "review", "--root", fixture.Path, "--tier", "2");

        Assert.Equal(2, result.Status);
        Assert.Contains("use 1 or 3", result.Output);
    }
}
