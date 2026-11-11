using System.Text.Json.Nodes;
using LiteSpec.Cli.Book;
using LiteSpec.Cli.Book.Details;
using LiteSpec.Cli.Book.Outline;
using LiteSpec.Cli.Book.Output;

namespace LiteSpec.Cli.Tests.Book;

/// <summary>Proves the book renders record links only for allowed schemes and orders records ordinally.</summary>
public sealed class BookLinkTests
{
    /// <summary>A Markdown link to a data URL renders as plain text with no href.</summary>
    /// <proves>litespec/core/model#point-every-book-entry-to-its-source</proves>
    [Fact]
    public void MarkdownDataLinkRendersAsPlainText()
    {
        var html = BookMarkdown.Render("[click](data:text/html,hello)");

        Assert.Contains("click", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data:text", html, StringComparison.Ordinal);
    }

    /// <summary>A Markdown link to a javascript URL renders as plain text with no href.</summary>
    /// <proves>litespec/core/model#point-every-book-entry-to-its-source</proves>
    [Fact]
    public void MarkdownJavascriptLinkRendersAsPlainText()
    {
        var html = BookMarkdown.Render("[click](javascript:alert)");

        Assert.Contains("click", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href", html, StringComparison.Ordinal);
        Assert.DoesNotContain("javascript:", html, StringComparison.Ordinal);
    }

    /// <summary>Allowed targets stay links: http, https, mailto, fragments, relative paths and the editor scheme.</summary>
    /// <proves>litespec/core/model#point-every-book-entry-to-its-source</proves>
    [Fact]
    public void SafeHrefAllowsTheListedTargets()
    {
        Assert.True(BookRender.IsSafeHref("https://example.com/page"));
        Assert.True(BookRender.IsSafeHref("http://example.com/page"));
        Assert.True(BookRender.IsSafeHref("mailto:team@example.com"));
        Assert.True(BookRender.IsSafeHref("#section"));
        Assert.True(BookRender.IsSafeHref(".litespec/page.md"));
        Assert.True(BookRender.IsSafeHref("../.litespec/page.md"));
        Assert.True(BookRender.IsSafeHref("vscode://file/root/page.cs:1", "vscode"));
    }

    /// <summary>Every other scheme and network path renders as plain text.</summary>
    /// <proves>litespec/core/model#point-every-book-entry-to-its-source</proves>
    [Fact]
    public void SafeHrefRejectsOtherTargets()
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

    /// <summary>A record explanation with a data Markdown link renders into the book detail with no data href.</summary>
    /// <proves>litespec/core/model#point-every-book-entry-to-its-source</proves>
    [Fact]
    public void BookDetailHoldsNoDataHref()
    {
        var model = JsonNode.Parse(
            """
            {"records":[
              {"type":"research","id":"research/links","title":"Links","owner":"Ada","status":"draft","effectiveStatus":"draft","decider":null,"decideBy":null,"file":".litespec/test.yaml","line":3,"fields":{"question":"Which links?","sources":[{"title":"Trap","url":"javascript:alert(1)"}]},"links":{},"explanation":"# Links\n\nSee [click](data:text/html,hello) and [site](https://example.com)."}
            ]}
            """)!.AsObject();

        var details = BookRecordDetails.Build(model, BookOutline.Build(model));
        var html = Str(details["research/links"], "bodyHtml") ?? "";

        Assert.Contains("click", html, StringComparison.Ordinal);
        Assert.Contains("href=\"https://example.com\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"data:", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"javascript:", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data:text", html, StringComparison.Ordinal);
    }

    static string? Str(JsonObject? obj, string name) =>
        obj?[name] is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? text : null;

    /// <summary>Records order by ordinal title, then id, so the order does not depend on the machine culture.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void RecordsOrderByOrdinalTitleThenId()
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
