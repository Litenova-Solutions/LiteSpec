using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Book;

/// <summary>Proves <c>litespec book</c> writes one offline shell with sharded data from a fixture consumer.</summary>
public sealed class BookCommandTests
{
    /// <summary>The book writes <c>index.html</c>, the four assets, and the manifest, lexicon, outline, record, search and security shards.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void BookWritesTheShardedBook()
    {
        using var consumer = ModelFixture.Prepare("small");
        using var temp = TestCli.Temp("book");
        var result = TestCli.Run(
            "book",
            "--root", consumer.Path,
            "--out", temp.Path);
        Assert.Equal(0, result.Status);

        Assert.True(File.Exists(Path.Combine(temp.Path, "index.html")), "index.html was not written.");
        foreach (var asset in new[] { "book.css", "book.js" })
        {
            Assert.True(File.Exists(Path.Combine(temp.Path, "assets", asset)), $"assets/{asset} was not written.");
        }

        var data = Path.Combine(temp.Path, "data");
        foreach (var shard in new[] { "manifest.js", "lexicon.js", "security.js" })
        {
            Assert.True(File.Exists(Path.Combine(data, shard)), $"data/{shard} was not written.");
        }

        Assert.False(File.Exists(Path.Combine(data, "queue.js")), "The review queue shard still ships.");

        Assert.True(File.Exists(Path.Combine(data, "outline-intent-0.js")), "No intent outline shard was written.");
        Assert.NotEmpty(Directory.GetFiles(data, "records-*.js"));
        Assert.NotEmpty(Directory.GetFiles(data, "search-*.js"));

        // The old pipeline's outputs are gone: no Mermaid, no search.json, no per-record pages.
        Assert.False(File.Exists(Path.Combine(temp.Path, "assets", "mermaid.min.js")), "Mermaid still ships.");
        Assert.False(File.Exists(Path.Combine(temp.Path, "assets", "mermaid.version")), "The Mermaid version still ships.");
        Assert.False(File.Exists(Path.Combine(temp.Path, "search.json")), "search.json still ships.");
        Assert.False(
            File.Exists(Path.Combine(temp.Path, "aggregate", "lending", "loan.html")),
            "Per-record pages still ship.");

        var shell = File.ReadAllText(Path.Combine(temp.Path, "index.html"));
        Assert.Contains("<script src=\"assets/book.js\"></script>", shell, StringComparison.Ordinal);
        Assert.Contains("<link rel=\"stylesheet\" href=\"assets/book.css\">", shell, StringComparison.Ordinal);
        Assert.Contains("<div id=\"app\"></div>", shell, StringComparison.Ordinal);
        Assert.Contains("<noscript>", shell, StringComparison.Ordinal);
        Assert.Contains("Library", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("{{title}}", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("{{noscript}}", shell, StringComparison.Ordinal);
    }

    /// <summary>The manifest carries the project, roots, editor template, totals, layers, shard map, search shards, unplaced ids and generation date, and no approvals.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void ManifestCarriesTheBookContract()
    {
        using var consumer = ModelFixture.Prepare("small");
        using var temp = TestCli.Temp("book");
        var result = TestCli.Run(
            "book",
            "--root", consumer.Path,
            "--out", temp.Path);
        Assert.Equal(0, result.Status);

        var manifest = ReadShard(Path.Combine(temp.Path, "data", "manifest.js"), "manifest");
        var project = manifest["project"]!.AsObject();
        Assert.Equal("product/library", project["id"]!.GetValue<string>());
        Assert.Equal("Library", project["title"]!.GetValue<string>());

        var roots = manifest["roots"]!.AsObject();
        Assert.True(roots["consumer"]!.GetValue<string>().Length > 0);
        Assert.True(roots["litespec"]!.GetValue<string>().Length > 0);

        Assert.Equal("vscode://file/{root}/{path}:{line}", manifest["editor"]!.GetValue<string>());

        var totals = manifest["totals"]!.AsObject();
        var total = totals.Sum(pair => pair.Value!.GetValue<int>());
        Assert.True(total > 0, "The book holds no records.");
        Assert.Equal(1, totals["product"]!.GetValue<int>());
        Assert.Equal(1, totals["module"]!.GetValue<int>());
        Assert.Equal(8, totals["use-case"]!.GetValue<int>());

        var layers = manifest["layers"]!.AsArray();
        Assert.Equal(
            new[] { "intent", "behavior", "implementation", "proof", "governance", "glossary" },
            layers.Select(node => node!.AsObject()["key"]!.GetValue<string>()).ToList());
        foreach (var layer in layers)
        {
            Assert.NotNull(layer!.AsObject()["label"]);
            Assert.NotNull(layer.AsObject()["outline"]);
        }

        var shardOf = manifest["shardOf"]!.AsObject();
        Assert.Equal(total, shardOf.Count);
        foreach (var pair in shardOf)
        {
            Assert.True(
                File.Exists(Path.Combine(temp.Path, "data", pair.Value!.GetValue<string>() + ".js")),
                $"Shard {pair.Value} is missing.");
        }

        var search = manifest["search"]!.AsArray();
        Assert.NotEmpty(search);
        foreach (var key in search)
        {
            Assert.True(
                File.Exists(Path.Combine(temp.Path, "data", key!.GetValue<string>() + ".js")),
                $"Search shard {key} is missing.");
        }

        // Every unplaced record is counted in the manifest and named on the console line.
        var unplaced = manifest["unplaced"]!.AsArray();
        foreach (var id in unplaced)
        {
            Assert.Contains(id!.GetValue<string>(), result.Output, StringComparison.Ordinal);
        }

        Assert.True(manifest["generatedOn"]!.GetValue<string>().Length > 0);
        Assert.Contains($"{total} records", result.Output, StringComparison.Ordinal);

        Assert.Null(manifest["approvals"]);
    }

    /// <summary>The console line names each unplaced identifier with the failed record-parent-missing rule.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void UnplacedConsoleLineNamesTheFailedRule()
    {
        using var temp = TestCli.Temp("book");
        var model = (JsonObject)JsonNode.Parse(
            """
            {
              "records": [
                {"type": "product", "id": "product/library", "title": "Library"},
                {"type": "requirement", "id": "requirement/nowhere.lost", "title": "Lost", "links": {}, "fields": {}}
              ]
            }
            """)!;
        var line = BookTestBooks.Write(model, temp.Path, Path.Combine(temp.Path, "out"));

        Assert.Contains("record-parent-missing", line, StringComparison.Ordinal);
        Assert.Contains("requirement/nowhere.lost", line, StringComparison.Ordinal);
    }

    /// <summary>The security shard carries the security items: cross-tenant access, outside calls and personal or sensitive data.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void SecurityShardCarriesSecurityItems()
    {
        using var consumer = ModelFixture.Prepare("small");
        using var temp = TestCli.Temp("book");
        var result = TestCli.Run(
            "book",
            "--root", consumer.Path,
            "--out", temp.Path);
        Assert.Equal(0, result.Status);

        var security = ReadShardArray(Path.Combine(temp.Path, "data", "security.js"), "security");

        foreach (var item in security.OfType<JsonObject>())
        {
            Assert.Contains(item["kind"]!.GetValue<string>(), new[] { "cross", "calls", "data" });
            Assert.True(item["id"]!.GetValue<string>().Length > 0);
            Assert.True(item["text"]!.GetValue<string>().Length > 0);
        }
    }

    /// <summary>The lexicon shard carries LiteSpec's own words: type labels, layer writers, link phrases and finding meanings.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void LexiconShardCarriesTypesAndLinks()
    {
        using var consumer = ModelFixture.Prepare("small");
        using var temp = TestCli.Temp("book");
        var result = TestCli.Run(
            "book",
            "--root", consumer.Path,
            "--out", temp.Path);
        Assert.Equal(0, result.Status);

        var lexicon = ReadShard(Path.Combine(temp.Path, "data", "lexicon.js"), "lexicon");
        var types = lexicon["types"]!.AsObject();
        Assert.True(types.Count > 0);
        Assert.Equal("Goal", types["goal"]!.AsObject()["label"]!.GetValue<string>());
        Assert.NotEmpty(lexicon["links"]!.AsArray());
    }

    /// <summary>The --root and --editor values reach the manifest; --editor none suppresses source links.</summary>
    /// <proves>litespec/core/model#point-every-book-entry-to-its-source</proves>
    [Fact]
    public void RootAndEditorOptionsReachTheManifest()
    {
        using var consumer = ModelFixture.Prepare("small");
        using var temp = TestCli.Temp("book");
        var result = TestCli.Run(
            "book",
            "--root", consumer.Path,
            "--out", Path.Combine(temp.Path, "out"),
            "--editor", "none");
        Assert.Equal(0, result.Status);

        var manifest = ReadShard(Path.Combine(temp.Path, "out", "data", "manifest.js"), "manifest");
        Assert.Equal(Path.GetFullPath(consumer.Path), manifest["roots"]!.AsObject()["consumer"]!.GetValue<string>());
        Assert.Equal("none", manifest["editor"]!.GetValue<string>());

        using var custom = TestCli.Temp("book-editor");
        result = TestCli.Run(
            "book",
            "--root", consumer.Path,
            "--out", custom.Path,
            "--editor", "myeditor://open?f={root}/{path}&l={line}");
        Assert.Equal(0, result.Status);

        manifest = ReadShard(Path.Combine(custom.Path, "data", "manifest.js"), "manifest");
        Assert.Equal("myeditor://open?f={root}/{path}&l={line}", manifest["editor"]!.GetValue<string>());
    }

    /// <summary>A screen capture the model lists is copied into the book output and its shard names the book path.</summary>
    /// <proves>litespec/core/model#point-every-book-entry-to-its-source</proves>
    [Fact]
    public void ScreenCapturesAreCopiedIntoTheBookOutput()
    {
        using var temp = TestCli.Temp("book");
        var plain = Path.Combine(temp.Path, "plain");
        Directory.CreateDirectory(Path.Combine(plain, "shots"));
        File.WriteAllBytes(Path.Combine(plain, "shots", "home.png"), [137, 80, 78, 71]);
        var model = (JsonObject)RecordText.Read(TestCli.Fixture("Book/model.json"))!;
        var screen = model["records"]!.AsArray().OfType<JsonObject>().Single(record => record["type"]!.GetValue<string>() == "screen");
        screen["captures"] = JsonNode.Parse("""[{"path":"shots/home.png","route":"/home","theme":"light","width":1440,"capturedOn":"2026-11-01"}]""");
        screen["captureState"] = "captured";

        BookTestBooks.Write(model, plain, Path.Combine(temp.Path, "out"));

        var copied = Path.Combine(temp.Path, "out", "assets", "captures", "screen-library-app-search", "home.png");
        Assert.True(File.Exists(copied), "The screen capture was not copied into the book output.");
        var shard = string.Join("\n", Directory.GetFiles(Path.Combine(temp.Path, "out", "data"), "records-*.js").Select(File.ReadAllText));
        Assert.Contains("assets/captures/screen-library-app-search/home.png", shard, StringComparison.Ordinal);
    }

    /// <summary>The consumer root defaults to the consumer found from the working directory, and the output to its book folder.</summary>
    /// <proves>litespec/core/model#point-every-book-entry-to-its-source</proves>
    [Fact]
    public void RootAndOutDefaultToTheConsumerAndItsBookFolder()
    {
        using var consumer = ModelFixture.Prepare("small");

        // No --out: the book lands in .litespec/book under the consumer root.
        var result = TestCli.Run("book", "--root", consumer.Path);
        Assert.Equal(0, result.Status);

        var bookDir = Path.Combine(consumer.Path, ".litespec", "book");
        Assert.True(File.Exists(Path.Combine(bookDir, "index.html")), "index.html was not written to the default book folder.");
        var manifest = ReadShard(Path.Combine(bookDir, "data", "manifest.js"), "manifest");
        Assert.Equal(Path.GetFullPath(consumer.Path), manifest["roots"]!.AsObject()["consumer"]!.GetValue<string>());

        // No --root: the consumer is found by walking up from the working directory.
        using var temp = TestCli.Temp("book");
        result = TestCli.Run(
            ["book", "--out", Path.Combine(temp.Path, "cwd-out")],
            workingDirectory: Path.Combine(consumer.Path, ".litespec", "behavior"));
        Assert.Equal(0, result.Status);

        // The operating system hands the CLI its working directory resolved, so on macOS a consumer under
        // /var arrives as /private/var. The reported root is the consumer folder when it carries the
        // consumer's own folder name and holds its project file, in either form.
        manifest = ReadShard(Path.Combine(temp.Path, "cwd-out", "data", "manifest.js"), "manifest");
        var found = manifest["roots"]!.AsObject()["consumer"]!.GetValue<string>();
        Assert.Equal(Path.GetFileName(Path.GetFullPath(consumer.Path)), Path.GetFileName(found));
        Assert.True(File.Exists(Path.Combine(found, ".litespec", "project.yaml")), $"The reported root '{found}' holds no project file.");
    }

    /// <summary>A root without a project file exits 2 and names the missing file, the usage error for a root that is not a consumer.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void BookWithoutProjectFileExits2()
    {
        using var temp = TestCli.Temp("book");
        var result = TestCli.Run("book", "--root", temp.Path);
        Assert.Equal(2, result.Status);
        Assert.Contains(".litespec/project.yaml", result.Output);
    }

    /// <summary>An unwritable output exits 1 and names the output folder.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void UnwritableOutputExits1()
    {
        using var consumer = ModelFixture.Prepare("small");
        using var temp = TestCli.Temp("book");
        var blocker = Path.Combine(temp.Path, "blocker");
        File.WriteAllText(blocker, "not a folder");
        var result = TestCli.Run("book", "--root", consumer.Path, "--out", blocker);
        Assert.Equal(1, result.Status);
        Assert.Contains(blocker, result.Output, StringComparison.Ordinal);
    }

    /// <summary>A carried LiteSpec page that lost its shape exits 2 and names the file.</summary>
    /// <proves>litespec/core/model#add-a-missing-fact-to-litespec-before-the-book-shows-it</proves>
    [Fact]
    public void BrokenCarriedPageExits2NamingTheFile()
    {
        using var consumer = ModelFixture.Prepare("small");
        using var temp = TestCli.Temp("book");
        using var home = TestCli.Temp("book-home");
        File.WriteAllText(Path.Combine(home.Path, "litespec.manifest.yaml"), """{"version":"5.2.0"}""");
        var result = TestCli.Run(
            ["book", "--root", consumer.Path, "--out", temp.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });
        Assert.Equal(2, result.Status);
        Assert.Contains("model.md", result.Output, StringComparison.Ordinal);
    }

    /// <summary>A missing carried LiteSpec release exits 2, like every other command.</summary>
    /// <proves>litespec/core/model#add-a-missing-fact-to-litespec-before-the-book-shows-it</proves>
    [Fact]
    public void MissingCarriedReleaseExits2()
    {
        using var consumer = ModelFixture.Prepare("small");
        using var temp = TestCli.Temp("book");
        using var home = TestCli.Temp("book-no-home");
        var result = TestCli.Run(
            ["book", "--root", consumer.Path, "--out", temp.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });
        Assert.Equal(2, result.Status);
        Assert.Contains("Cannot find the carried LiteSpec release", result.Output, StringComparison.Ordinal);
    }

    static JsonObject ReadShard(string path, string key)
    {
        var text = File.ReadAllText(path);
        var prefix = $"BookData.put(\"{key}\", ";
        Assert.StartsWith(prefix, text, StringComparison.Ordinal);
        Assert.EndsWith(");\n", text, StringComparison.Ordinal);
        return JsonNode.Parse(text[prefix.Length..^3])!.AsObject();
    }

    static JsonArray ReadShardArray(string path, string key)
    {
        var text = File.ReadAllText(path);
        var prefix = $"BookData.put(\"{key}\", ";
        Assert.StartsWith(prefix, text, StringComparison.Ordinal);
        Assert.EndsWith(");\n", text, StringComparison.Ordinal);
        return JsonNode.Parse(text[prefix.Length..^3])!.AsArray();
    }
}
