using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Book;

/// <summary>Proves the review page groups drafts and reopened records by review batch.</summary>
/// <follows>litespec/core/release#compute-the-review-queue-from-the-model</follows>
public sealed class BookReviewBatchesTests
{
    const string BorrowCopyUseCase = @"{""type"":""use-case"",""id"":""use-case/lending.borrow-copy"",""title"":""Borrow Copy"",""owner"":""Ada"",""status"":""draft"",""effectiveStatus"":""draft"",""decider"":""Ada"",""decideBy"":""2026-11-20"",""file"":""docs/behavior/modules/lending/loans/borrow-copy.md"",""line"":3,""fields"":{""risk"":[""money""]},""links"":{}}";
    const string BorrowedResult = @"{""type"":""result"",""id"":""result/borrow-copy.borrowed"",""title"":""Borrowed"",""owner"":""Ada"",""status"":""draft"",""effectiveStatus"":""draft"",""decider"":""Ada"",""decideBy"":""2026-11-15"",""file"":""docs/behavior/modules/lending/loans/borrow-copy.md"",""line"":40,""fields"":{},""links"":{}}";
    const string BorrowingScenario = @"{""type"":""scenario"",""id"":""scenario/borrow-copy.borrowing-succeeds"",""title"":""Borrowing Succeeds"",""owner"":""Ada"",""status"":""draft"",""effectiveStatus"":""draft"",""decider"":null,""decideBy"":null,""file"":""docs/behavior/modules/lending/loans/borrow-copy.md"",""line"":60,""fields"":{},""links"":{}}";

    /// <summary>Drafts of one use case render under one batch heading with the approve command.</summary>
    /// <proves>litespec/core/release#compute-the-review-queue-from-the-model</proves>
    [Fact]
    public void Batch_Groups_Use_Case_Drafts_Under_One_Heading()
    {
        using var temp = Cli.Temp("book-batches");
        var model = WriteModel(temp, string.Join(",", BorrowCopyUseCase, BorrowedResult, BorrowingScenario), "[]", "[]");
        var result = Cli.Run("book", "--model", model, "--out", Out(temp));
        Assert.Equal(0, result.Status);
        var html = ReviewHtml(temp);
        Assert.Contains("<h3 id=\"batch-use-case-lending-borrow-copy\">", html, StringComparison.Ordinal);
        Assert.Contains("tier 1, 3 records", html, StringComparison.Ordinal);
        Assert.Contains("2026-11-15", html, StringComparison.Ordinal);
        Assert.Contains(
            "litespec model approve --batch use-case/lending.borrow-copy --by Ada",
            html,
            StringComparison.Ordinal);
        var heading = html.IndexOf("batch-use-case-lending-borrow-copy", StringComparison.Ordinal);
        Assert.True(
            heading < html.IndexOf("use-case/lending.borrow-copy", heading + 1, StringComparison.Ordinal),
            "The use case renders under its batch heading.");
        Assert.True(
            heading < html.IndexOf("result/borrow-copy.borrowed", StringComparison.Ordinal),
            "The result renders under its use case batch heading.");
        Assert.True(
            heading < html.IndexOf("scenario/borrow-copy.borrowing-succeeds", StringComparison.Ordinal),
            "The scenario renders under its use case batch heading.");
    }

    /// <summary>Two batches render in queue order: Intent before Behavior whatever the dates say.</summary>
    /// <proves>litespec/core/release#compute-the-review-queue-from-the-model</proves>
    [Fact]
    public void Batches_Render_In_Queue_Order()
    {
        using var temp = Cli.Temp("book-batches");
        var feature = @"{""type"":""feature"",""id"":""feature/self-checkout"",""title"":""Self Checkout"",""owner"":""Ada"",""status"":""draft"",""effectiveStatus"":""draft"",""decider"":""Ada"",""decideBy"":""2026-12-31"",""file"":""docs/product/features/self-checkout.md"",""line"":3,""fields"":{},""links"":{}}";
        var useCase = @"{""type"":""use-case"",""id"":""use-case/lending.return-copy"",""title"":""Return Copy"",""owner"":""Ada"",""status"":""draft"",""effectiveStatus"":""draft"",""decider"":""Ada"",""decideBy"":""2026-11-01"",""file"":""docs/behavior/modules/lending/loans/return-copy.md"",""line"":3,""fields"":{},""links"":{}}";
        var model = WriteModel(temp, string.Join(",", feature, useCase), "[]", "[]");
        var result = Cli.Run("book", "--model", model, "--out", Out(temp));
        Assert.Equal(0, result.Status);
        var html = ReviewHtml(temp);
        Assert.Contains("<h3 id=\"batch-intent-feature\">", html, StringComparison.Ordinal);
        Assert.Contains("<h3 id=\"batch-use-case-lending-return-copy\">", html, StringComparison.Ordinal);
        Assert.True(
            html.IndexOf("batch-intent-feature", StringComparison.Ordinal)
                < html.IndexOf("batch-use-case-lending-return-copy", StringComparison.Ordinal),
            "The Intent batch sorts before the Behavior batch despite its later decide-by date.");
    }

