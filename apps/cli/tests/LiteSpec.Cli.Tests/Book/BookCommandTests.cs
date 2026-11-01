using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Book;

/// <summary>Proves <c>litespec book</c> writes one offline shell with sharded data from a fixture model.</summary>
public sealed class BookCommandTests
{
    /// <summary>The book writes <c>index.html</c>, the four assets, and the manifest, lexicon, outline, record, search and queue shards.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Book_Writes_The_Sharded_Book()
    {
        using var temp = Cli.Temp("book");
        var result = Cli.Run(
            "book",
            "--model", Cli.Fixture("Book/model.json"),
            "--out", temp.Path);
        Assert.Equal(0, result.Status);

        Assert.True(File.Exists(Path.Combine(temp.Path, "index.html")), "index.html was not written.");
        foreach (var asset in new[] { "book.css", "book-extra.css", "book-fonts.css", "book.js" })
        {
            Assert.True(File.Exists(Path.Combine(temp.Path, "assets", asset)), $"assets/{asset} was not written.");
        }

        var data = Path.Combine(temp.Path, "data");
        foreach (var shard in new[] { "manifest.js", "lexicon.js", "queue.js" })
        {
            Assert.True(File.Exists(Path.Combine(data, shard)), $"data/{shard} was not written.");
        }

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
        Assert.Contains("window.BookData", shell, StringComparison.Ordinal);
        Assert.Contains("data/manifest.js", shell, StringComparison.Ordinal);
        Assert.Contains("<noscript>", shell, StringComparison.Ordinal);
        Assert.Contains("Library", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("{{title}}", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("{{noscript}}", shell, StringComparison.Ordinal);
    }

    /// <summary>The manifest carries the project, roots, editor template, totals, layers, shard map, search shards, unplaced ids and generation date.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Manifest_Carries_The_Book_Contract()
    {
        using var temp = Cli.Temp("book");
        var result = Cli.Run(
            "book",
            "--model", Cli.Fixture("Book/model.json"),
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
        Assert.Equal(83, total);

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
        Assert.Contains("83 records", result.Output, StringComparison.Ordinal);
    }

    /// <summary>The console line names each unplaced identifier with the failed record-parent-missing rule.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Unplaced_Console_Line_Names_The_Failed_Rule()
    {
        using var temp = Cli.Temp("book");
        var result = Cli.Run(
            "book",
            "--model", Cli.Fixture("Book/review.json"),
            "--out", temp.Path);
        Assert.Equal(0, result.Status);

        Assert.Contains("record-parent-missing", result.Output, StringComparison.Ordinal);
        Assert.Contains("journey/borrowing", result.Output, StringComparison.Ordinal);
    }

    /// <summary>The queue shard carries the review batches with their order, the ballots, the security items and the review settings.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Queue_Carries_Batches_Ballots_And_Security()
    {
        using var temp = Cli.Temp("book");
        var result = Cli.Run(
            "book",
            "--model", Cli.Fixture("Book/model.json"),
            "--out", temp.Path);
        Assert.Equal(0, result.Status);

        var queue = ReadShard(Path.Combine(temp.Path, "data", "queue.js"), "queue");
        Assert.NotNull(queue["batches"]);
        Assert.NotNull(queue["order"]);
        Assert.True(queue["ballots"] is JsonArray);
        Assert.True(queue["security"] is JsonArray);
        var review = queue["review"]!.AsObject();
        Assert.True(review["vetoWindowDays"]!.GetValue<int>() > 0);

        foreach (var batch in queue["batches"]!.AsObject())
        {
            var value = batch.Value!.AsObject();
            Assert.Equal(batch.Key, value["selector"]!.GetValue<string>());
            Assert.True(value["records"] is JsonArray);
            Assert.Equal(value["records"]!.AsArray().Count, value["n"]!.GetValue<int>());
        }

        foreach (var ballot in queue["ballots"]!.AsArray().OfType<JsonObject>())
        {
            Assert.True(ballot["id"]!.GetValue<string>().Length > 0);
            Assert.NotNull(ballot["q"]);
        }
    }

    /// <summary>The lexicon shard carries LiteSpec's own words: type labels, layer writers, link phrases and finding meanings.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Lexicon_Shard_Carries_Types_And_Links()
    {
        using var temp = Cli.Temp("book");
        var result = Cli.Run(
            "book",
            "--model", Cli.Fixture("Book/model.json"),
            "--out", temp.Path);
        Assert.Equal(0, result.Status);

        var lexicon = ReadShard(Path.Combine(temp.Path, "data", "lexicon.js"), "lexicon");
        var types = lexicon["types"]!.AsObject();
        Assert.True(types.Count > 0);
        Assert.Equal("Goal", types["goal"]!.AsObject()["label"]!.GetValue<string>());
        Assert.NotEmpty(lexicon["links"]!.AsArray());
    }

    /// <summary>The --root and --editor values reach the manifest; --editor none suppresses source links.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Root_And_Editor_Options_Reach_The_Manifest()
    {
        using var temp = Cli.Temp("book");
        var result = Cli.Run(
            "book",
            "--model", Cli.Fixture("Book/model.json"),
            "--out", Path.Combine(temp.Path, "out"),
            "--root", temp.Path,
            "--editor", "none");
        Assert.Equal(0, result.Status);

        var manifest = ReadShard(Path.Combine(temp.Path, "out", "data", "manifest.js"), "manifest");
        Assert.Equal(Path.GetFullPath(temp.Path), manifest["roots"]!.AsObject()["consumer"]!.GetValue<string>());
        Assert.Equal("none", manifest["editor"]!.GetValue<string>());

        using var custom = Cli.Temp("book-editor");
        result = Cli.Run(
            "book",
            "--model", Cli.Fixture("Book/model.json"),
            "--out", custom.Path,
            "--editor", "myeditor://open?f={root}/{path}&l={line}");
        Assert.Equal(0, result.Status);

        manifest = ReadShard(Path.Combine(custom.Path, "data", "manifest.js"), "manifest");
        Assert.Equal("myeditor://open?f={root}/{path}&l={line}", manifest["editor"]!.GetValue<string>());
    }

    /// <summary>The consumer root defaults to the model's folder, or its parent when the model sits in a .model folder.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Root_Defaults_To_The_Model_Folder_Or_Its_Parent()
    {
        using var temp = Cli.Temp("book");
        var plain = Path.Combine(temp.Path, "plain");
        Directory.CreateDirectory(plain);
        var model = Path.Combine(plain, "model.json");
        File.Copy(Cli.Fixture("Book/model.json"), model);
        var result = Cli.Run("book", "--model", model, "--out", Path.Combine(temp.Path, "out"));
        Assert.Equal(0, result.Status);

        var manifest = ReadShard(Path.Combine(temp.Path, "out", "data", "manifest.js"), "manifest");
        Assert.Equal(Path.GetFullPath(plain), manifest["roots"]!.AsObject()["consumer"]!.GetValue<string>());

        var dotModel = Path.Combine(temp.Path, "consumer", ".model");
        Directory.CreateDirectory(dotModel);
        var nested = Path.Combine(dotModel, "model.json");
        File.Copy(Cli.Fixture("Book/model.json"), nested);
        result = Cli.Run("book", "--model", nested, "--out", Path.Combine(temp.Path, "nested-out"));
        Assert.Equal(0, result.Status);

        manifest = ReadShard(Path.Combine(temp.Path, "nested-out", "data", "manifest.js"), "manifest");
        Assert.Equal(
            Path.GetFullPath(Path.Combine(temp.Path, "consumer")),
            manifest["roots"]!.AsObject()["consumer"]!.GetValue<string>());
    }

    /// <summary>A missing --model exits 2 and names the missing option, the usage error for bad arguments.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Book_Without_Model_Exits_2()
    {
        using var temp = Cli.Temp("book");
        var result = Cli.Run("book", "--out", temp.Path);
        Assert.Equal(2, result.Status);
        Assert.Contains("--model", result.Output);
    }

    /// <summary>An unreadable model exits 1 and names the model file.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Missing_Model_File_Exits_1()
    {
        using var temp = Cli.Temp("book");
        var result = Cli.Run("book", "--model", Path.Combine(temp.Path, "missing.json"), "--out", temp.Path);
        Assert.Equal(1, result.Status);
        Assert.Contains("missing.json", result.Output, StringComparison.Ordinal);
    }

    /// <summary>A carried LiteSpec page that lost its shape exits 2 and names the file.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Broken_Carried_Page_Exits_2_Naming_The_File()
    {
        using var temp = Cli.Temp("book");
        using var home = Cli.Temp("book-home");
        File.WriteAllText(Path.Combine(home.Path, "litespec.manifest.json"), """{"version":"3.0.0"}""");
        var result = Cli.Run(
            ["book", "--model", Cli.Fixture("Book/model.json"), "--out", temp.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });
        Assert.Equal(2, result.Status);
        Assert.Contains("model.md", result.Output, StringComparison.Ordinal);
    }

    /// <summary>A missing carried LiteSpec release exits 2, like every other command.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Missing_Carried_Release_Exits_2()
    {
        using var temp = Cli.Temp("book");
        using var home = Cli.Temp("book-no-home");
        var result = Cli.Run(
            ["book", "--model", Cli.Fixture("Book/model.json"), "--out", temp.Path],
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
}
