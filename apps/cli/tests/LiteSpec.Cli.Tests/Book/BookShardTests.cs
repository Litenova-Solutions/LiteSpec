using System.Text.Json.Nodes;
using LiteSpec.Book;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Book;

/// <summary>Proves the book shard writer packs records, outlines and search rows under their caps with script-callable wrappers.</summary>
public sealed class BookShardTests
{
    /// <summary>Packing fills each shard to its target, keeps one oversized item whole, and packs nothing to nothing.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Pack_Splits_Past_The_Target_And_Keeps_Big_Items_Whole()
    {
        var packs = BookShards.Pack(new[] { "a", "b", "c" }, _ => 100, 256);
        Assert.Equal(2, packs.Count);
        Assert.Equal(new[] { "a", "b" }, packs[0].ToList());
        Assert.Equal(new[] { "c" }, packs[1].ToList());

        var big = BookShards.Pack(new[] { "big" }, _ => 600 * 1024, 256 * 1024);
        Assert.Single(big);
        Assert.Equal(new[] { "big" }, big[0].ToList());

        Assert.Empty(BookShards.Pack(Array.Empty<string>(), _ => 1, 256));
    }

    /// <summary>Records pack in identifier order with zero-padded shard ids, a full shard map, and every shard under the hard cap.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Records_Write_In_Identifier_Order_With_A_Full_Shard_Map()
    {
        using var temp = Cli.Temp("book-shards");
        var details = new Dictionary<string, JsonObject>(StringComparer.Ordinal)
        {
            ["use-case/lending.borrow-copy"] = Detail("use-case/lending.borrow-copy", "use-case", "Borrow Copy"),
            ["goal/faster-borrowing"] = Detail("goal/faster-borrowing", "goal", "Faster Borrowing"),
            ["feature/self-checkout"] = Detail("feature/self-checkout", "feature", "Self Checkout", new string('x', 300 * 1024)),
        };

        var dataDir = Path.Combine(temp.Path, "data");
        var shardOf = BookShards.WriteRecords(dataDir, details);

        Assert.Equal(3, shardOf.Count);
        Assert.Equal("records-000", shardOf["feature/self-checkout"]);
        Assert.Equal("records-001", shardOf["goal/faster-borrowing"]);
        Assert.Equal("records-001", shardOf["use-case/lending.borrow-copy"]);
        Assert.True(File.Exists(Path.Combine(dataDir, "records-000.js")), "records-000.js was not written.");
        Assert.True(File.Exists(Path.Combine(dataDir, "records-001.js")), "records-001.js was not written.");

        var first = ReadShard(Path.Combine(dataDir, "records-000.js"), "records-000");
        Assert.Equal(new[] { "feature/self-checkout" }, PropertyNames(first));
        var second = ReadShard(Path.Combine(dataDir, "records-001.js"), "records-001");
        Assert.Equal(new[] { "goal/faster-borrowing", "use-case/lending.borrow-copy" }, PropertyNames(second));

        foreach (var file in Directory.GetFiles(dataDir, "*.js"))
        {
            Assert.True(new FileInfo(file).Length <= BookShards.PackCapBytes, $"{file} breaks the 512 KB cap.");
        }
    }

    /// <summary>Record outline nodes carry their records shard id; group nodes carry none; children nest below.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Outline_Nodes_Carry_Their_Record_Shard()
    {
        var shardOf = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["goal/faster-borrowing"] = "records-000",
            ["feature/self-checkout"] = "records-001",
        };
        var node = new BookOutlineNode(
            "intent/goal",
            "group",
            "Goals",
            "goal",
            null,
            new List<BookOutlineNode>
            {
                new("goal/faster-borrowing", "record", "Faster Borrowing", "goal", "approved", new List<BookOutlineNode>
                {
                    new("feature/self-checkout", "record", "Self Checkout", "feature", "draft", new List<BookOutlineNode>()),
                }),
            });

        var json = BookShards.OutlineNodeJson(node, shardOf);