    /// <summary>The page no longer renders the flat drafts table once batches group the drafts.</summary>
    /// <proves>litespec/core/release#compute-the-review-queue-from-the-model</proves>
    [Fact]
    public void Drafts_Do_Not_Render_As_A_Flat_Table()
    {
        using var temp = Cli.Temp("book-batches");
        var model = WriteModel(temp, string.Join(",", BorrowCopyUseCase, BorrowedResult, BorrowingScenario), "[]", "[]");
        var result = Cli.Run("book", "--model", model, "--out", Out(temp));
        Assert.Equal(0, result.Status);
        var html = ReviewHtml(temp);
        Assert.Contains("batch-use-case-lending-borrow-copy", html, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "<th>Record</th><th>Type</th><th>Decider</th><th>Decide By</th>",
            html,
            StringComparison.Ordinal);
    }

    /// <summary>A reopened record renders in its batch with its flags, and proof state shows stale.</summary>
    /// <proves>litespec/core/release#compute-the-review-queue-from-the-model</proves>
    [Fact]
    public void Batch_Shows_Reopened_Flags_And_Stale_Proof()
    {
        using var temp = Cli.Temp("book-batches");
        var requirement = @"{""type"":""requirement"",""id"":""requirement/borrowing.late-fee"",""title"":""Late Fee"",""owner"":""Ada"",""status"":""approved"",""effectiveStatus"":""approved"",""decider"":""Ada"",""decideBy"":null,""file"":""docs/product/requirements/late-fee.md"",""line"":3,""fields"":{""approvedHash"":""stale"",""risk"":[""money""]},""links"":{}}";
        var asking = BorrowCopyUseCase.Replace(
            @"""fields"":{""risk"":[""money""]}",
            @"""fields"":{""questions"":[{""question"":""Who approves shared copies?"",""decider"":""Ada"",""decideBy"":""2999-01-01""}]}",
            StringComparison.Ordinal);
        var proves = @"[{""scenario"":""Library.Tests.BorrowTests.Borrowing"",""claim"":""scenario/borrow-copy.borrowing-succeeds"",""confirmed"":true,""passing"":true,""indirect"":false}]";
        var model = WriteModel(
            temp,
            string.Join(",", requirement, asking, BorrowingScenario),
            proves,
            @"[""scenario/borrow-copy.borrowing-succeeds""]");
        var result = Cli.Run("book", "--model", model, "--out", Out(temp));
        Assert.Equal(0, result.Status);
        var html = ReviewHtml(temp);
        Assert.Contains("reopened", html, StringComparison.Ordinal);
        Assert.Contains("full re-read", html, StringComparison.Ordinal);
        Assert.Contains("1 open question", html, StringComparison.Ordinal);
        Assert.Contains("1 scenario, 1 confirmed, 0 passing, 1 stale", html, StringComparison.Ordinal);
        Assert.True(
            html.IndexOf("batch-intent-requirement", StringComparison.Ordinal)
                < html.IndexOf("batch-use-case-lending-borrow-copy", StringComparison.Ordinal),
            "The batch holding the full re-read sorts first.");
    }

    static string WriteModel(TempFolder temp, string records, string proves, string stale)
    {
        var path = Path.Combine(temp.Path, "model.json");
        File.WriteAllText(
            path,
            "{\"records\":[" + records + "],"
                + "\"links\":{\"proves\":" + proves + "},"
                + "\"status\":{},\"requirementStatus\":{},\"reachableStates\":{},\"stale\":" + stale + "}");
        return path;
    }

    static string Out(TempFolder temp) => Path.Combine(temp.Path, "out");

    static string ReviewHtml(TempFolder temp) =>
        File.ReadAllText(Path.Combine(Out(temp), "review", "index.html"));
}
