using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Book;

/// <summary>Proves the book says only true things about proof, steps and checks: never-run proof, automation serving, walked steps and embedded findings.</summary>
public sealed class BookTruthTests
{
    /// <summary>A model with no test run carries a null run and the not-run proof state on every scenario and rule, with machine truth kept.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Book_With_No_Test_Run_Says_Not_Run()
    {
        using var temp = Cli.Temp("book-truth");
        var model = WriteReviewModel(temp, root => root["testRun"] = null);
        var result = Cli.Run("book", "--model", model, "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var data = Path.Combine(Out(temp), "data");
        var manifest = ReadShard(Path.Combine(data, "manifest.js"), "manifest");
        Assert.Null(manifest["testRun"]);

        var details = ReadRecords(data);
        foreach (var id in new[]
        {
            "scenario/borrow-copy.borrowing-an-available-copy-succeeds",
            "scenario/borrow-copy.borrowing-a-loaned-copy-is-refused",
            "rule/loan.one-loan-per-copy",
            "rule/loan.late-fee",
        })
        {
            Assert.Equal("norun", Str(Obj(details[id], "proof"), "state"));
        }

        // Proof links still count; only the run verdict is unknown.
        Assert.Equal(1, Obj(details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"], "proof")["tests"]?.GetValue<int>());

        var borrow = details["use-case/lending.borrow-copy"];
        Assert.True(BoolOf(Obj(borrow, "machine")["implemented"]));
        Assert.False(BoolOf(Obj(borrow, "machine")["verified"]));
        Assert.Null(borrow["proof"]);
    }

    /// <summary>A journey step served only by an automation lists the automation as its server and no screen.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Journey_Step_Served_Only_By_An_Automation_Reads_As_Automation()
    {
        using var temp = Cli.Temp("book-truth");
        var automationId = "";
        var model = WriteReviewModel(temp, root =>
        {
            var automation = Records(root).First(record => Text(record["type"]) == "automation");
            automationId = Text(automation["id"]);
            ((JsonObject)automation["fields"]!)["serves"] = new JsonArray("journey/borrowing#s2");
        });
        Assert.True(automationId.Length > 0, "The fixture holds no automation.");
        var result = Cli.Run("book", "--model", model, "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var journey = ReadRecords(Path.Combine(Out(temp), "data"))["journey/borrowing"];
        var servers = Obj(journey, "stepServers");
        Assert.True(HasString(servers["s1"] as JsonArray ?? new JsonArray(), "use-case/lending.borrow-copy"));
        Assert.True(HasString(servers["s2"] as JsonArray ?? new JsonArray(), automationId));
        Assert.Empty(Obj(journey, "stepScreens")["s2"] as JsonArray ?? new JsonArray());
        Assert.True(HasEdge(Arr(journey, "in"), automationId, "automation.serves"));
    }

    /// <summary>A journey step a passing test walked lists as exercised; the step no passing test walked does not.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Exercised_Step_Renders_As_Walked()
    {
        using var temp = Cli.Temp("book-truth");
        var model = WriteReviewModel(temp, root => root["exercisedSteps"] = new JsonArray("journey/borrowing#s1"));
        var result = Cli.Run("book", "--model", model, "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var journey = ReadRecords(Path.Combine(Out(temp), "data"))["journey/borrowing"];
        var exercised = Arr(journey, "exercised");
        Assert.True(HasString(exercised, "s1"));
        Assert.False(HasString(exercised, "s2"));
    }

    /// <summary>Findings embedded in the model reach their subject records, open and known apart.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Embedded_Findings_Reach_Their_Records()
    {
        using var temp = Cli.Temp("book-truth");
        var model = ModelWithEmbeddedFindings(temp);
        var result = Cli.Run("book", "--model", model, "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var details = ReadRecords(Path.Combine(Out(temp), "data"));
        var open = Arr(details["use-case/lending.return-copy"], "findings");
        Assert.Single(open);
        var finding = open[0]!.AsObject();
        Assert.Equal("use-case-unimplemented", Str(finding, "code"));
        Assert.Equal("G", Str(finding, "tier"));
        Assert.Contains("has no handler or automation", Str(finding, "message") ?? "", StringComparison.Ordinal);
        Assert.False(BoolOf(finding["known"]));
        Assert.Equal("docs/behavior/modules/lending/loans/return-copy.md", Str(finding, "path"));
        Assert.Equal(3, finding["line"]?.GetValue<int>());

        var known = Arr(details["journey/borrowing"], "findings");
        Assert.Single(known);
        Assert.Equal("step-screen-missing", Str(known[0]!.AsObject(), "code"));
        Assert.True(BoolOf(known[0]!.AsObject()["known"]));
        Assert.Equal("accepted", Str(known[0]!.AsObject(), "outcome"));
        Assert.Equal("The kiosk covers it next quarter.", Str(known[0]!.AsObject(), "reason"));
    }

    /// <summary>The review fixture with one open and one known finding on records the fixture holds.</summary>
    internal static string ModelWithEmbeddedFindings(TempFolder temp) =>
        WriteReviewModel(temp, root =>
        {
            root["findings"] = new JsonArray(
                new JsonObject
                {
                    ["code"] = "use-case-unimplemented",
                    ["tier"] = "G",
                    ["message"] = "Use case 'use-case/lending.return-copy' has no handler or automation.",
                    ["subject"] = "use-case/lending.return-copy",
                    ["path"] = "docs/behavior/modules/lending/loans/return-copy.md",
                    ["line"] = 3,
                });
            root["known"] = new JsonArray(
                new JsonObject
                {
                    ["code"] = "step-screen-missing",
                    ["tier"] = "A",
                    ["message"] = "Step s2 has no screen.",
                    ["subject"] = "journey/borrowing",
                    ["outcome"] = "accepted",
                    ["reason"] = "The kiosk covers it next quarter.",
                });
        });

    static string WriteReviewModel(TempFolder temp, Action<JsonObject> mutate)
    {
        var root = (JsonObject)JsonNode.Parse(File.ReadAllText(Cli.Fixture("Book/review.json")))!;
        mutate(root);
        var path = Path.Combine(temp.Path, "model.json");
        File.WriteAllText(path, root.ToJsonString());
        return path;
    }

    static IEnumerable<JsonObject> Records(JsonObject root) =>
        ((JsonArray)root["records"]!).OfType<JsonObject>();

    static string Text(JsonNode? node) => node?.GetValue<string>() ?? "";

    static string Out(TempFolder temp) => Path.Combine(temp.Path, "out");

    static JsonObject ReadShard(string path, string key)
    {
        var text = File.ReadAllText(path);
        var prefix = $"BookData.put(\"{key}\", ";
        Assert.StartsWith(prefix, text, StringComparison.Ordinal);
        Assert.EndsWith(");\n", text, StringComparison.Ordinal);
        return JsonNode.Parse(text[prefix.Length..^3])!.AsObject();
    }

    static Dictionary<string, JsonObject> ReadRecords(string dataDir)
    {
        var details = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(dataDir, "records-*.js").OrderBy(path => path, StringComparer.Ordinal))
        {
            var shard = ReadShard(file, Path.GetFileNameWithoutExtension(file));
            foreach (var pair in shard)
            {
                details[pair.Key] = pair.Value!.AsObject();
            }
        }

        return details;
    }

    static string? Str(JsonObject? obj, string name) =>
        obj?[name] is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? text : null;

    static bool BoolOf(JsonNode? node) =>
        node is JsonValue scalar && scalar.TryGetValue<bool>(out var flag) && flag;

    static JsonObject Obj(JsonObject obj, string name) => obj[name] as JsonObject ?? new JsonObject();

    static JsonArray Arr(JsonObject obj, string name) => obj[name] as JsonArray ?? new JsonArray();

    static bool HasString(JsonArray list, string value)
    {
        foreach (var item in list)
        {
            if (item is JsonValue scalar && scalar.TryGetValue<string>(out var text) && text == value)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasEdge(JsonArray edges, string id, string key)
    {
        foreach (var edge in edges.OfType<JsonObject>())
        {
            if (Str(edge, "id") == id && Str(edge, "key") == key)
            {
                return true;
            }
        }

        return false;
    }
}
