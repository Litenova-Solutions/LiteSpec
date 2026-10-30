using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Review;

/// <summary>Proves <c>litespec model approve --batch</c> approves one bounded batch.</summary>
/// <follows>litespec/core/release#approve-drafts-in-bounded-batches</follows>
public sealed class BatchApproveTests
{
    static string BorrowCopy(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md");

    static string LoanAggregate(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "loans", "README.md");

    static string Borrowing(string root) => ReviewPages.Page(
        root, "docs", "product", "lending", "borrowing.md");

    static string LoanPeriod(string root) => ReviewPages.Page(
        root, "docs", "product", "decisions", "0001-loan-period.md");

    /// <summary>A use-case batch promotes the use case, its results and scenarios, and writes the manifest.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    /// <proves>litespec/core/release#write-a-manifest-for-every-batch-approval</proves>
    [Fact]
    public void Approve_Use_Case_Batch_Promotes_And_Writes_Manifest()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftPage(BorrowCopy(fixture.Path));
        ReviewPages.AppendEntry(BorrowCopy(fixture.Path), new JsonObject
        {
            ["type"] = "scenario",
            ["id"] = "scenario/borrow-copy.borrowing-with-a-receipt-succeeds",
            ["title"] = "Borrowing with a receipt succeeds",
            ["owner"] = "lending-team",
            ["given"] = "A reader holds an available copy.",
            ["when"] = "The reader borrows the copy.",
            ["then"] = "The copy is lent.",
            ["result"] = "result/borrow-copy.borrowed",
        });

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("4 records approved, 0 held.", result.Output);

        var (front, _) = ReviewPages.Read(BorrowCopy(fixture.Path));

        Assert.Equal("reviewed", front["status"]!.GetValue<string>());
        Assert.Equal("lending-team", front["approvedBy"]!.GetValue<string>());
        Assert.Equal("2026-11-20", front["approvedOn"]!.GetValue<string>());
        Assert.True(front["approvedHash"]!.GetValue<string>().Length == 8);

        foreach (var entry in front["records"]!.AsArray().OfType<JsonObject>())
        {
            Assert.Equal("lending-team", entry["approvedBy"]!.GetValue<string>());
            Assert.Equal("2026-11-20", entry["approvedOn"]!.GetValue<string>());
            Assert.True(entry["approvedHash"]!.GetValue<string>().Length == 8);
        }

        var manifestPath = ReviewPages.Page(
            fixture.Path, "docs", "proof", "approvals", "2026-11-20-use-case-lending-borrow-copy.json");
        var manifest = (JsonObject)JsonNode.Parse(File.ReadAllText(manifestPath))!;

        Assert.Equal("use-case/lending.borrow-copy", manifest["selector"]!.GetValue<string>());
        Assert.Equal("lending-team", manifest["approvedBy"]!.GetValue<string>());
        Assert.Equal("2026-11-20", manifest["approvedOn"]!.GetValue<string>());
        Assert.Equal(0.1, manifest["sampleShare"]!.GetValue<double>());
        Assert.Single(manifest["sample"]!.AsArray());

        var records = manifest["records"]!.AsArray();

        Assert.Equal(4, records.Count);
        Assert.All(records, record => Assert.Equal("promoted", record!["action"]!.GetValue<string>()));
        Assert.Equal(
            records.Select(record => record!["id"]!.GetValue<string>()).OrderBy(id => id, StringComparer.Ordinal),
            records.Select(record => record!["id"]!.GetValue<string>()));
    }

    /// <summary>A decision batch promotes the decision first, then the records it decides.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Approve_Decision_Batch_Promotes_Decision_First()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftPage(LoanPeriod(fixture.Path));
        ReviewPages.DraftEntry(Borrowing(fixture.Path), "requirement/borrowing.loan-period-days");

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "decision/decision/0001-loan-period", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("2 records approved, 0 held.", result.Output);

        var (decision, _) = ReviewPages.Read(LoanPeriod(fixture.Path));

        Assert.Equal("approved", decision["status"]!.GetValue<string>());
        Assert.Equal("lending-team", decision["approvedBy"]!.GetValue<string>());

        var (borrowing, _) = ReviewPages.Read(Borrowing(fixture.Path));
        var requirement = borrowing["records"]!.AsArray()
            .OfType<JsonObject>()
            .First(entry => entry["id"]!.GetValue<string>() == "requirement/borrowing.loan-period-days");

