using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Review;

/// <summary>Proves <c>litespec model approve --batch</c> refuses the whole batch when one guard fails.</summary>
/// <follows>litespec/core/release#approve-drafts-in-bounded-batches</follows>
public sealed class BatchGuardTests
{
    static string BorrowCopy(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md");

    static string Borrowing(string root) => ReviewPages.Page(
        root, "docs", "product", "lending", "borrowing.md");

    static void DraftTier1UseCase(string root)
    {
        ReviewPages.DraftPage(BorrowCopy(root));
        ReviewPages.AddPageRisk(BorrowCopy(root));
    }

    /// <summary>Adds another owner to the project, so the second-reading guard applies.</summary>
    static void AddOwner(string root, string owner = "second-team")
    {
        var path = Path.Combine(root, "litespec.project.json");
        var project = (System.Text.Json.Nodes.JsonObject)System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
        ((System.Text.Json.Nodes.JsonArray)project["owners"]!).Add(owner);
        File.WriteAllText(path, project.ToJsonString() + "\n");
    }

    /// <summary>A tier 1 batch without a named owner approver is refused and writes nothing.</summary>
    /// <proves>litespec/core/release#refuse-a-batch-without-a-named-approver</proves>
    [Fact]
    public void Guard_Tier1_Without_Named_Approver_Refuses()
    {
        using var fixture = ModelFixture.Prepare("small");

        DraftTier1UseCase(fixture.Path);

        var before = File.ReadAllText(BorrowCopy(fixture.Path));

        var missing = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--second-reviewer", "second-team");

        Assert.Equal(1, missing.Status);
        Assert.Contains("use-case/lending.borrow-copy: needs --by naming a project owner", missing.Output);

        var auto = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "auto (veto window)",
            "--second-reviewer", "second-team");

        Assert.Equal(1, auto.Status);
        Assert.Contains("needs --by naming a project owner", auto.Output);

        var stranger = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "stranger",
            "--second-reviewer", "second-team");

        Assert.Equal(1, stranger.Status);
        Assert.Contains("'stranger' is not a project owner", stranger.Output);

