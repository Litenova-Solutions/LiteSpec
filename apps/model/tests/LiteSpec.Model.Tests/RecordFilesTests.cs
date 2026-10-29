using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the page reader yields the page record and its entries.</summary>
public sealed class RecordFilesTests
{
    /// <summary>A page with two entries yields three records with their lines.</summary>
    /// <proves>litespec/core/model#state-the-common-fields-on-every-record</proves>
    [Fact]
    public void Reads_Page_With_Two_Entries()
    {
        var records = RecordFiles.Read("docs/borrow-copy.md", Page);

        Assert.Equal(3, records.Count);

        var page = records[0];
        Assert.Equal("use-case", page.Type);
        Assert.Equal("use-case/lending.borrow-copy", page.Id);
        Assert.Equal("Borrow copy", page.Title);
        Assert.Equal("lending-team", page.Owner);
        Assert.Equal("reviewed", page.Status);
        Assert.Equal("docs/borrow-copy.md", page.File);
        Assert.Equal(3, page.Line);
        Assert.False(page.IsEntry);
        Assert.Null(page.PageId);
        Assert.Empty(page.Ids("records"));

        var borrowed = records[1];
        Assert.Equal("result", borrowed.Type);
        Assert.Equal("result/borrow-copy.borrowed", borrowed.Id);
        Assert.Equal("success", borrowed.Field("outcome"));
        Assert.Equal(11, borrowed.Line);
        Assert.True(borrowed.IsEntry);
        Assert.Equal("use-case/lending.borrow-copy", borrowed.PageId);

        var unavailable = records[2];
        Assert.Equal("result/borrow-copy.copy-unavailable", unavailable.Id);
        Assert.Equal("draft", unavailable.Status);
        Assert.Equal(17, unavailable.Line);
        Assert.True(unavailable.IsEntry);
        Assert.Equal("use-case/lending.borrow-copy", unavailable.PageId);
    }

    /// <summary>An entry without its own owner or status takes the page's.</summary>
    /// <proves>litespec/core/model#keep-one-owner-and-status-on-every-record</proves>
    [Fact]
    public void Inherits_Status_From_Page()
    {
        var records = RecordFiles.Read("docs/borrow-copy.md", Page);

        var borrowed = records[1];
        Assert.Equal("reviewed", borrowed.Status);
        Assert.Equal("lending-team", borrowed.Owner);
        Assert.Null(borrowed.Field("status"));
    }

    /// <summary>A page with no frontmatter yields nothing.</summary>
    /// <proves>litespec/core/model#state-the-common-fields-on-every-record</proves>
    [Fact]
    public void Page_Without_Frontmatter_Yields_Nothing()
    {
        Assert.Empty(RecordFiles.Read("docs/prose.md", "# Just prose\n\nNo fence here.\n"));
    }

    /// <summary>An unclosed fence, malformed JSON and a typeless object yield nothing.</summary>
    /// <proves>litespec/core/model#state-the-common-fields-on-every-record</proves>
    [Fact]
    public void Malformed_Frontmatter_Yields_Nothing()
    {
        Assert.Empty(RecordFiles.Read("docs/open.md", "---\n{\"type\": \"use-case\"\n"));
        Assert.Empty(RecordFiles.Read("docs/broken.md", "---\n{oops}\n---\n"));
        Assert.Empty(RecordFiles.Read("docs/plain.md", "---\n{\"title\": \"No type\"}\n---\n"));
    }

    const string Page = """
        ---
        {
          "type": "use-case",
          "id": "use-case/lending.borrow-copy",
          "title": "Borrow copy",
          "owner": "lending-team",
          "status": "reviewed",
          "records": [
            {
              "type": "result",
              "id": "result/borrow-copy.borrowed",
              "title": "Borrowed",
              "outcome": "success"
            },
            {
              "type": "result",
              "id": "result/borrow-copy.copy-unavailable",
              "title": "Copy unavailable",
              "status": "draft"
            }
          ]
        }
        ---

        # Borrow copy
        """;
}