        Assert.Equal("group", json["kind"]!.GetValue<string>());
        Assert.Null(json["shard"]);
        var goal = json["children"]!.AsArray()[0]!.AsObject();
        Assert.Equal("records-000", goal["shard"]!.GetValue<string>());
        Assert.Equal("Faster Borrowing", goal["label"]!.GetValue<string>());
        Assert.Equal("goal", goal["type"]!.GetValue<string>());
        Assert.Equal("approved", goal["status"]!.GetValue<string>());
        var feature = goal["children"]!.AsArray()[0]!.AsObject();
        Assert.Equal("records-001", feature["shard"]!.GetValue<string>());
    }

    /// <summary>Code, test and finding outline nodes carry their kind with no shard, nested under their subject.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Outline_Leaf_Nodes_Carry_Their_Kind_With_No_Shard()
    {
        var shardOf = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["use-case/lending.borrow-copy"] = "records-000",
        };
        var node = new BookOutlineNode(
            "use-case/lending.borrow-copy",
            "record",
            "Borrow Copy",
            "use-case",
            "reviewed",
            new List<BookOutlineNode>
            {
                new("code/src/App/Borrow.cs", "code", "Borrow.cs", "code", null, new List<BookOutlineNode>()),
                new("test/Library.Tests.BorrowTests.Borrowing", "test", "Borrowing", "test", null, new List<BookOutlineNode>()),
                new("finding/1", "finding", "Missing handler.", "finding", null, new List<BookOutlineNode>()),
            });

        var json = BookShards.OutlineNodeJson(node, shardOf);

        Assert.Equal("records-000", json["shard"]!.GetValue<string>());
        var children = json["children"]!.AsArray();
        Assert.Equal(3, children.Count);
        Assert.Equal("code", children[0]!.AsObject()["kind"]!.GetValue<string>());
        Assert.Equal("Borrow.cs", children[0]!.AsObject()["label"]!.GetValue<string>());
        Assert.Equal("test", children[1]!.AsObject()["kind"]!.GetValue<string>());
        Assert.Equal("finding", children[2]!.AsObject()["kind"]!.GetValue<string>());
        foreach (var child in children.OfType<JsonObject>())
        {
            Assert.Null(child["shard"]);
        }
    }

    /// <summary>A layer past the outline budget splits into sequential shards that hold every top node in order.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Layers_Split_Into_Sequential_Shards_Past_Seven_Hundred_Kilobytes()
    {
        using var temp = Cli.Temp("book-shards");
        var dataDir = Path.Combine(temp.Path, "data");
        var tops = new List<BookOutlineNode>();
        for (var i = 0; i < 80; i++)
        {
            tops.Add(new BookOutlineNode(
                $"goal/g-{i:000}",
                "record",
                $"G-{i:000} " + new string('w', 9000),
                "goal",
                "approved",
                new List<BookOutlineNode>()));
        }

        var shardOf = tops.ToDictionary(node => node.Id, _ => "records-000", StringComparer.Ordinal);
        var keys = BookShards.WriteLayer(dataDir, "intent", tops, shardOf);

        Assert.True(keys.Count >= 2, "The layer did not split.");
        Assert.Equal(keys.Select((_, at) => $"outline-intent-{at}").ToList(), keys.ToList());
        foreach (var key in keys)
        {
            var file = Path.Combine(dataDir, key + ".js");
            Assert.True(File.Exists(file), $"{key}.js was not written.");
            Assert.True(
                new FileInfo(file).Length <= BookShards.OutlineSplitBytes + 4096,
                $"{key}.js breaks the outline budget.");
        }

        var seen = new List<string>();
        foreach (var key in keys)
        {
            foreach (var node in ReadShardArray(Path.Combine(dataDir, key + ".js"), key).OfType<JsonObject>())
            {
                seen.Add(node["id"]!.GetValue<string>());
            }
        }

        Assert.Equal(tops.Select(node => node.Id).ToList(), seen);
    }

    /// <summary>Search rows pack in order into sequential shards with script-callable wrappers.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Search_Packs_Rows_In_Order()
    {
        using var temp = Cli.Temp("book-shards");
        var dataDir = Path.Combine(temp.Path, "data");
        var rows = new JsonArray();
        for (var i = 0; i < 5; i++)
        {
            rows.Add(new JsonArray($"goal/g-{i}", $"G {i}", "goal", $"excerpt {i}"));
        }

        var keys = BookShards.WriteSearch(dataDir, rows);

        Assert.Equal(new[] { "search-0" }, keys.ToList());
        var back = ReadShardArray(Path.Combine(dataDir, "search-0.js"), "search-0");
        Assert.Equal(5, back.Count);
        Assert.Equal("goal/g-3", back[3]!.AsArray()[0]!.GetValue<string>());
        Assert.Equal("excerpt 3", back[3]!.AsArray()[3]!.GetValue<string>());
    }

    /// <summary>Every shard file holds exactly one BookData.put call whose JSON parses back to the value, with markup escaped.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Shard_Files_Call_BookData_Put_With_Escaped_Json()
    {
        using var temp = Cli.Temp("book-shards");
        var dataDir = Path.Combine(temp.Path, "data");
        var value = new JsonObject
        {
            ["bodyHtml"] = "</script><b>Bold</b>",
            ["line"] = "a" + ((char)0x2028).ToString() + "b",
        };
        BookShards.WriteShard(dataDir, "records-000", value);

        var text = File.ReadAllText(Path.Combine(dataDir, "records-000.js"));
        Assert.StartsWith("BookData.put(\"records-000\", ", text, StringComparison.Ordinal);
        Assert.EndsWith(");\n", text, StringComparison.Ordinal);
        Assert.Equal(1, text.Split("BookData.put(").Length - 1);
        Assert.DoesNotContain("</script", text, StringComparison.Ordinal);
        Assert.DoesNotContain(((char)0x2028).ToString(), text, StringComparison.Ordinal);

        var back = ReadShard(Path.Combine(dataDir, "records-000.js"), "records-000");
        Assert.Equal("</script><b>Bold</b>", back["bodyHtml"]!.GetValue<string>());
        Assert.Equal("a" + ((char)0x2028).ToString() + "b", back["line"]!.GetValue<string>());
    }

    /// <summary>Search excerpts prefer the statement-like fields, then the body's head on one line cut to 200 characters.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Search_Excerpts_Prefer_Statement_Then_Body()
    {
        var stated = new JsonObject
        {
            ["fields"] = new JsonObject
            {
                ["statement"] = "Wait under ten minutes.",
                ["summary"] = "Short waits.",
            },
            ["body"] = "Long prose.",
        };
        Assert.Equal("Wait under ten minutes.", BookShards.SearchExcerpt(stated));

        var summarized = new JsonObject
        {
            ["fields"] = new JsonObject { ["summary"] = "Short waits." },
            ["body"] = "Long prose.",
        };
        Assert.Equal("Short waits.", BookShards.SearchExcerpt(summarized));

        var bodied = new JsonObject
        {
            ["fields"] = new JsonObject(),
            ["body"] = "First line.\nSecond line.",
        };
        Assert.Equal("First line. Second line.", BookShards.SearchExcerpt(bodied));

        var longBody = new JsonObject
        {
            ["fields"] = new JsonObject(),
            ["body"] = new string('z', 500),
        };
        Assert.Equal(200, BookShards.SearchExcerpt(longBody).Length);

        Assert.Equal("", BookShards.SearchExcerpt(new JsonObject { ["fields"] = new JsonObject() }));
    }

    static JsonObject Detail(string id, string type, string title, string? pad = null)
    {
        var detail = new JsonObject
        {
            ["type"] = type,
            ["title"] = title,
            ["fields"] = new JsonObject(),
        };

        if (pad is not null)
        {
            detail["pad"] = pad;
        }

        return detail;
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

    static List<string> PropertyNames(JsonObject obj) =>
        obj.Select(pair => pair.Key).ToList();
}
