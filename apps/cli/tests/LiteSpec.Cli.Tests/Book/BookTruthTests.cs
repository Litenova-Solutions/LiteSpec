using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Book;

/// <summary>Proves the book says only true things about proof, steps and checks: never-run proof, reaction serving, walked steps and embedded findings.</summary>
public sealed class BookTruthTests
{
    /// <summary>A model with no test run carries a null run and the not-run proof state on every scenario and rule, with machine truth kept.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void BookWithNoTestRunSaysNotRun()
    {
        using var temp = TestCli.Temp("book-truth");
        var model = WriteReviewModel(root => root["testRun"] = null);
        BookTestBooks.Write(model, temp.Path, Out(temp));

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

        // Proof links still count; only the run outcome is unknown.
        Assert.Equal(1, Obj(details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"], "proof")["tests"]?.GetValue<int>());

        var borrow = details["use-case/lending.borrow-copy"];
        Assert.True(BoolOf(Obj(borrow, "machine")["coded"]));
        Assert.False(BoolOf(Obj(borrow, "machine")["verified"]));
        Assert.Null(borrow["proof"]);
    }

    /// <summary>A flow step served only by a reaction lists the reaction as its server and no screen.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void FlowStepServedOnlyByAReactionReadsAsReaction()
    {
        using var temp = TestCli.Temp("book-truth");
        var reactionId = "";
        var model = WriteReviewModel(root =>
        {
            var reaction = Records(root).First(record => Text(record["type"]) == "reaction");
            reactionId = Text(reaction["id"]);
            ((JsonObject)reaction["fields"]!)["serves"] = new JsonArray("step/borrowing.return-copy");
            ((JsonArray)root["records"]!).Add(new JsonObject
            {
                ["type"] = "step",
                ["id"] = "step/borrowing.return-copy",
                ["title"] = "The member returns the copy.",
                ["owner"] = "lending-team",
                ["status"] = "approved",
                ["effectiveStatus"] = "approved",
                ["decider"] = null,
                ["decideBy"] = null,
                ["file"] = ".litespec/product/flows/borrowing.yaml",
                ["line"] = 10,
                ["fields"] = new JsonObject
                {
                    ["id"] = "step/borrowing.return-copy",
                    ["statement"] = "The member returns the copy.",
                },
                ["links"] = new JsonObject(),
            });
        });
        Assert.True(reactionId.Length > 0, "The fixture holds no reaction.");
        BookTestBooks.Write(model, temp.Path, Out(temp));

        var records = ReadRecords(Path.Combine(Out(temp), "data"));
        var flow = records["flow/borrowing"];
        var servers = Obj(flow, "stepServers");
        Assert.True(HasString(servers["step/borrowing.borrow-copy"] as JsonArray ?? new JsonArray(), "use-case/lending.borrow-copy"));
        Assert.True(HasString(servers["step/borrowing.return-copy"] as JsonArray ?? new JsonArray(), reactionId));
        Assert.Empty(Obj(flow, "stepScreens")["step/borrowing.return-copy"] as JsonArray ?? new JsonArray());
        Assert.True(HasEdge(Arr(records["step/borrowing.return-copy"], "in"), reactionId, "reaction.serves"));
    }

    /// <summary>A flow step a passing test walked lists as exercised; the step no passing test walked does not.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void ExercisedStepRendersAsWalked()
    {
        using var temp = TestCli.Temp("book-truth");
        var model = WriteReviewModel(root => root["exercisedSteps"] = new JsonArray("step/borrowing.borrow-copy"));
        BookTestBooks.Write(model, temp.Path, Out(temp));

        var flow = ReadRecords(Path.Combine(Out(temp), "data"))["flow/borrowing"];
        var exercised = Arr(flow, "exercised");
        Assert.True(HasString(exercised, "step/borrowing.borrow-copy"));
        Assert.False(HasString(exercised, "step/borrowing.return-copy"));
    }

    /// <summary>Findings embedded in the model reach their subject records, open and known apart.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void EmbeddedFindingsReachTheirRecords()
    {
        using var temp = TestCli.Temp("book-truth");
        var model = ModelWithEmbeddedFindings();
        BookTestBooks.Write(model, temp.Path, Out(temp));

        var details = ReadRecords(Path.Combine(Out(temp), "data"));
        var open = Arr(details["use-case/lending.return-copy"], "findings");
        Assert.Single(open);
        var finding = open[0]!.AsObject();
        Assert.Equal("use-case-unimplemented", Str(finding, "code"));
        Assert.Equal("G", Str(finding, "tier"));
        Assert.Contains("has no handler, reaction or job", Str(finding, "message") ?? "", StringComparison.Ordinal);
        Assert.False(BoolOf(finding["covered"]));
        Assert.Equal(".litespec/behavior/modules/lending/aggregates/loans/return-copy.yaml", Str(finding, "path"));
        Assert.Equal(3, finding["line"]?.GetValue<int>());

        var known = Arr(details["flow/borrowing"], "findings");
        Assert.Single(known);
        Assert.Equal("step-screen-missing", Str(known[0]!.AsObject(), "code"));
        Assert.True(BoolOf(known[0]!.AsObject()["covered"]));
        Assert.Equal("accepted", Str(known[0]!.AsObject(), "outcome"));
        Assert.Equal("The kiosk covers it next quarter.", Str(known[0]!.AsObject(), "reason"));
    }

    /// <summary>The review fixture with one open and one known finding on records the fixture holds.</summary>
    /// <returns>The parsed review model with the embedded findings.</returns>
    internal static JsonObject ModelWithEmbeddedFindings() =>
        WriteReviewModel(root =>
        {
            root["findings"] = new JsonArray(
                new JsonObject
                {
                    ["code"] = "use-case-unimplemented",
                    ["tier"] = "G",
                    ["message"] = "Use case 'use-case/lending.return-copy' has no handler, reaction or job.",
                    ["subject"] = "use-case/lending.return-copy",
                    ["path"] = ".litespec/behavior/modules/lending/aggregates/loans/return-copy.yaml",
                    ["line"] = 3,
                });
            root["covered"] = new JsonArray(
                new JsonObject
                {
                    ["code"] = "step-screen-missing",
                    ["tier"] = "A",
                    ["message"] = "Step s2 has no screen.",
                    ["subject"] = "flow/borrowing",
                    ["outcome"] = "accepted",
                    ["reason"] = "The kiosk covers it next quarter.",
                });
        });

    static JsonObject WriteReviewModel(Action<JsonObject> mutate)
    {
        var root = (JsonObject)RecordText.Read(TestCli.Fixture("Book/review.json"))!;
        mutate(root);
        return root;
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
