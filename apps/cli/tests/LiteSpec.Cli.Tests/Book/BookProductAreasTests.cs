using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Details;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Book;

/// <summary>Proves the book groups Intent features and flows under the Behavior modules whose records reach them.</summary>
public sealed class BookProductAreasTests
{
    /// <summary>A module lists the features whose requirements its use cases, reactions, jobs or screens fulfill, and the flows whose steps they serve; a feature two modules reach is listed under each.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void ModulesListTheFeaturesAndFlowsTheirRecordsReach()
    {
        var model = Model(
            Record("product", "product/library", "Library"),
            Record("module", "module/lending", "Lending"),
            Record("module", "module/copies", "Copies"),
            Record("feature", "feature/self-checkout", "Self Checkout"),
            Record("requirement", "requirement/self-checkout.max-wait", "Max Wait"),
            Record("requirement", "requirement/evening-access", "Evening Access"),
            Record(
                "use-case",
                "use-case/lending.borrow-copy",
                "Borrow Copy",
                new JsonObject { ["fulfills"] = Ids("requirement/self-checkout.max-wait", "requirement/evening-access") }),
            Record(
                "job",
                "job/copies.send-overdue-notices",
                "Send Overdue Notices",
                new JsonObject { ["fulfills"] = Ids("requirement/self-checkout.max-wait") }),
            Record(
                "flow",
                "flow/borrowing",
                "Borrowing",
                new JsonObject { ["steps"] = Steps("s1", "s2") }),
            Record(
                "reaction",
                "reaction/lending.send-due-reminder",
                "Send Due Reminder",
                new JsonObject { ["serves"] = Ids("flow/borrowing#s1") }),
            Record(
                "screen",
                "screen/library-app.search",
                "Search",
                new JsonObject
                {
                    ["offers"] = Ids("use-case/lending.borrow-copy"),
                    ["serves"] = Ids("flow/borrowing#s2"),
                }));

        var areas = BookProductAreas.Build(model);

        Assert.Equal(
            new[] { "module/copies", "module/lending" },
            areas.Modules.Select(area => area.Id).ToList());
        var copies = areas.Modules.Single(area => area.Id == "module/copies");
        Assert.Equal("Copies", copies.Title);
        Assert.Equal(new[] { "feature/self-checkout" }, copies.Features.ToList());
        Assert.Empty(copies.Flows);
        var lending = areas.Modules.Single(area => area.Id == "module/lending");
        Assert.Equal(new[] { "feature/self-checkout" }, lending.Features.ToList());
        Assert.Equal(new[] { "flow/borrowing" }, lending.Flows.ToList());
        Assert.Empty(areas.Unfulfilled.Features);
        Assert.Empty(areas.Unfulfilled.Flows);

        // The product-level requirement belongs to no feature, so fulfilling it places nothing.
        var json = BookProductAreas.ToJson(areas);
        Assert.Equal(2, json["modules"]!.AsArray().Count);
        Assert.Equal("module/copies", json["modules"]![0]!["id"]!.GetValue<string>());
        Assert.Equal("Copies", json["modules"]![0]!["title"]!.GetValue<string>());
        Assert.Equal(
            new[] { "feature/self-checkout" },
            json["modules"]![0]!["features"]!.AsArray().Select(node => node!.GetValue<string>()).ToList());
        Assert.Empty(json["modules"]![0]!["flows"]!.AsArray());
        Assert.Empty(json["unfulfilled"]!["features"]!.AsArray());
        Assert.Empty(json["unfulfilled"]!["flows"]!.AsArray());
    }

    /// <summary>A feature no module fulfills and a flow no module serves are listed unfulfilled: one with no requirements or steps, one reached only by a rule, and one reached by nothing.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void UnreachedFeaturesAndFlowsListUnderUnfulfilled()
    {
        var model = Model(
            Record("product", "product/library", "Library"),
            Record("module", "module/lending", "Lending"),
            Record("feature", "feature/lonely", "Lonely"),
            Record("requirement", "requirement/lonely.quiet", "Quiet"),
            Record("feature", "feature/empty", "Empty"),
            Record("feature", "feature/ruled", "Ruled"),
            Record("requirement", "requirement/ruled.cap", "Cap"),
            Record(
                "rule",
                "rule/lending.cap",
                "Cap",
                new JsonObject { ["fulfills"] = Ids("requirement/ruled.cap") }),
            Record(
                "flow",
                "flow/unstaffed",
                "Unstaffed",
                new JsonObject { ["steps"] = Steps("s1") }),
            Record("flow", "flow/starts", "Starts"));

        var areas = BookProductAreas.Build(model);

        var lending = Assert.Single(areas.Modules.ToList());
        Assert.Equal("module/lending", lending.Id);
        Assert.Empty(lending.Features);
        Assert.Empty(lending.Flows);
        Assert.Equal(
            new[] { "feature/empty", "feature/lonely", "feature/ruled" },
            areas.Unfulfilled.Features.ToList());
        Assert.Equal(
            new[] { "flow/starts", "flow/unstaffed" },
            areas.Unfulfilled.Flows.ToList());
    }

