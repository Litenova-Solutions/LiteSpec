using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Review;

/// <summary>Proves a module and type batch approves the records whose guards pass and holds the rest.</summary>
/// <follows>litespec/core/release#hold-only-the-blocked-records-of-a-type-batch</follows>
public sealed class PartialBatchTests
{
    static string LoanAggregate(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "loans", "README.md");

    static string Borrowing(string root) => ReviewPages.Page(
        root, "docs", "product", "lending", "borrowing.md");

    static string Manifest(string root) => ReviewPages.Page(
        root, "docs", "proof", "approvals", "2026-11-20-lending-rule.json");

    /// <summary>Adds one open question to the entry carrying the id.</summary>
    static void AddEntryQuestion(
        string path, string id, string question = "Who approves an extension past the loan period?") =>
        ReviewPages.PatchEntry(path, id, front => front["questions"] = new JsonArray(
            new JsonObject
            {
                ["question"] = question,
                ["decider"] = "lending-team",
                ["decideBy"] = "2027-06-01",
            }));

    static JsonObject ManifestJson(string root) =>
        (JsonObject)JsonNode.Parse(File.ReadAllText(Manifest(root)))!;

    static JsonObject ManifestRecord(JsonObject manifest, string id) =>
        manifest["records"]!.AsArray()
            .OfType<JsonObject>()
            .First(record => record["id"]!.GetValue<string>() == id);

    /// <summary>A record with an open question is held with its guard while its siblings are approved.</summary>
    /// <proves>litespec/core/release#hold-only-the-blocked-records-of-a-type-batch</proves>
    [Fact]
    public void Module_Type_Batch_Holds_Questioned_Record_And_Approves_Siblings()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftEntry(LoanAggregate(fixture.Path), "rule/loan.has-one-borrower");
        ReviewPages.DraftEntry(LoanAggregate(fixture.Path), "rule/loan.extended-before-due");
        AddEntryQuestion(LoanAggregate(fixture.Path), "rule/loan.extended-before-due");

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "lending/rule", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("1 record approved, 1 held.", result.Output);

        var (front, _) = ReviewPages.Read(LoanAggregate(fixture.Path));
        var entries = front["records"]!.AsArray().OfType<JsonObject>().ToList();
        var approved = entries.First(entry => entry["id"]!.GetValue<string>() == "rule/loan.has-one-borrower");
        var held = entries.First(entry => entry["id"]!.GetValue<string>() == "rule/loan.extended-before-due");

        Assert.Equal("reviewed", approved["status"]!.GetValue<string>());
        Assert.Equal("lending-team", approved["approvedBy"]!.GetValue<string>());
        Assert.Equal("2026-11-20", approved["approvedOn"]!.GetValue<string>());
        Assert.True(approved["approvedHash"]!.GetValue<string>().Length == 8);

        Assert.Equal("draft", held["status"]!.GetValue<string>());
        Assert.Null(held["approvedBy"]);
        Assert.Null(held["approvedOn"]);
        Assert.Null(held["approvedHash"]);

        var manifest = ManifestJson(fixture.Path);
        var records = manifest["records"]!.AsArray();

        Assert.Equal(2, records.Count);

        var approvedEntry = ManifestRecord(manifest, "rule/loan.has-one-borrower");
        var heldEntry = ManifestRecord(manifest, "rule/loan.extended-before-due");

        Assert.Equal("promoted", approvedEntry["action"]!.GetValue<string>());
        Assert.Null(approvedEntry["heldBecause"]);
        Assert.Equal("held", heldEntry["action"]!.GetValue<string>());
        Assert.Contains(
            "has open questions",
            heldEntry["heldBecause"]!.AsArray().Select(node => node!.GetValue<string>()));

        var sample = manifest["sample"]!.AsArray().Select(node => node!.GetValue<string>()).ToList();