        Assert.Equal("approved", requirement["status"]!.GetValue<string>());
        Assert.Equal("lending-team", requirement["approvedBy"]!.GetValue<string>());
    }

    /// <summary>A module and type batch approves its tier 3 drafts; a tier 1 rule waits in a batch of its own.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Approve_Module_Type_Batch_Leaves_Tier1_Out()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftEntry(LoanAggregate(fixture.Path), "rule/loan.has-one-borrower");
        ReviewPages.DraftEntry(LoanAggregate(fixture.Path), "rule/loan.extended-before-due");
        ReviewPages.AddEntryRisk(LoanAggregate(fixture.Path), "rule/loan.extended-before-due");

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "lending/rule", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("1 record approved, 0 held.", result.Output);

        // The tier 1 rule is no member of the module batch: it waits in a batch of its own.
        var queue = Cli.Run("model", "review", "--root", fixture.Path);
        Assert.Contains("rule/loan.extended-before-due  tier 1  1 record", queue.Output);

        var (front, _) = ReviewPages.Read(LoanAggregate(fixture.Path));
        var entries = front["records"]!.AsArray().OfType<JsonObject>().ToList();
        var approved = entries.First(entry => entry["id"]!.GetValue<string>() == "rule/loan.has-one-borrower");
        var leftOut = entries.First(entry => entry["id"]!.GetValue<string>() == "rule/loan.extended-before-due");

        Assert.Equal("reviewed", approved["status"]!.GetValue<string>());
        Assert.Equal("lending-team", approved["approvedBy"]!.GetValue<string>());
        Assert.Equal("draft", leftOut["status"]!.GetValue<string>());
        Assert.Null(leftOut["approvedHash"]);

        var manifest = (JsonObject)JsonNode.Parse(File.ReadAllText(ReviewPages.Page(
            fixture.Path, "docs", "proof", "approvals", "2026-11-20-lending-rule.json")))!;

        Assert.Single(manifest["records"]!.AsArray());
        Assert.Equal(
            "rule/loan.has-one-borrower",
            manifest["records"]![0]!["id"]!.GetValue<string>());
    }

    /// <summary>Records already approved with an unchanged hash are held and untouched.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Approve_Held_Records_Listed_And_Untouched()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftEntry(BorrowCopy(fixture.Path), "result/borrow-copy.copy-unavailable");

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("1 record approved, 2 held.", result.Output);

        var manifest = (JsonObject)JsonNode.Parse(File.ReadAllText(ReviewPages.Page(
            fixture.Path, "docs", "proof", "approvals", "2026-11-20-use-case-lending-borrow-copy.json")))!;
        var records = manifest["records"]!.AsArray();

        Assert.Equal(3, records.Count);
        Assert.Equal(
            "held",
            records.First(record => record!["id"]!.GetValue<string>() == "use-case/lending.borrow-copy")!["action"]!.GetValue<string>());
        Assert.Equal(
            "promoted",
            records.First(record => record!["id"]!.GetValue<string>() == "result/borrow-copy.copy-unavailable")!["action"]!.GetValue<string>());

        // The held records keep their approval data.
        Assert.Contains("\"approvedHash\": \"fa5ceb0c\"", File.ReadAllText(BorrowCopy(fixture.Path)));
        Assert.Contains("\"approvedHash\": \"a1559ff6\"", File.ReadAllText(BorrowCopy(fixture.Path)));
    }

    /// <summary>A reopened record is re-stamped with its new hash.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Approve_Reopened_Record_Re_Stamped()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.PatchEntry(
            BorrowCopy(fixture.Path), "result/borrow-copy.borrowed",
            entry => entry["meaning"] = "The copy is lent to the reader.");

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("re-stamped", result.Output);
        Assert.DoesNotContain("a1559ff6", File.ReadAllText(BorrowCopy(fixture.Path)));

        var (front, _) = ReviewPages.Read(BorrowCopy(fixture.Path));
        var entry = front["records"]!.AsArray()
            .OfType<JsonObject>()
            .First(entry => entry["id"]!.GetValue<string>() == "result/borrow-copy.borrowed");

        Assert.Equal("lending-team", entry["approvedBy"]!.GetValue<string>());
        Assert.Equal("2026-11-20", entry["approvedOn"]!.GetValue<string>());
        Assert.Equal(
            ReviewPages.SemanticHash("result", "result/borrow-copy.borrowed", entry),
            entry["approvedHash"]!.GetValue<string>());
    }
}
