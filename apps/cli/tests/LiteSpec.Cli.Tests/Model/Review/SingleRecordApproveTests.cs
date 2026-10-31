using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Review;

/// <summary>Proves <c>litespec model approve</c> of one record promotes its status, and re-approval writes the new approver and date.</summary>
/// <follows>litespec/core/model#record-approval-as-data</follows>
public sealed class SingleRecordApproveTests
{
    static string LoanAggregate(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "loans", "README.md");

    static string Borrowing(string root) => ReviewPages.Page(
        root, "docs", "product", "lending", "borrowing.md");

    static JsonObject Entry(string path, string id) =>
        ReviewPages.Read(path).Front["records"]!.AsArray()
            .OfType<JsonObject>()
            .First(entry => entry["id"]!.GetValue<string>() == id);

    /// <summary>Approving one draft Intent record promotes it to approved with the approver, date and hash.</summary>
    /// <proves>litespec/core/model#record-approval-as-data</proves>
    [Fact]
    public void Approve_Single_Draft_Intent_Record_Promotes_To_Approved()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftEntry(Borrowing(fixture.Path), "requirement/borrowing.max-active-loans");

        var before = Entry(Borrowing(fixture.Path), "requirement/borrowing.loan-period-days")["approvedHash"]!.GetValue<string>();

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "requirement/borrowing.max-active-loans", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("1 records approved.", result.Output);

        var approved = Entry(Borrowing(fixture.Path), "requirement/borrowing.max-active-loans");

        Assert.Equal("approved", approved["status"]!.GetValue<string>());
        Assert.Equal("lending-team", approved["approvedBy"]!.GetValue<string>());
        Assert.Equal("2026-11-20", approved["approvedOn"]!.GetValue<string>());
        Assert.True(approved["approvedHash"]!.GetValue<string>().Length == 8);

        var sibling = Entry(Borrowing(fixture.Path), "requirement/borrowing.loan-period-days");

        Assert.Equal(before, sibling["approvedHash"]!.GetValue<string>());
    }

    /// <summary>Approving one draft Behavior record promotes it to reviewed with the approver, date and hash.</summary>
    /// <proves>litespec/core/model#record-approval-as-data</proves>
    [Fact]
    public void Approve_Single_Draft_Behavior_Record_Promotes_To_Reviewed()
    {
        using var fixture = ModelFixture.Prepare("small");

        ReviewPages.DraftEntry(LoanAggregate(fixture.Path), "rule/loan.has-one-borrower");

        var before = Entry(LoanAggregate(fixture.Path), "rule/loan.extended-before-due")["approvedHash"]!.GetValue<string>();

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "rule/loan.has-one-borrower", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("1 records approved.", result.Output);

        var approved = Entry(LoanAggregate(fixture.Path), "rule/loan.has-one-borrower");

        Assert.Equal("reviewed", approved["status"]!.GetValue<string>());
        Assert.Equal("lending-team", approved["approvedBy"]!.GetValue<string>());
        Assert.Equal("2026-11-20", approved["approvedOn"]!.GetValue<string>());
        Assert.True(approved["approvedHash"]!.GetValue<string>().Length == 8);

        var sibling = Entry(LoanAggregate(fixture.Path), "rule/loan.extended-before-due");

        Assert.Equal(before, sibling["approvedHash"]!.GetValue<string>());
    }

    /// <summary>Re-approving a changed record writes the new approver, date and hash beside its kept status.</summary>
    /// <proves>litespec/core/model#record-approval-as-data</proves>
    [Fact]
    public void Approve_Single_Changed_Record_Writes_New_Approver_And_Date()
    {
        using var fixture = ModelFixture.Prepare("small");

        var stale = Entry(Borrowing(fixture.Path), "requirement/borrowing.max-active-loans")["approvedHash"]!.GetValue<string>();

        ReviewPages.PatchEntry(
            Borrowing(fixture.Path),
            "requirement/borrowing.max-active-loans",
            front => front["statement"] = "A reader holds at most six active loans.");

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "requirement/borrowing.max-active-loans", "--by", "catalog-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);
        Assert.Contains("1 records approved.", result.Output);

        var approved = Entry(Borrowing(fixture.Path), "requirement/borrowing.max-active-loans");

        Assert.Equal("approved", approved["status"]!.GetValue<string>());
        Assert.Equal("catalog-team", approved["approvedBy"]!.GetValue<string>());
        Assert.Equal("2026-11-20", approved["approvedOn"]!.GetValue<string>());
        Assert.True(approved["approvedHash"]!.GetValue<string>().Length == 8);
        Assert.NotEqual(stale, approved["approvedHash"]!.GetValue<string>());
    }
}
