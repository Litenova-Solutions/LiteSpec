using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Review;

/// <summary>Proves the approval tool fixes: screen layer, automation scenario stamps, decision approval data, and transcribed reporting.</summary>
/// <follows>litespec/core/release#approve-drafts-in-bounded-batches</follows>
public sealed class ApprovalToolFixTests
{
    static string Screen(string root) => ReviewPages.Page(
        root, "docs", "behavior", "screens", "library-app.loans.md");

    static string EventAutomation(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "automations", "send-due-notice-on-loan-borrowed.md");

    static string ScheduledAutomation(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "automations", "nightly-due-scan.md");

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

    /// <summary>An event automation batch holds the scenarios on its page, each stamped with its own approval data.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Approve_Event_Automation_Batch_Stamps_Page_Scenarios()
    {
        using var fixture = ModelFixture.Prepare("large");

        ReviewPages.AppendEntry(EventAutomation(fixture.Path), new JsonObject
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
        ReviewPages.DraftPage(EventAutomation(fixture.Path));

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "automation/lending.send-due-notice-on-loan-borrowed", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("2 records approved, 0 held.", result.Output);

        var (front, _) = ReviewPages.Read(EventAutomation(fixture.Path));

        Assert.Equal("reviewed", front["status"]!.GetValue<string>());

        var scenario = front["records"]!.AsArray()
            .OfType<JsonObject>()
            .First(entry => entry["id"]!.GetValue<string>() == "scenario/borrow-copy.due-notice-sent-on-borrow");

        Assert.Equal("lending-team", scenario["approvedBy"]!.GetValue<string>());
        Assert.Equal("2026-11-20", scenario["approvedOn"]!.GetValue<string>());
        Assert.True(scenario["approvedHash"]!.GetValue<string>().Length == 8);
    }

    /// <summary>A scheduled automation batch holds the scenarios on its page, each stamped with its own approval data.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Approve_Scheduled_Automation_Batch_Stamps_Page_Scenarios()
    {
        using var fixture = ModelFixture.Prepare("large");

        ReviewPages.AppendEntry(ScheduledAutomation(fixture.Path), new JsonObject
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
        ReviewPages.DraftPage(ScheduledAutomation(fixture.Path));

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "automation/lending.nightly-due-scan", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("2 records approved, 0 held.", result.Output);

        var (front, _) = ReviewPages.Read(ScheduledAutomation(fixture.Path));

        Assert.Equal("reviewed", front["status"]!.GetValue<string>());

        var scenario = front["records"]!.AsArray()
            .OfType<JsonObject>()
            .First(entry => entry["id"]!.GetValue<string>() == "scenario/borrow-copy.due-scan-marks-overdue-loan");

        Assert.Equal("lending-team", scenario["approvedBy"]!.GetValue<string>());
        Assert.Equal("2026-11-20", scenario["approvedOn"]!.GetValue<string>());
        Assert.True(scenario["approvedHash"]!.GetValue<string>().Length == 8);
    }

    /// <summary>Approving a decision writes approval data and no decidedOn.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Approve_Decision_Batch_Writes_Approval_Without_DecidedOn()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftPage(LoanPeriod(fixture.Path));

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "decision/decision/0001-loan-period", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);

        var (front, _) = ReviewPages.Read(LoanPeriod(fixture.Path));

        Assert.Equal("approved", front["status"]!.GetValue<string>());
        Assert.Equal("21 days", front["choice"]!.GetValue<string>());
        Assert.Equal("lending-team", front["approvedBy"]!.GetValue<string>());
        Assert.Equal("2026-11-20", front["approvedOn"]!.GetValue<string>());
        Assert.True(front["approvedHash"]!.GetValue<string>().Length == 8);
        Assert.False(front.ContainsKey("decidedOn"));
    }

    /// <summary>Transcribed-reviewed fires only while the Intent parent it rests on is unapproved.</summary>
    /// <proves>litespec/core/model#hold-transcribed-records-in-draft</proves>
    [Fact]
    public void Check_Transcribed_Reviewed_Follows_Its_Parent()
    {
        using var fixture = ModelFixture.Prepare("large");

        ReviewPages.PatchPage(BorrowCopy(fixture.Path), front => front["provenance"] = "transcribed");

        var approved = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("transcription-status-invalid", approved.Output);

        ReviewPages.PatchPage(
            BorrowCopy(fixture.Path),
            front =>
            {
                front.Remove("fulfills");
                front.Remove("serves");
            });

        var orphaned = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("transcription-status-invalid", orphaned.Output);

        ReviewPages.PatchPage(
            BorrowCopy(fixture.Path),
            front =>
            {
                front["fulfills"] = new JsonArray("requirement/borrowing.max-active-loans");
                front["serves"] = new JsonArray("journey/borrow-and-return#s2");
            });
        ReviewPages.DraftEntry(Borrowing(fixture.Path), "requirement/borrowing.max-active-loans");

        var draft = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Contains("transcription-status-invalid", draft.Output);
        Assert.Contains("use-case/lending.borrow-copy", draft.Output);
    }