    /// <summary>A screen counts toward the modules of the use cases it offers; a screen offering nothing, or nothing known, places its flow nowhere.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void ScreensCountTowardTheirOfferedUseCasesModules()
    {
        var model = Model(
            Record("product", "product/library", "Library"),
            Record("module", "module/lending", "Lending"),
            Record("use-case", "use-case/lending.borrow-copy", "Borrow Copy"),
            Record(
                "flow",
                "flow/borrowing",
                "Borrowing",
                new JsonObject { ["steps"] = Steps("s1") }),
            Record(
                "flow",
                "flow/kiosk",
                "Kiosk",
                new JsonObject { ["steps"] = Steps("s1") }),
            Record(
                "flow",
                "flow/deserted",
                "Deserted",
                new JsonObject { ["steps"] = Steps("s1") }),
            Record(
                "screen",
                "screen/library-app.search",
                "Search",
                new JsonObject
                {
                    ["offers"] = Ids("use-case/lending.borrow-copy"),
                    ["serves"] = Ids("flow/borrowing#s1"),
                }),
            Record(
                "screen",
                "screen/library-app.help",
                "Help",
                new JsonObject
                {
                    ["offers"] = Ids("use-case/missing.nothing"),
                    ["serves"] = Ids("flow/kiosk#s1"),
                }),
            Record(
                "screen",
                "screen/library-app.about",
                "About",
                new JsonObject { ["serves"] = Ids("flow/deserted#s1") }));

        var areas = BookProductAreas.Build(model);

        var lending = Assert.Single(areas.Modules.ToList());
        Assert.Equal(new[] { "flow/borrowing" }, lending.Flows.ToList());
        Assert.Equal(
            new[] { "flow/deserted", "flow/kiosk" },
            areas.Unfulfilled.Flows.ToList());
    }

    /// <summary>A record's module resolves from its stated module, then its file's module folder, then its identifier home; a serves target without a step names no flow.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void ModuleResolvesFromFieldFileAndHome()
    {
        var model = Model(
            Record("product", "product/library", "Library"),
            Record("module", "module/lending", "Lending"),
            Record("module", "module/copies", "Copies"),
            Record("feature", "feature/a", "A"),
            Record("requirement", "requirement/a.one", "One"),
            Record("feature", "feature/b", "B"),
            Record("requirement", "requirement/b.two", "Two"),
            Record("feature", "feature/c", "C"),
            Record("requirement", "requirement/c.three", "Three"),
            Record("feature", "feature/d", "D"),
            Record("requirement", "requirement/d.four", "Four"),
            Record(
                "use-case",
                "use-case/lending.field-wins",
                "Field Wins",
                new JsonObject { ["fulfills"] = Ids("requirement/a.one") },
                module: "copies"),
            Record(
                "use-case",
                "use-case/lending.file-wins",
                "File Wins",
                new JsonObject { ["fulfills"] = Ids("requirement/b.two") },
                file: ".litespec/behavior/modules/copies/use-cases/file-wins/file-wins.yaml"),
            Record(
                "use-case",
                "use-case/lending.home-wins",
                "Home Wins",
                new JsonObject { ["fulfills"] = Ids("requirement/c.three") }),
            Record(
                "use-case",
                "use-case/nope.ghost",
                "Ghost",
                new JsonObject { ["fulfills"] = Ids("requirement/d.four") },
                module: "nope"),
            Record(
                "flow",
                "flow/bare",
                "Bare",
                new JsonObject { ["steps"] = Steps("s1") }),
            Record(
                "use-case",
                "use-case/lending.bare-serves",
                "Bare Serves",
                new JsonObject { ["serves"] = Ids("flow/bare", "goal/faster-borrowing") }));

        var areas = BookProductAreas.Build(model);

        var copies = areas.Modules.Single(area => area.Id == "module/copies");
        Assert.Equal(new[] { "feature/a", "feature/b" }, copies.Features.ToList());
        var lending = areas.Modules.Single(area => area.Id == "module/lending");
        Assert.Equal(new[] { "feature/c" }, lending.Features.ToList());
        Assert.Equal(new[] { "feature/d" }, areas.Unfulfilled.Features.ToList());
        Assert.Equal(new[] { "flow/bare" }, areas.Unfulfilled.Flows.ToList());
    }

