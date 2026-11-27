using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the page reader yields the page record and its entries.</summary>
public sealed class RecordFilesTests
{
    /// <summary>A page with typed child arrays yields three records with their lines.</summary>
    /// <proves>litespec/core/model#state-the-common-fields-on-every-record</proves>
    [Fact]
    public void ReadsPageWithTypedChildArrays()
    {
        var records = RecordFiles.Read("docs/borrow-copy.yaml", Page);

        Assert.Equal(3, records.Count);

        var page = records[0];
        Assert.Equal("use-case", page.Type);
        Assert.Equal("use-case/lending.borrow-copy", page.Id);
        Assert.Equal("Borrow copy", page.Title);
        Assert.Equal("docs/borrow-copy.yaml", page.File);
        Assert.Equal(1, page.Line);
        Assert.False(page.IsEntry);
        Assert.Null(page.PageId);
        Assert.Null(page.Field("records"));

        var borrowed = records[1];
        Assert.Equal("result", borrowed.Type);
        Assert.Equal("result/borrow-copy.borrowed", borrowed.Id);
        Assert.Equal("success", borrowed.Field("outcome"));
        Assert.Equal(5, borrowed.Line);
        Assert.True(borrowed.IsEntry);
        Assert.Equal("use-case/lending.borrow-copy", borrowed.PageId);

        var unavailable = records[2];
        Assert.Equal("scenario", unavailable.Type);
        Assert.Equal("scenario/borrow-copy.copy-unavailable", unavailable.Id);
        Assert.Equal(10, unavailable.Line);
        Assert.True(unavailable.IsEntry);
        Assert.Equal("use-case/lending.borrow-copy", unavailable.PageId);
    }

    /// <summary>An entry stating the wrong type reports record-invalid.</summary>
    /// <proves>litespec/core/model#state-the-common-fields-on-every-record</proves>
    [Fact]
    public void WrongTypedEntryReportsRecordInvalid()
    {
        var scanned = RecordFiles.Scan("docs/borrow-copy.yaml", WrongTypedPage);

        Assert.Single(scanned.Records);
        var problem = Assert.Single(scanned.Problems);
        Assert.Equal("record-invalid", problem.Code);
    }

    /// <summary>A permission set yields its permission entries with their lines.</summary>
    /// <proves>litespec/core/model#state-the-common-fields-on-every-record</proves>
    [Fact]
    public void ReadsPermissionSetEntries()
    {
        var records = RecordFiles.Read("docs/spending-permissions.yaml", PermissionSetPage);

        var permission = Assert.Single(records);
        Assert.Equal("permission", permission.Type);
        Assert.Equal("permission/spending.approve", permission.Id);
        Assert.True(permission.IsEntry);
    }

    /// <summary>Prose that is no YAML mapping yields nothing.</summary>
    /// <proves>litespec/core/model#state-the-common-fields-on-every-record</proves>
    [Fact]
    public void ProseYieldsNothing()
    {
        Assert.Empty(RecordFiles.Read("docs/prose.yaml", "# Just prose\n\nNo mapping here.\n"));
    }

    /// <summary>An unclosed scalar, a flow collection and a typeless mapping yield nothing.</summary>
    /// <proves>litespec/core/model#state-the-common-fields-on-every-record</proves>
    [Fact]
    public void MalformedYamlYieldsNothing()
    {
        Assert.Empty(RecordFiles.Read("docs/open.yaml", "type: \"use-case\n"));
        Assert.Empty(RecordFiles.Read("docs/broken.yaml", "type: [use-case, rule]\n"));
        Assert.Empty(RecordFiles.Read("docs/plain.yaml", "title: No type\n"));
    }

    const string Page = """
        type: use-case
        id: use-case/lending.borrow-copy
        title: Borrow copy
        results:
          - type: result
            id: result/borrow-copy.borrowed
            title: Borrowed
            outcome: success
        scenarios:
          - id: scenario/borrow-copy.copy-unavailable
            title: Copy unavailable
        """;

    const string WrongTypedPage = """
        type: use-case
        id: use-case/lending.borrow-copy
        title: Borrow copy
        results:
          - type: scenario
            id: scenario/borrow-copy.borrowed
            title: Borrowed
        """;

    const string PermissionSetPage = """
        type: permission-set
        id: permission/spending.all
        title: Spending permissions
        permissions:
          - id: permission/spending.approve
            title: Approve
        """;
}