    /// <summary>A journey batches with the goal it serves, so a question on one goal holds back only its journeys.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Journeys_Batch_With_The_Goal_They_Serve()
    {
        using var temp = Cli.Temp("review");
        var config = LiteSpec.Tests.Model.Checks.TestModel.Config(temp.Path);
        var faster = LiteSpec.Tests.Model.Checks.TestModel.Record("journey", "journey/borrow-and-return", Path.Combine(temp.Path, "borrow.md"), """{"serves": ["goal/faster-pickup"]}""");
        var wider = LiteSpec.Tests.Model.Checks.TestModel.Record("journey", "journey/join-the-library", Path.Combine(temp.Path, "join.md"), """{"serves": ["goal/more-readers"]}""");
        var model = LiteSpec.Tests.Model.Checks.TestModel.Model(config, faster, wider);

        Assert.Equal("faster-pickup", LiteSpec.Model.ReviewQueue.ModuleOf(model, faster));
        Assert.Equal("more-readers", LiteSpec.Model.ReviewQueue.ModuleOf(model, wider));
    }

    /// <summary>A decision that names no choice is not decided, so its batch is refused.</summary>
    /// <proves>litespec/core/intent#shape-each-decision-page</proves>
    [Fact]
    public void Approve_Decision_Without_Choice_Is_Refused()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftPage(LoanPeriod(fixture.Path));
        ReviewPages.PatchPage(LoanPeriod(fixture.Path), front => front.Remove("choice"));

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "decision/decision/0001-loan-period", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(1, result.Status);
        Assert.Contains("names no choice", result.Output, StringComparison.Ordinal);
        Assert.Equal("draft", ReviewPages.Read(LoanPeriod(fixture.Path)).Front["status"]!.GetValue<string>());
    }

    /// <summary>Stamping a page leaves an entry that inherited its status, and is not in the batch, at the page's former status.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Page_Stamp_Keeps_Inherited_Entry_Status()
    {
        using var temp = Cli.Temp("review");
        var page = Path.Combine(temp.Path, "loan.md");
        File.WriteAllText(page, """
            ---
            {
              "type": "aggregate",
              "id": "aggregate/lending.loan",
              "title": "Loan",
              "owner": "lending-team",
              "status": "draft",
              "decider": "lending-team",
              "decideBy": "2027-01-31",
              "records": [
                {
                  "type": "rule",
                  "id": "rule/loan.one-active-loan-per-copy",
                  "title": "One active loan per copy"
                }
              ]
            }
            ---
            # Loan
            """);
        var record = LiteSpec.Tests.Model.Checks.TestModel.Record("aggregate", "aggregate/lending.loan", page, """{"status": "draft"}""");

        LiteSpec.Model.Approver.StampBatch([new LiteSpec.Model.Approver.BatchStamp(record, "reviewed", "lending-team", "2026-11-20")]);

        var (front, _) = ReviewPages.Read(page);
        var rule = front["records"]!.AsArray()[0]!.AsObject();

        Assert.Equal("reviewed", front["status"]!.GetValue<string>());
        Assert.Equal("draft", rule["status"]!.GetValue<string>());
        Assert.Equal("lending-team", rule["decider"]!.GetValue<string>());
        Assert.Equal("2027-01-31", rule["decideBy"]!.GetValue<string>());
    }

    /// <summary>A transcribed record that serves a draft journey stays in draft, so its batch is refused.</summary>
    /// <proves>litespec/core/model#hold-transcribed-records-in-draft</proves>
    [Fact]
    public void Approve_Transcribed_Use_Case_Behind_Draft_Journey_Is_Refused()
    {
        using var fixture = ModelFixture.Prepare("large");

        ReviewPages.DraftPage(BorrowCopy(fixture.Path));
        ReviewPages.PatchPage(BorrowCopy(fixture.Path), front => front["provenance"] = "transcribed");
        ReviewPages.DraftPage(ReviewPages.Page(fixture.Path, "docs", "product", "journeys", "borrow-and-return.md"));

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "lending-team", "--on", "2026-11-20", "--dry-run");

        Assert.Contains("stays transcribed behind draft journey/borrow-and-return", result.Output, StringComparison.Ordinal);
    }
}
