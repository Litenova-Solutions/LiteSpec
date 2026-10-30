using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Review;

/// <summary>Proves the four approval tool fixes: screen layer, reaction and job scenario stamps, decision date, and transcribed reporting.</summary>
/// <follows>litespec/core/release#approve-drafts-in-bounded-batches</follows>
public sealed class ApprovalToolFixTests
{
    static string Screen(string root) => ReviewPages.Page(
        root, "docs", "behavior", "screens", "library-app.loans.md");

    static string Reaction(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "reactions", "send-due-notice-on-loan-borrowed.md");

    static string Job(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "jobs", "nightly-due-scan.md");

    static string LoanPeriod(string root) => ReviewPages.Page(
        root, "docs", "product", "decisions", "0001-loan-period.md");

    static string BorrowCopy(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md");

    static string Borrowing(string root) => ReviewPages.Page(
        root, "docs", "product", "lending", "borrowing.md");

    /// <summary>A screen is Behavior: the queue names its layer behavior and approval promotes it to reviewed.</summary>
    /// <proves>litespec/core/release#compute-the-review-queue-from-the-model</proves>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Approve_Screen_Batch_Promotes_To_Reviewed()
    {
        using var fixture = ModelFixture.Prepare("large");

        ReviewPages.DraftPage(Screen(fixture.Path));

        var queue = Cli.Run("model", "review", "--root", fixture.Path, "--format", "json");

        Assert.Equal(0, queue.Status);

        var batches = ReviewPages.OutputJson(queue)["batches"]!.AsArray();
        var screen = batches
            .SelectMany(batch => batch!["records"]!.AsArray())
            .First(record => record!["id"]!.GetValue<string>() == "screen/library-app.loans");

        Assert.Equal("behavior", screen!["layer"]!.GetValue<string>());

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "library-app/screen", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);

        var (front, _) = ReviewPages.Read(Screen(fixture.Path));

        Assert.Equal("reviewed", front["status"]!.GetValue<string>());
        Assert.Equal("lending-team", front["approvedBy"]!.GetValue<string>());
        Assert.Equal("2026-11-20", front["approvedOn"]!.GetValue<string>());
        Assert.True(front["approvedHash"]!.GetValue<string>().Length == 8);
    }

    /// <summary>A reaction batch holds the scenarios on its page, each stamped with its own approval data.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Approve_Reaction_Batch_Stamps_Page_Scenarios()
    {
        using var fixture = ModelFixture.Prepare("large");

        ReviewPages.AppendEntry(Reaction(fixture.Path), new JsonObject
        {
            ["type"] = "scenario",
            ["id"] = "scenario/borrow-copy.due-notice-sent-on-borrow",
            ["title"] = "Due notice sent on borrow",
            ["owner"] = "lending-team",
            ["given"] = "A reader borrows a copy.",
            ["when"] = "The loan is borrowed.",
            ["then"] = "The reader receives a due notice.",
            ["result"] = "result/borrow-copy.borrowed",
        });
        ReviewPages.DraftPage(Reaction(fixture.Path));

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "reaction/lending.send-due-notice-on-loan-borrowed", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("2 records approved, 0 held.", result.Output);

        var (front, _) = ReviewPages.Read(Reaction(fixture.Path));

        Assert.Equal("reviewed", front["status"]!.GetValue<string>());

        var scenario = front["records"]!.AsArray()
            .OfType<JsonObject>()
            .First(entry => entry["id"]!.GetValue<string>() == "scenario/borrow-copy.due-notice-sent-on-borrow");

        Assert.Equal("lending-team", scenario["approvedBy"]!.GetValue<string>());
        Assert.Equal("2026-11-20", scenario["approvedOn"]!.GetValue<string>());
        Assert.True(scenario["approvedHash"]!.GetValue<string>().Length == 8);
    }

    /// <summary>A job batch holds the scenarios on its page, each stamped with its own approval data.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Approve_Job_Batch_Stamps_Page_Scenarios()
    {
        using var fixture = ModelFixture.Prepare("large");

        ReviewPages.AppendEntry(Job(fixture.Path), new JsonObject
        {
            ["type"] = "scenario",
            ["id"] = "scenario/borrow-copy.due-scan-marks-overdue-loan",
            ["title"] = "Due scan marks overdue loan",
            ["owner"] = "lending-team",
            ["given"] = "A loan passed its due date.",
            ["when"] = "The nightly scan runs.",
            ["then"] = "The loan is marked overdue.",
            ["result"] = "result/borrow-copy.borrowed",
        });
        ReviewPages.DraftPage(Job(fixture.Path));

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "job/lending.nightly-due-scan", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("2 records approved, 0 held.", result.Output);

        var (front, _) = ReviewPages.Read(Job(fixture.Path));

        Assert.Equal("reviewed", front["status"]!.GetValue<string>());

        var scenario = front["records"]!.AsArray()
            .OfType<JsonObject>()
            .First(entry => entry["id"]!.GetValue<string>() == "scenario/borrow-copy.due-scan-marks-overdue-loan");

        Assert.Equal("lending-team", scenario["approvedBy"]!.GetValue<string>());
        Assert.Equal("2026-11-20", scenario["approvedOn"]!.GetValue<string>());
        Assert.True(scenario["approvedHash"]!.GetValue<string>().Length == 8);
    }

    /// <summary>Approving a decision writes decidedOn beside the chosen option it names.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Approve_Decision_Batch_Writes_DecidedOn()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftPage(LoanPeriod(fixture.Path));
        ReviewPages.PatchPage(LoanPeriod(fixture.Path), front => front.Remove("decidedOn"));

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "decision/decision/0001-loan-period", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);

        var (front, _) = ReviewPages.Read(LoanPeriod(fixture.Path));

        Assert.Equal("approved", front["status"]!.GetValue<string>());
        Assert.Equal("2026-11-20", front["decidedOn"]!.GetValue<string>());
        Assert.Equal("21 days", front["chosen"]!.GetValue<string>());
        Assert.Equal("lending-team", front["approvedBy"]!.GetValue<string>());
        Assert.Equal("2026-11-20", front["approvedOn"]!.GetValue<string>());
        Assert.True(front["approvedHash"]!.GetValue<string>().Length == 8);
    }

    /// <summary>Transcribed-reviewed fires only while the Intent parent it rests on is unapproved.</summary>
    /// <proves>litespec/core/model#hold-transcribed-records-in-draft</proves>
    [Fact]
    public void Check_Transcribed_Reviewed_Follows_Its_Parent()
    {
        using var fixture = ModelFixture.Prepare("large");

        ReviewPages.PatchPage(BorrowCopy(fixture.Path), front => front["source"] = "transcribed");

        var approved = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("transcribed-reviewed", approved.Output);

        ReviewPages.PatchPage(
            BorrowCopy(fixture.Path),
            front =>
            {
                front.Remove("fulfills");
                front.Remove("serves");
            });

        var orphaned = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("transcribed-reviewed", orphaned.Output);

        ReviewPages.PatchPage(
            BorrowCopy(fixture.Path),
            front =>
            {
                front["fulfills"] = new JsonArray("requirement/borrowing.max-active-loans");
                front["serves"] = new JsonArray("journey/borrow-and-return#s2");
            });
        ReviewPages.DraftEntry(Borrowing(fixture.Path), "requirement/borrowing.max-active-loans");

        var draft = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Contains("transcribed-reviewed", draft.Output);
        Assert.Contains("use-case/lending.borrow-copy", draft.Output);
    }
}
