using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Review;

/// <summary>Proves <c>litespec model approve</c> approves many batches in one run.</summary>
/// <follows>litespec/core/release#approve-drafts-in-bounded-batches</follows>
public sealed class BatchManyApproveTests
{
    static string BorrowCopy(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md");

    static string LoanAggregate(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "loans", "README.md");

    static string Borrowing(string root) => ReviewPages.Page(
        root, "docs", "product", "lending", "borrowing.md");

    static string Manifest(string root, string name) => ReviewPages.Page(
        root, "docs", "proof", "approvals", name);

    /// <summary>Two batches in one run approve both and write two manifests.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    /// <proves>litespec/core/release#write-a-manifest-for-every-batch-approval</proves>
    [Fact]
    public void Approve_Two_Batches_In_One_Run_Writes_Two_Manifests()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftPage(BorrowCopy(fixture.Path));
        ReviewPages.DraftEntry(LoanAggregate(fixture.Path), "rule/loan.has-one-borrower");

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy",
            "--batch", "lending/rule",
            "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("2 batches approved, 0 refused.", result.Output);
        Assert.Contains("2026-11-20-use-case-lending-borrow-copy.json", result.Output);
        Assert.Contains("2026-11-20-lending-rule.json", result.Output);
        Assert.True(File.Exists(Manifest(fixture.Path, "2026-11-20-use-case-lending-borrow-copy.json")));
        Assert.True(File.Exists(Manifest(fixture.Path, "2026-11-20-lending-rule.json")));

        var (front, _) = ReviewPages.Read(BorrowCopy(fixture.Path));

        Assert.Equal("reviewed", front["status"]!.GetValue<string>());

        var (loans, _) = ReviewPages.Read(LoanAggregate(fixture.Path));
        var rule = loans["records"]!.AsArray()
            .OfType<JsonObject>()
            .First(entry => entry["id"]!.GetValue<string>() == "rule/loan.has-one-borrower");

        Assert.Equal("reviewed", rule["status"]!.GetValue<string>());
    }

    /// <summary>A refused batch writes nothing and does not stop the next batch.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Approve_Refused_Batch_Does_Not_Stop_The_Next()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftPage(BorrowCopy(fixture.Path));
        ReviewPages.AddPageQuestion(BorrowCopy(fixture.Path));
        ReviewPages.DraftEntry(LoanAggregate(fixture.Path), "rule/loan.has-one-borrower");

        var before = File.ReadAllText(BorrowCopy(fixture.Path));

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy",
            "--batch", "lending/rule",
            "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(1, result.Status);
        Assert.Contains("use-case/lending.borrow-copy: refused:", result.Output);
        Assert.Contains("has open questions", result.Output);
        Assert.Contains("lending/rule: approved:", result.Output);
        Assert.Contains("1 batches approved, 1 refused.", result.Output);
        Assert.Equal(before, File.ReadAllText(BorrowCopy(fixture.Path)));
        Assert.False(File.Exists(Manifest(fixture.Path, "2026-11-20-use-case-lending-borrow-copy.json")));
        Assert.True(File.Exists(Manifest(fixture.Path, "2026-11-20-lending-rule.json")));
    }

    /// <summary>An intent batch earlier in the run lets a behavior batch that waits on it pass.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    /// <proves>litespec/core/release#refuse-a-record-whose-effective-status-stays-draft</proves>
    [Fact]
    public void Approve_Intent_Batch_Lets_Waiting_Behavior_Batch_Pass()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftEntry(Borrowing(fixture.Path), "requirement/borrowing.max-active-loans");
        ReviewPages.DraftPage(BorrowCopy(fixture.Path));

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "intent/requirement",
            "--batch", "use-case/lending.borrow-copy",
            "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("2 batches approved, 0 refused.", result.Output);
        Assert.True(File.Exists(Manifest(fixture.Path, "2026-11-20-intent-requirement.json")));
        Assert.True(File.Exists(Manifest(fixture.Path, "2026-11-20-use-case-lending-borrow-copy.json")));

        var (front, _) = ReviewPages.Read(BorrowCopy(fixture.Path));

        Assert.Equal("reviewed", front["status"]!.GetValue<string>());
    }

    /// <summary>A batches file ignores comments and blank lines and carries a per-batch report.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    /// <proves>litespec/core/release#write-a-manifest-for-every-batch-approval</proves>
    [Fact]
    public void Approve_Batches_File_With_Comments_And_Per_Batch_Report()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftPage(BorrowCopy(fixture.Path));
        ReviewPages.AddPageRisk(BorrowCopy(fixture.Path));
        ReviewPages.DraftEntry(LoanAggregate(fixture.Path), "rule/loan.has-one-borrower");

        var report = ReviewPages.Page(fixture.Path, "review-report.md");

        File.WriteAllText(report, "# Agent review\n\nThe grant reads correctly.\n");

        var batches = ReviewPages.Page(fixture.Path, "batches.txt");

        File.WriteAllText(batches,
            "# Batches to approve\n"
            + "\n"
            + $"use-case/lending.borrow-copy {report}\n"
            + "# The rule batch needs no report.\n"
            + "lending/rule\n");

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batches", batches,
            "--by", "lending-team", "--second-reviewer", "agent", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("2 batches approved, 0 refused.", result.Output);
        Assert.True(File.Exists(Manifest(fixture.Path, "2026-11-20-use-case-lending-borrow-copy.json")));
        Assert.True(File.Exists(Manifest(fixture.Path, "2026-11-20-lending-rule.json")));

        var manifest = (JsonObject)JsonNode.Parse(File.ReadAllText(
            Manifest(fixture.Path, "2026-11-20-use-case-lending-borrow-copy.json")))!;

        Assert.Equal(report, manifest["reviewReport"]!.GetValue<string>());
    }

    /// <summary>A dry run over many batches prints the plans and writes nothing.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Approve_Many_Batches_Dry_Run_Writes_Nothing()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftPage(BorrowCopy(fixture.Path));
        ReviewPages.DraftEntry(LoanAggregate(fixture.Path), "rule/loan.has-one-borrower");

        var borrowBefore = File.ReadAllText(BorrowCopy(fixture.Path));
        var loansBefore = File.ReadAllText(LoanAggregate(fixture.Path));

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy",
            "--batch", "lending/rule",
            "--by", "lending-team", "--on", "2026-11-20", "--dry-run");

        Assert.Equal(0, result.Status);
        Assert.Contains("would be promoted", result.Output);
        Assert.Equal(borrowBefore, File.ReadAllText(BorrowCopy(fixture.Path)));
        Assert.Equal(loansBefore, File.ReadAllText(LoanAggregate(fixture.Path)));
        Assert.False(Directory.Exists(ReviewPages.Page(fixture.Path, "docs", "proof", "approvals")));
    }
}
