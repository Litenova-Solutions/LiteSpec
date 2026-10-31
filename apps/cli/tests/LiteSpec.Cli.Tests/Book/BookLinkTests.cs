using System.Text.Json.Nodes;
using LiteSpec.Book;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Book;

/// <summary>Proves the book renders record links only for allowed schemes and orders records ordinally.</summary>
public sealed class BookLinkTests
{
    /// <summary>A Markdown link to a data URL renders as plain text with no href.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Markdown_Data_Link_Renders_As_Plain_Text()
    {
        var html = BookMarkdown.Render("[click](data:text/html,hello)");

        Assert.Contains("click", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data:text", html, StringComparison.Ordinal);
    }

    /// <summary>A Markdown link to a javascript URL renders as plain text with no href.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Markdown_Javascript_Link_Renders_As_Plain_Text()
    {
        var html = BookMarkdown.Render("[click](javascript:alert)");

        Assert.Contains("click", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href", html, StringComparison.Ordinal);
        Assert.DoesNotContain("javascript:", html, StringComparison.Ordinal);
    }

    /// <summary>Allowed targets stay links: http, https, mailto, fragments, relative paths and the editor scheme.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Safe_Href_Allows_The_Listed_Targets()
    {
        Assert.True(BookRender.IsSafeHref("https://example.com/page"));
        Assert.True(BookRender.IsSafeHref("http://example.com/page"));
        Assert.True(BookRender.IsSafeHref("mailto:team@example.com"));
        Assert.True(BookRender.IsSafeHref("#section"));
        Assert.True(BookRender.IsSafeHref("docs/page.md"));
        Assert.True(BookRender.IsSafeHref("../docs/page.md"));
        Assert.True(BookRender.IsSafeHref("vscode://file/root/page.cs:1", "vscode"));
    }

    /// <summary>Every other scheme and network path renders as plain text.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Safe_Href_Rejects_Other_Targets()
    {
        Assert.False(BookRender.IsSafeHref("javascript:alert(1)"));
        Assert.False(BookRender.IsSafeHref("JaVaScRiPt:alert"));
        Assert.False(BookRender.IsSafeHref("data:text/html,hello"));
        Assert.False(BookRender.IsSafeHref("vbscript:msgbox"));
        Assert.False(BookRender.IsSafeHref("//example.com/page"));
        Assert.False(BookRender.IsSafeHref("vscode://file/root/page.cs:1"));
        Assert.False(BookRender.IsSafeHref(""));
        Assert.False(BookRender.IsSafeHref(null));
    }

    /// <summary>A record body with a data Markdown link renders into the book detail with no data href.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Book_Detail_Holds_No_Data_Href()
    {
        var model = JsonNode.Parse(
            """
            {"records":[
              {"type":"research","id":"research/links","title":"Links","owner":"Ada","status":"draft","effectiveStatus":"draft","decider":null,"decideBy":null,"file":"docs/test.md","line":3,"fields":{"question":"Which links?","sources":[{"title":"Trap","url":"javascript:alert(1)"}]},"links":{},"body":"See [click](data:text/html,hello) and [site](https://example.com)."}
            ]}
            """)!.AsObject();

        var details = BookEnrich.Build(model, BookOutline.Build(model));
        var html = Str(details["research/links"], "bodyHtml") ?? "";

        Assert.Contains("click", html, StringComparison.Ordinal);
        Assert.Contains("href=\"https://example.com\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"data:", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"javascript:", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data:text", html, StringComparison.Ordinal);
    }

    /// <summary>The client renders the research URL and the dataschema only through the safe-href rule.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Client_Links_Research_And_Dataschema_Through_SafeHref()
    {
        using var temp = Cli.Temp("book-links");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Path.Combine(temp.Path, "out"));
        Assert.Equal(0, result.Status);

        var script = File.ReadAllText(Path.Combine(temp.Path, "out", "assets", "book.js"));
        Assert.Contains("function safeHref", script, StringComparison.Ordinal);
        Assert.Contains("safeHref(x.url)", script, StringComparison.Ordinal);
        Assert.Contains("linkOrText(f.dataschema)", script, StringComparison.Ordinal);
        Assert.DoesNotContain("esc(x.url)", script, StringComparison.Ordinal);
        Assert.DoesNotContain("esc(f.dataschema)", script, StringComparison.Ordinal);
    }

    static string? Str(JsonObject? obj, string name) =>
        obj?[name] is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? text : null;

    /// <summary>Records order by ordinal title, then id, so the order does not depend on the machine culture.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Records_Order_By_Ordinal_Title_Then_Id()
    {
        var model = JsonNode.Parse(
            """
            {"records":[
              {"type":"research","id":"research/b","title":"apple"},
              {"type":"research","id":"research/a","title":"Zebra"},
              {"type":"research","id":"research/c","title":"apple"}
            ]}
            """)!.AsObject();

        var index = BookModel.BuildIndex(model);
        var ordered = index.ByType["research"]
            .Select(record => BookRender.StrProp(record, "id") ?? "")
            .ToList();

        // Ordinal: "Zebra" (Z) sorts before "apple" (a); equal titles fall back to the id.
        Assert.Equal(["research/a", "research/b", "research/c"], ordered);
    }
}
