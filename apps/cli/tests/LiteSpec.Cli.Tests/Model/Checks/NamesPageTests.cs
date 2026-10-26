using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the book names page renders one row per module with its names and the lookalike terms.</summary>
public sealed class NamesPageTests
{
    [Fact]
    public void Names_Page_Renders_A_Row_Per_Module()
    {
        using var temp = Cli.Temp("names-page");
        var model = Path.Combine(temp.Path, "model.json");
        File.WriteAllText(model, NamesModel());

        var result = Cli.Run("book", "--model", model, "--out", temp.Path);

        Assert.Equal(0, result.Status);
        var html = File.ReadAllText(Path.Combine(temp.Path, "names", "index.html"));

        Assert.Contains("Module A", html, StringComparison.Ordinal);
        Assert.Contains("Module B", html, StringComparison.Ordinal);
        Assert.Equal(2, Count(html, "<tr><td>"));
        Assert.Contains("borrow (1)", html, StringComparison.Ordinal);
        Assert.Contains("renew (1)", html, StringComparison.Ordinal);
        Assert.Contains("lend (1)", html, StringComparison.Ordinal);
        Assert.Contains("book (2)", html, StringComparison.Ordinal);
        Assert.Contains("event/book.borrowed", html, StringComparison.Ordinal);
        Assert.Contains("a.copy-missing", html, StringComparison.Ordinal);
        Assert.Contains("book-due-member", html, StringComparison.Ordinal);
        Assert.Contains("max-books", html, StringComparison.Ordinal);
        Assert.Contains(">Lookalikes<", html, StringComparison.Ordinal);
        Assert.Contains("No lookalike terms are reported.", html, StringComparison.Ordinal);
    }

    static int Count(string text, string fragment) =>
        text.Split(fragment, StringSplitOptions.None).Length - 1;

    static string NamesModel() =>
        """
        {
          "records": [
            {"type": "module", "id": "module/a", "title": "Module A", "owner": "team", "status": "approved", "file": "docs/a.md", "line": 1, "fields": {}, "links": {}},
            {"type": "module", "id": "module/b", "title": "Module B", "owner": "team", "status": "approved", "file": "docs/b.md", "line": 1, "fields": {}, "links": {}},
            {"type": "use-case", "id": "use-case/a.borrow-book", "title": "Borrow Book", "owner": "team", "status": "approved", "file": "docs/a.md", "line": 1, "fields": {"kind": "command"}, "links": {}},
            {"type": "use-case", "id": "use-case/a.renew-book", "title": "Renew Book", "owner": "team", "status": "approved", "file": "docs/a.md", "line": 1, "fields": {"kind": "command"}, "links": {}},
            {"type": "use-case", "id": "use-case/b.lend-book", "title": "Lend Book", "owner": "team", "status": "approved", "file": "docs/b.md", "line": 1, "fields": {"kind": "command"}, "links": {}},
            {"type": "aggregate", "id": "aggregate/a.book", "title": "Book", "owner": "team", "status": "approved", "file": "docs/a.md", "line": 1, "fields": {}, "links": {}},
            {"type": "event", "id": "event/book.borrowed", "title": "Borrowed", "owner": "team", "status": "approved", "file": "docs/a.md", "line": 1, "fields": {"meaning": "A book was borrowed."}, "links": {}},
            {"type": "reaction", "id": "reaction/a.send-notice-on-book-borrowed", "title": "Send Notice", "owner": "team", "status": "approved", "file": "docs/a.md", "line": 1, "fields": {"on": ["event/book.borrowed"]}, "links": {}},
            {"type": "result", "id": "result/borrow-book.copy-missing", "title": "Copy Missing", "owner": "team", "status": "approved", "file": "docs/a.md", "line": 1, "fields": {"outcome": "refusal", "code": "a.copy-missing", "meaning": "The copy is missing."}, "links": {}},
            {"type": "notification", "id": "notification/a.book-due", "title": "Book Due", "owner": "team", "status": "approved", "file": "docs/a.md", "line": 1, "fields": {"template": "book-due-member"}, "links": {}},
            {"type": "setting", "id": "setting/a.max-books", "title": "Max Books", "owner": "team", "status": "approved", "file": "docs/a.md", "line": 1, "fields": {}, "links": {}}
          ]
        }
        """;
}
