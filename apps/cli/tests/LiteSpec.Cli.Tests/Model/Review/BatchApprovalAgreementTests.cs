using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Review;

/// <summary>Proves a batch stamps exactly the records its queue batch holds, with the hash the file carries.</summary>
/// <follows>litespec/core/release#approve-drafts-in-bounded-batches</follows>
public sealed class BatchApprovalAgreementTests
{
    static string Automation(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "automations", "send-due-notice-on-loan-borrowed.md");

    static string BorrowCopy(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md");

    static string LoanPeriod(string root) => ReviewPages.Page(
        root, "docs", "product", "decisions", "0001-loan-period.md");

    /// <summary>A use-case batch leaves a scenario on an automation page alone, and the automation batch stamps it.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Approve_Use_Case_Batch_Leaves_Automation_Page_Scenarios()
    {
        using var fixture = ModelFixture.Prepare("large");

        ReviewPages.AppendEntry(Automation(fixture.Path), new JsonObject
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
        ReviewPages.DraftPage(Automation(fixture.Path));
        ReviewPages.DraftPage(BorrowCopy(fixture.Path));

        var queue = Cli.Run("model", "review", "--root", fixture.Path, "--format", "json");

        Assert.Equal(0, queue.Status);

        var batches = ReviewPages.OutputJson(queue)["batches"]!.AsArray();
        var holding = batches
            .Where(batch => batch!["records"]!.AsArray()
                .Any(record => record!["id"]!.GetValue<string>() == "scenario/borrow-copy.due-notice-sent-on-borrow"))
            .Select(batch => batch!["selector"]!.GetValue<string>())
            .ToList();

        Assert.Single(holding);
        Assert.Equal("automation/lending.send-due-notice-on-loan-borrowed", holding[0]);

        var useCase = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "use-case/lending.borrow-copy", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, useCase.Status);
        Assert.Contains("3 records approved, 1 held.", useCase.Output);

        var manifest = (JsonObject)JsonNode.Parse(File.ReadAllText(ReviewPages.Page(
            fixture.Path, "docs", "proof", "approvals", "2026-11-20-use-case-lending-borrow-copy.json")))!;
        var ids = manifest["records"]!.AsArray()
            .Select(record => record!["id"]!.GetValue<string>())
            .ToList();

        Assert.DoesNotContain("scenario/borrow-copy.due-notice-sent-on-borrow", ids);

        var (page, _) = ReviewPages.Read(Automation(fixture.Path));
        var scenario = page["records"]!.AsArray()
            .OfType<JsonObject>()
            .First(entry => entry["id"]!.GetValue<string>() == "scenario/borrow-copy.due-notice-sent-on-borrow");

        Assert.Null(scenario["approvedBy"]);
        Assert.Null(scenario["approvedHash"]);

        var automationResult = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "automation/lending.send-due-notice-on-loan-borrowed", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, automationResult.Status);
        Assert.Contains("2 records approved, 0 held.", automationResult.Output);
    }

    /// <summary>Approving a decision writes one hash to the manifest and the file, and no decidedOn.</summary>
    /// <proves>litespec/core/release#approve-drafts-in-bounded-batches</proves>
    [Fact]
    public void Approve_Decision_Batch_Manifest_Hash_Matches_File()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftPage(LoanPeriod(fixture.Path));

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "decision/decision/0001-loan-period", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);

        var (front, _) = ReviewPages.Read(LoanPeriod(fixture.Path));

        Assert.False(front.ContainsKey("decidedOn"));
        Assert.Equal(
            ReviewPages.SemanticHash("decision", "decision/0001-loan-period", front),
            front["approvedHash"]!.GetValue<string>());

        var manifest = (JsonObject)JsonNode.Parse(File.ReadAllText(ReviewPages.Page(
            fixture.Path, "docs", "proof", "approvals", "2026-11-20-decision-decision-0001-loan-period.json")))!;
        var decision = manifest["records"]!.AsArray()
            .OfType<JsonObject>()
            .First(record => record["id"]!.GetValue<string>() == "decision/0001-loan-period");

        Assert.Equal("promoted", decision["action"]!.GetValue<string>());
        Assert.Equal(front["approvedHash"]!.GetValue<string>(), decision["hash"]!.GetValue<string>());
    }
}