        Assert.Single(sample);
        Assert.Equal("rule/loan.has-one-borrower", sample[0]);
    }

    /// <summary>A record resting on a draft parent is held while its siblings are approved.</summary>
    /// <proves>litespec/core/release#hold-only-the-blocked-records-of-a-type-batch</proves>
    [Fact]
    public void Module_Type_Batch_Holds_Record_Behind_Draft_Parent()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftEntry(LoanAggregate(fixture.Path), "rule/loan.has-one-borrower");
        ReviewPages.DraftEntry(LoanAggregate(fixture.Path), "rule/loan.extended-before-due");
        ReviewPages.DraftEntry(Borrowing(fixture.Path), "requirement/borrowing.max-active-loans");

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "lending/rule", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("1 record approved, 1 held.", result.Output);

        var (front, _) = ReviewPages.Read(LoanAggregate(fixture.Path));
        var entries = front["records"]!.AsArray().OfType<JsonObject>().ToList();
        var held = entries.First(entry => entry["id"]!.GetValue<string>() == "rule/loan.has-one-borrower");
        var approved = entries.First(entry => entry["id"]!.GetValue<string>() == "rule/loan.extended-before-due");

        Assert.Equal("reviewed", approved["status"]!.GetValue<string>());
        Assert.Equal("lending-team", approved["approvedBy"]!.GetValue<string>());

        Assert.Equal("draft", held["status"]!.GetValue<string>());
        Assert.Null(held["approvedBy"]);
        Assert.Null(held["approvedOn"]);
        Assert.Null(held["approvedHash"]);

        var manifest = ManifestJson(fixture.Path);
        var heldEntry = ManifestRecord(manifest, "rule/loan.has-one-borrower");

        Assert.Equal("held", heldEntry["action"]!.GetValue<string>());
        Assert.Contains(
            "stays draft behind draft requirement/borrowing.max-active-loans",
            heldEntry["heldBecause"]!.AsArray().Select(node => node!.GetValue<string>()));
        Assert.Equal(
            "promoted",
            ManifestRecord(manifest, "rule/loan.extended-before-due")["action"]!.GetValue<string>());
    }

    /// <summary>A dry run names each held record with its guard and writes nothing.</summary>
    /// <proves>litespec/core/release#hold-only-the-blocked-records-of-a-type-batch</proves>
    [Fact]
    public void Module_Type_Batch_Dry_Run_Names_Held_Guards()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftEntry(LoanAggregate(fixture.Path), "rule/loan.has-one-borrower");
        ReviewPages.DraftEntry(LoanAggregate(fixture.Path), "rule/loan.extended-before-due");
        AddEntryQuestion(LoanAggregate(fixture.Path), "rule/loan.extended-before-due");

        var before = File.ReadAllText(LoanAggregate(fixture.Path));

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "lending/rule", "--by", "lending-team", "--dry-run");

        Assert.Equal(0, result.Status);
        Assert.Contains("1 to approve, 1 held", result.Output);
        Assert.Contains(
            "rule/loan.extended-before-due would be held: has open questions", result.Output);
        Assert.Contains("Sample (1 of 1", result.Output);
        Assert.DoesNotContain("Refused", result.Output);
        Assert.Equal(before, File.ReadAllText(LoanAggregate(fixture.Path)));
        Assert.False(Directory.Exists(ReviewPages.Page(fixture.Path, "docs", "proof", "approvals")));
    }

    /// <summary>A module and type batch without a named owner approver is refused whole.</summary>
    /// <proves>litespec/core/release#refuse-a-batch-without-a-named-approver</proves>
    [Fact]
    public void Module_Type_Batch_Without_Named_Approver_Refuses_Whole()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftEntry(LoanAggregate(fixture.Path), "rule/loan.has-one-borrower");
        ReviewPages.DraftEntry(LoanAggregate(fixture.Path), "rule/loan.extended-before-due");

        var before = File.ReadAllText(LoanAggregate(fixture.Path));

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "lending/rule");

        Assert.Equal(1, result.Status);
        Assert.Contains("needs --by naming a project owner", result.Output);
        Assert.Equal(before, File.ReadAllText(LoanAggregate(fixture.Path)));
        Assert.False(Directory.Exists(ReviewPages.Page(fixture.Path, "docs", "proof", "approvals")));
    }

    /// <summary>A module and type batch no record passes is refused whole and writes nothing.</summary>
    /// <proves>litespec/core/release#hold-only-the-blocked-records-of-a-type-batch</proves>
    [Fact]
    public void Module_Type_Batch_With_No_Passing_Record_Refuses_Whole()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftEntry(LoanAggregate(fixture.Path), "rule/loan.has-one-borrower");
        AddEntryQuestion(LoanAggregate(fixture.Path), "rule/loan.has-one-borrower");

        var before = File.ReadAllText(LoanAggregate(fixture.Path));

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "lending/rule", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(1, result.Status);
        Assert.Contains("rule/loan.has-one-borrower: has open questions", result.Output);
        Assert.Contains("Refused: 1 guard failures.", result.Output);
        Assert.Equal(before, File.ReadAllText(LoanAggregate(fixture.Path)));
        Assert.False(Directory.Exists(ReviewPages.Page(fixture.Path, "docs", "proof", "approvals")));
    }
}
