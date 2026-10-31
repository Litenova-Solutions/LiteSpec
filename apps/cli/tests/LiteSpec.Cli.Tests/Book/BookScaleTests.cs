using System.Text.Json.Nodes;
using LiteSpec.Book;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Book;

/// <summary>Proves the book holds NightHall size: 21,500 records pack under their shard caps with a complete outline and a full shard map.</summary>
public sealed class BookScaleTests
{
    const int ScaleRecords = 21500;
    const long FirstPaintBudgetBytes = 500L * 1024;
    const long LayerBudgetBytes = 1536L * 1024;

    sealed record OutlineCheck(string Id, string? Shard);

    /// <summary>Every records and search shard stays under the hard cap, the behavior outline splits past its budget, and the manifest counts every record.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Scale_Model_Packs_Every_Shard_Under_Its_Cap()
    {
        using var temp = Cli.Temp("book-scale");
        var result = Cli.Run("book", "--model", WriteScaleModel(temp), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var data = Path.Combine(Out(temp), "data");
        var recordShards = Directory.GetFiles(data, "records-*.js");
        Assert.True(recordShards.Length > 1, "The scale model packed into one records shard.");
        foreach (var file in recordShards)
        {
            Assert.True(new FileInfo(file).Length <= BookShards.PackCapBytes, $"{file} breaks the 512 KB cap.");
        }

        foreach (var file in Directory.GetFiles(data, "search-*.js"))
        {
            Assert.True(new FileInfo(file).Length <= BookShards.PackCapBytes, $"{file} breaks the 512 KB cap.");
        }

        foreach (var file in Directory.GetFiles(data, "outline-*.js"))
        {
            Assert.True(
                new FileInfo(file).Length <= BookShards.OutlineSplitBytes + 4096,
                $"{file} breaks the outline budget.");
        }

        Assert.True(
            Directory.GetFiles(data, "outline-behavior-*.js").Length > 1,
            "The behavior outline did not split.");

        var manifest = ReadShard(Path.Combine(data, "manifest.js"), "manifest");
        var total = manifest["totals"]!.AsObject().Sum(pair => pair.Value!.GetValue<int>());
        Assert.Equal(ScaleRecords, total);
        Assert.Contains("21500 records", result.Output, StringComparison.Ordinal);
    }

    /// <summary>First paint stays under 500 KB and every layer outline under 1.5 MB: the shell, fonts, styles, script, manifest, lexicon and Intent outline load first, and each layer outline loads on first open.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Budgets_Hold_First_Paint_And_Layers()
    {
        using var temp = Cli.Temp("book-scale");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var firstPaint = new[]
        {
            Path.Combine(Out(temp), "index.html"),
            Path.Combine(Out(temp), "assets", "book.js"),
            Path.Combine(Out(temp), "assets", "book.css"),
            Path.Combine(Out(temp), "assets", "book-extra.css"),
            Path.Combine(Out(temp), "assets", "book-fonts.css"),
            Path.Combine(Out(temp), "data", "manifest.js"),
            Path.Combine(Out(temp), "data", "lexicon.js"),
        }.Concat(Directory.GetFiles(Path.Combine(Out(temp), "data"), "outline-intent-*.js"));
        var firstBytes = firstPaint.Sum(file => new FileInfo(file).Length);
        Assert.True(firstBytes <= FirstPaintBudgetBytes, $"First paint loads {firstBytes} bytes, past the 500 KB budget.");

        foreach (var layer in BookOutline.LayerKeys)
        {
            var layerBytes = Directory
                .GetFiles(Path.Combine(Out(temp), "data"), $"outline-{layer}-*.js")
                .Sum(file => new FileInfo(file).Length);
            Assert.True(layerBytes <= LayerBudgetBytes, $"The {layer} outline loads {layerBytes} bytes, past the 1.5 MB budget.");
        }
    }

    /// <summary>The outline places every scale record exactly once, with none unplaced.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Scale_Outline_Places_Every_Record_Exactly_Once()
    {
        using var temp = Cli.Temp("book-scale");
        var result = Cli.Run("book", "--model", WriteScaleModel(temp), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var data = Path.Combine(Out(temp), "data");
        var manifest = ReadShard(Path.Combine(data, "manifest.js"), "manifest");
        var placed = RecordNodes(data, manifest);

        Assert.Equal(ScaleRecords, placed.Count);
        Assert.Equal(ScaleRecords, placed.Select(node => node.Id).ToHashSet(StringComparer.Ordinal).Count);
        Assert.True(
            ScaleIds().SetEquals(placed.Select(node => node.Id)),
            "The outline misses scale records or holds strangers.");
        Assert.Empty(manifest["unplaced"]!.AsArray());
    }

    /// <summary>Every scale record sits in the records shard the manifest map names, and every outline node and search row reaches it.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Scale_Shard_Map_Reaches_Every_Record()
    {
        using var temp = Cli.Temp("book-scale");
        var result = Cli.Run("book", "--model", WriteScaleModel(temp), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var data = Path.Combine(Out(temp), "data");
        var manifest = ReadShard(Path.Combine(data, "manifest.js"), "manifest");
        var shardOf = manifest["shardOf"]!.AsObject();
        Assert.Equal(ScaleRecords, shardOf.Count);

        var expected = ScaleIds();
        foreach (var id in expected)
        {
            Assert.True(shardOf.ContainsKey(id), $"The shard map names no shard for '{id}'.");
        }

        var found = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(data, "records-*.js"))
        {
            var key = Path.GetFileNameWithoutExtension(file);
            foreach (var pair in ReadShard(file, key))
            {
                found[pair.Key] = key;
            }
        }

        Assert.Equal(ScaleRecords, found.Count);
        foreach (var id in expected)
        {
            Assert.True(found.TryGetValue(id, out var key), $"No records shard holds '{id}'.");
            Assert.Equal(shardOf[id]!.GetValue<string>(), key);
        }

        foreach (var node in RecordNodes(data, manifest))
        {
            Assert.Equal(shardOf[node.Id]!.GetValue<string>(), node.Shard);
        }

        var rows = 0;
        foreach (var key in manifest["search"]!.AsArray())
        {
            var name = key!.GetValue<string>();
            rows += ReadShardArray(Path.Combine(data, name + ".js"), name).Count;
        }

        Assert.Equal(ScaleRecords, rows);
    }

    static string Out(TempFolder temp) => Path.Combine(temp.Path, "out");

    static string WriteScaleModel(TempFolder temp)
    {
        var records = new JsonArray();
        foreach (var entry in ScaleEntries())
        {
            records.Add(entry);
        }

        var model = new JsonObject
        {
            ["version"] = 1,
            ["root"] = ".",
            ["records"] = records,
            ["code"] = new JsonObject
            {
                ["handlers"] = new JsonArray(),
                ["endpoints"] = new JsonArray(),
                ["aggregateMethods"] = new JsonArray(),
                ["automations"] = new JsonArray(),
            },
            ["links"] = new JsonObject
            {
                ["implements"] = new JsonArray(),
                ["calls"] = new JsonArray(),
                ["proves"] = new JsonArray(),
                ["follows"] = new JsonArray(),
            },
            ["status"] = new JsonObject(),
            ["requirementStatus"] = new JsonObject(),
            ["reachableStates"] = new JsonObject(),
            ["stale"] = new JsonArray(),
            ["tests"] = new JsonArray(),
            ["browserTests"] = new JsonArray(),
            ["screens"] = new JsonArray(),
            ["exercisedSteps"] = new JsonArray(),
            ["findings"] = new JsonArray(),
            ["known"] = new JsonArray(),
            ["testRun"] = null,
        };

        var path = Path.Combine(temp.Path, "model.json");
        File.WriteAllText(path, model.ToJsonString());
        return path;
    }

    static List<JsonObject> ScaleEntries()
    {
        var entries = new List<JsonObject> { ScaleRecord("product", "product/scale", "Scale", "approved") };
        for (var goal = 0; goal < 1299; goal++)
        {
            entries.Add(ScaleRecord("goal", $"goal/g-{goal:0000}", $"G {goal:0000}", "approved"));
        }

        for (var module = 0; module < 200; module++)
        {
            entries.Add(ScaleRecord("module", $"module/m{module:000}", $"M {module:000}", "reviewed"));
        }

        for (var item = 0; item < 10000; item++)
        {
            entries.Add(ScaleRecord("aggregate", $"aggregate/m{item / 50:000}.a{item:00000}", $"A {item:00000}", "reviewed"));
            entries.Add(ScaleRecord("use-case", $"use-case/m{item / 50:000}.u{item:00000}", $"U {item:00000}", "reviewed"));
        }

        return entries;
    }

    static HashSet<string> ScaleIds() =>
        new(ScaleEntries().Select(entry => entry["id"]!.GetValue<string>()), StringComparer.Ordinal);

    static JsonObject ScaleRecord(string type, string id, string title, string status) =>
        new()
        {
            ["type"] = type,
            ["id"] = id,
            ["title"] = title,
            ["owner"] = "scale-team",
            ["status"] = status,
            ["effectiveStatus"] = status,
            ["decider"] = null,
            ["decideBy"] = null,
            ["file"] = "docs/scale.md",
            ["line"] = 3,
            ["fields"] = new JsonObject(),
            ["links"] = new JsonObject(),
        };

    static List<OutlineCheck> RecordNodes(string data, JsonObject manifest)
    {
        var placed = new List<OutlineCheck>();
        foreach (var layer in manifest["layers"]!.AsArray())
        {
            foreach (var key in layer!.AsObject()["outline"]!.AsArray())
            {
                var name = key!.GetValue<string>();
                foreach (var node in ReadShardArray(Path.Combine(data, name + ".js"), name).OfType<JsonObject>())
                {
                    Collect(node, placed);
                }
            }
        }

        return placed;
    }

    static void Collect(JsonObject node, List<OutlineCheck> placed)
    {
        if (string.Equals(node["kind"]!.GetValue<string>(), "record", StringComparison.Ordinal))
        {
            placed.Add(new OutlineCheck(
                node["id"]!.GetValue<string>(),
                node["shard"] is JsonValue shard && shard.TryGetValue<string>(out var name) ? name : null));
        }

        foreach (var child in node["children"]!.AsArray().OfType<JsonObject>())
        {
            Collect(child, placed);
        }
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