    /// <summary>The written book carries a product-areas shard whose every module, feature and flow identifier names a record of that type.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void WrittenBookCarriesAProductAreasShardOfKnownRecords()
    {
        using var temp = TestCli.Temp("book-areas");
        BookTestBooks.Write(BookTestBooks.LoadFixture("Book/review.json"), temp.Path, temp.Path);

        var data = Path.Combine(temp.Path, "data");
        var path = Path.Combine(data, "product-areas.js");
        Assert.True(File.Exists(path), "data/product-areas.js was not written.");
        var shard = ReadShard(path, "product-areas");
        var types = RecordTypesOf(data);

        var modules = shard["modules"]!.AsArray().OfType<JsonObject>().ToList();
        Assert.NotEmpty(modules);
        Assert.Contains(modules, module => module["id"]!.GetValue<string>() == "module/lending");
        foreach (var module in modules)
        {
            var id = module["id"]!.GetValue<string>();
            Assert.True(types.TryGetValue(id, out var type) && type == "module", $"'{id}' names no module record.");
            Assert.True(module["title"]!.GetValue<string>().Length > 0, $"'{id}' carries no title.");
            foreach (var feature in module["features"]!.AsArray())
            {
                var target = feature!.GetValue<string>();
                Assert.True(types.TryGetValue(target, out var found) && found == "feature", $"'{target}' names no feature record.");
            }

            foreach (var flow in module["flows"]!.AsArray())
            {
                var target = flow!.GetValue<string>();
                Assert.True(types.TryGetValue(target, out var found) && found == "flow", $"'{target}' names no flow record.");
            }
        }

        var unfulfilled = shard["unfulfilled"]!.AsObject();
        foreach (var feature in unfulfilled["features"]!.AsArray())
        {
            var target = feature!.GetValue<string>();
            Assert.True(types.TryGetValue(target, out var found) && found == "feature", $"'{target}' names no feature record.");
        }

        foreach (var flow in unfulfilled["flows"]!.AsArray())
        {
            var target = flow!.GetValue<string>();
            Assert.True(types.TryGetValue(target, out var found) && found == "flow", $"'{target}' names no flow record.");
        }
    }

    static JsonObject Record(
        string type,
        string id,
        string title = "",
        JsonObject? fields = null,
        string? file = null,
        string? module = null)
    {
        var record = new JsonObject
        {
            ["type"] = type,
            ["id"] = id,
            ["title"] = title.Length > 0 ? title : id,
        };

        if (file is not null)
        {
            record["file"] = file;
        }

        if (module is not null)
        {
            record["module"] = module;
        }

        record["fields"] = fields ?? new JsonObject();
        return record;
    }

    static JsonObject Model(params JsonObject[] records)
    {
        var array = new JsonArray();
        foreach (var record in records)
        {
            array.Add(record);
        }

        return new JsonObject { ["records"] = array };
    }

    static JsonArray Ids(params string[] ids)
    {
        var array = new JsonArray();
        foreach (var id in ids)
        {
            array.Add(id);
        }

        return array;
    }

    static JsonArray Steps(params string[] ids)
    {
        var array = new JsonArray();
        foreach (var id in ids)
        {
            array.Add(new JsonObject { ["id"] = id });
        }

        return array;
    }

    static JsonObject ReadShard(string path, string key)
    {
        var text = File.ReadAllText(path);
        var prefix = $"BookData.put(\"{key}\", ";
        Assert.StartsWith(prefix, text, StringComparison.Ordinal);
        Assert.EndsWith(");\n", text, StringComparison.Ordinal);
        return JsonNode.Parse(text[prefix.Length..^3])!.AsObject();
    }

    static Dictionary<string, string> RecordTypesOf(string dataDir)
    {
        var types = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(dataDir, "records-*.js").OrderBy(path => path, StringComparer.Ordinal))
        {
            var text = File.ReadAllText(file);
            var key = Path.GetFileNameWithoutExtension(file);
            var prefix = $"BookData.put(\"{key}\", ";
            var pack = JsonNode.Parse(text[prefix.Length..^3])!.AsObject();
            foreach (var pair in pack)
            {
                types[pair.Key] = pair.Value!.AsObject()["type"]!.GetValue<string>();
            }
        }

        return types;
    }
}