        Assert.Equal(before, File.ReadAllText(BorrowCopy(fixture.Path)));
        Assert.False(Directory.Exists(ReviewPages.Page(fixture.Path, "docs", "proof", "approvals")));
    }

    /// <summary>A tier 1 batch with a named owner approver and second reviewer passes.</summary>
    /// <proves>litespec/core/release#refuse-a-batch-without-a-named-approver</proves>
    /// <proves>litespec/core/release#refuse-self-review-alone-for-tier-1-behavior</proves>
    [Fact]
    public void Guard_Tier1_With_Named_Approver_Passes()
    {
        using var fixture = ModelFixture.Prepare("small");

        DraftTier1UseCase(fixture.Path);

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "lending-team",
            "--second-reviewer", "second-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("3 records approved, 0 held.", result.Output);
    }

    /// <summary>A tier 1 batch on a solo project needs no second reading.</summary>
    /// <proves>litespec/core/release#refuse-self-review-alone-for-tier-1-behavior</proves>
    [Fact]
    public void Guard_Tier1_Solo_Owner_Needs_No_Second_Reviewer()
    {
        using var fixture = ModelFixture.Prepare("small");

        DraftTier1UseCase(fixture.Path);

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("3 records approved, 0 held.", result.Output);
    }

    /// <summary>A tier 1 batch without a second reading is refused and writes nothing.</summary>
    /// <proves>litespec/core/release#refuse-self-review-alone-for-tier-1-behavior</proves>
    [Fact]
    public void Guard_Tier1_Without_Second_Reviewer_Refuses()
    {
        using var fixture = ModelFixture.Prepare("small");

        DraftTier1UseCase(fixture.Path);
        AddOwner(fixture.Path);

        var before = File.ReadAllText(BorrowCopy(fixture.Path));

        var missing = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "lending-team");

        Assert.Equal(1, missing.Status);
        Assert.Contains(
            "use-case/lending.borrow-copy: tier 1 needs --second-reviewer other than the approver",
            missing.Output);
        Assert.Contains("holds one name", missing.Output);

        var same = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "lending-team",
            "--second-reviewer", "lending-team");

        Assert.Equal(1, same.Status);
        Assert.Contains("names the approver", same.Output);

        var agent = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "lending-team",
            "--second-reviewer", "agent");

        Assert.Equal(1, agent.Status);
        Assert.Contains("agent needs an existing --review-report file", agent.Output);

        Assert.Equal(before, File.ReadAllText(BorrowCopy(fixture.Path)));
        Assert.False(Directory.Exists(ReviewPages.Page(fixture.Path, "docs", "proof", "approvals")));
    }

    /// <summary>An agent second reading with an existing report file passes.</summary>
    /// <proves>litespec/core/release#refuse-self-review-alone-for-tier-1-behavior</proves>
    [Fact]
    public void Guard_Tier1_With_Agent_Report_Passes()
    {
        using var fixture = ModelFixture.Prepare("small");

        DraftTier1UseCase(fixture.Path);

        var report = ReviewPages.Page(fixture.Path, "review-report.md");

        File.WriteAllText(report, "# Agent review\n\nThe grant reads correctly.\n");

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "lending-team",
            "--second-reviewer", "agent", "--review-report", report, "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("3 records approved, 0 held.", result.Output);
    }

    /// <summary>A batch holding an open question is refused until the question is answered.</summary>
    /// <proves>litespec/core/release#refuse-a-batch-holding-an-open-question</proves>
    [Fact]
    public void Guard_Open_Question_Refuses_Until_Answered()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftPage(BorrowCopy(fixture.Path));
        ReviewPages.AddPageQuestion(BorrowCopy(fixture.Path));

        var before = File.ReadAllText(BorrowCopy(fixture.Path));

        var refused = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "lending-team");

        Assert.Equal(1, refused.Status);
        Assert.Contains("use-case/lending.borrow-copy: has open questions", refused.Output);
        Assert.Equal(before, File.ReadAllText(BorrowCopy(fixture.Path)));
        Assert.False(Directory.Exists(ReviewPages.Page(fixture.Path, "docs", "proof", "approvals")));

        using var passing = ModelFixture.Prepare("small");

        ReviewPages.DraftPage(BorrowCopy(passing.Path));

        var approved = Cli.Run(
            "model", "approve", "--root", passing.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, approved.Status);
        Assert.Contains("3 records approved, 0 held.", approved.Output);
    }

    /// <summary>A record resting on a draft parent is refused until the parent is approved.</summary>
    /// <proves>litespec/core/release#refuse-a-record-whose-effective-status-stays-draft</proves>
    [Fact]
    public void Guard_Effective_Status_Refuses_Until_Parent_Approved()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftEntry(Borrowing(fixture.Path), "requirement/borrowing.max-active-loans");
        ReviewPages.DraftPage(BorrowCopy(fixture.Path));

        var before = File.ReadAllText(BorrowCopy(fixture.Path));

        var refused = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "lending-team");

        Assert.Equal(1, refused.Status);
        Assert.Contains(
            "use-case/lending.borrow-copy: stays draft behind draft requirement/borrowing.max-active-loans",
            refused.Output);
        Assert.Equal(before, File.ReadAllText(BorrowCopy(fixture.Path)));

        var parent = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "decision/decision/0001-loan-period", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, parent.Status);
        Assert.Contains("1 record approved, 4 held.", parent.Output);

        var approved = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, approved.Status);
        Assert.Contains("3 records approved, 0 held.", approved.Output);
    }

    /// <summary>A dry run prints the plan, failures, and sample, and writes nothing.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Approve_Dry_Run_Writes_Nothing()
    {
        using var fixture = ModelFixture.Prepare("small");

        DraftTier1UseCase(fixture.Path);
        AddOwner(fixture.Path);

        var before = File.ReadAllText(BorrowCopy(fixture.Path));

        var refused = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "lending-team", "--dry-run");

        Assert.Equal(1, refused.Status);
        Assert.Contains("would be promoted", refused.Output);
        Assert.Contains("tier 1 needs --second-reviewer", refused.Output);
        Assert.Contains("Sample (0 of 0", refused.Output);
        Assert.Equal(before, File.ReadAllText(BorrowCopy(fixture.Path)));
        Assert.False(Directory.Exists(ReviewPages.Page(fixture.Path, "docs", "proof", "approvals")));

        var planned = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "lending-team",
            "--second-reviewer", "second-team", "--dry-run");

        Assert.Equal(0, planned.Status);
        Assert.Contains("use-case/lending.borrow-copy would be promoted to reviewed", planned.Output);
        Assert.Equal(before, File.ReadAllText(BorrowCopy(fixture.Path)));
        Assert.False(Directory.Exists(ReviewPages.Page(fixture.Path, "docs", "proof", "approvals")));
    }
}
