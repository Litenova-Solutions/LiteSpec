using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Book;

/// <summary>Proves the queue shard groups drafts and reopened records by review batch.</summary>
/// <follows>litespec/core/release#compute-the-review-queue-from-the-model</follows>
public sealed class BookReviewBatchesTests
{
    const string BorrowCopyUseCase = @"{""type"":""use-case"",""id"":""use-case/lending.borrow-copy"",""title"":""Borrow Copy"",""owner"":""Ada"",""status"":""draft"",""effectiveStatus"":""draft"",""decider"":""Ada"",""decideBy"":""2026-11-20"",""file"":""docs/behavior/modules/lending/loans/borrow-copy.md"",""line"":3,""fields"":{""risk"":[""money""]},""links"":{}}";
    const string BorrowedResult = @"{""type"":""result"",""id"":""result/borrow-copy.borrowed"",""title"":""Borrowed"",""owner"":""Ada"",""status"":""draft"",""effectiveStatus"":""draft"",""decider"":""Ada"",""decideBy"":""2026-11-15"",""file"":""docs/behavior/modules/lending/loans/borrow-copy.md"",""line"":40,""fields"":{},""links"":{}}";
    const string BorrowingScenario = @"{""type"":""scenario"",""id"":""scenario/borrow-copy.borrowing-succeeds"",""title"":""Borrowing Succeeds"",""owner"":""Ada"",""status"":""draft"",""effectiveStatus"":""draft"",""decider"":null,""decideBy"":null,""file"":""docs/behavior/modules/lending/loans/borrow-copy.md"",""line"":60,""fields"":{},""links"":{}}";

    /// <summary>Drafts of one use case group under one batch with its tier, deciders, decide-by date and record order.</summary>
    /// <proves>litespec/core/release#compute-the-review-queue-from-the-model</proves>
    [Fact]
    public void Batch_Groups_Use_Case_Drafts_Under_One_Selector()
    {
        using var temp = Cli.Temp("book-batches");
        var model = WriteModel(temp, string.Join(",", BorrowCopyUseCase, BorrowedResult, BorrowingScenario), "[]", "[]");
        var result = Cli.Run("book", "--model", model, "--out", Out(temp));
        Assert.Equal(0, result.Status);
        var queue = ReadShard(Path.Combine(Out(temp), "data", "queue.js"), "queue");
        var order = queue["order"]!.AsArray().Select(node => node!.GetValue<string>()).ToList();
        Assert.Equal(new[] { "use-case/lending.borrow-copy" }, order);
        var batch = queue["batches"]!.AsObject()["use-case/lending.borrow-copy"]!.AsObject();
        Assert.Equal("use-case/lending.borrow-copy", Str(batch, "selector"));
        Assert.Equal("use-case", Str(batch, "kind"));
        Assert.Equal(1, batch["tier"]?.GetValue<int>());
        Assert.Equal(3, batch["n"]?.GetValue<int>());
        Assert.Equal("2026-11-15", Str(batch, "decideBy"));
        Assert.Equal(new[] { "Ada" }, batch["deciders"]!.AsArray().Select(node => node!.GetValue<string>()).ToList());
        Assert.Equal(0, batch["awaiting"]?.GetValue<int>());
        Assert.Equal(
            new[] { "use-case/lending.borrow-copy", "result/borrow-copy.borrowed", "scenario/borrow-copy.borrowing-succeeds" },
            batch["records"]!.AsArray().Select(node => node!.GetValue<string>()).ToList());
    }

    /// <summary>Two batches order in queue order: Intent before Behavior whatever the dates say.</summary>
    /// <proves>litespec/core/release#compute-the-review-queue-from-the-model</proves>
    [Fact]
    public void Batches_Render_In_Queue_Order()
    {
        using var temp = Cli.Temp("book-batches");
        var feature = @"{""type"":""feature"",""id"":""feature/self-checkout"",""title"":""Self Checkout"",""owner"":""Ada"",""status"":""draft"",""effectiveStatus"":""draft"",""decider"":""Ada"",""decideBy"":""2026-12-31"",""file"":""docs/product/features/self-checkout.md"",""line"":3,""fields"":{},""links"":{}}";
        var useCase = @"{""type"":""use-case"",""id"":""use-case/lending.return-copy"",""title"":""Return Copy"",""owner"":""Ada"",""status"":""draft"",""effectiveStatus"":""draft"",""decider"":""Ada"",""decideBy"":""2026-11-01"",""file"":""docs/behavior/modules/lending/loans/return-copy.md"",""line"":3,""fields"":{},""links"":{}}";
        var model = WriteModel(temp, string.Join(",", feature, useCase), "[]", "[]");
        var result = Cli.Run("book", "--model", model, "--out", Out(temp));
        Assert.Equal(0, result.Status);
        var queue = ReadShard(Path.Combine(Out(temp), "data", "queue.js"), "queue");
        var order = queue["order"]!.AsArray().Select(node => node!.GetValue<string>()).ToList();
        Assert.Equal(new[] { "intent/feature", "use-case/lending.return-copy" }, order);
        Assert.Equal("module-type", Str(queue["batches"]!.AsObject()["intent/feature"]!.AsObject(), "kind"));
        Assert.Equal("use-case", Str(queue["batches"]!.AsObject()["use-case/lending.return-copy"]!.AsObject(), "kind"));
    }

    /// <summary>Every draft sits in exactly one batch; the queue carries no flat drafts list.</summary>
    /// <proves>litespec/core/release#compute-the-review-queue-from-the-model</proves>
    [Fact]
    public void Drafts_Group_Into_Batches_With_No_Flat_List()
    {
        using var temp = Cli.Temp("book-batches");
        var model = WriteModel(temp, string.Join(",", BorrowCopyUseCase, BorrowedResult, BorrowingScenario), "[]", "[]");
        var result = Cli.Run("book", "--model", model, "--out", Out(temp));
        Assert.Equal(0, result.Status);
        var queue = ReadShard(Path.Combine(Out(temp), "data", "queue.js"), "queue");
        Assert.Null(queue["drafts"]);
        var grouped = new List<string>();
        foreach (var batch in queue["batches"]!.AsObject())
        {
            grouped.AddRange(batch.Value!.AsObject()["records"]!.AsArray().Select(node => node!.GetValue<string>()));
        }

        Assert.Equal(
            new[] { "result/borrow-copy.borrowed", "scenario/borrow-copy.borrowing-succeeds", "use-case/lending.borrow-copy" },
            grouped.OrderBy(id => id, StringComparer.Ordinal).ToList());
    }

    /// <summary>A reopened full re-read sorts first, and the batch records carry their questions and hashes.</summary>
    /// <proves>litespec/core/release#compute-the-review-queue-from-the-model</proves>
    [Fact]
    public void Reopened_Full_Re_Read_Sorts_First_With_Questions_Carried()
    {
        using var temp = Cli.Temp("book-batches");
        var requirement = @"{""type"":""requirement"",""id"":""requirement/borrowing.late-fee"",""title"":""Late Fee"",""owner"":""Ada"",""status"":""approved"",""effectiveStatus"":""approved"",""decider"":""Ada"",""decideBy"":null,""file"":""docs/product/requirements/late-fee.md"",""line"":3,""fields"":{""approvedHash"":""stale"",""risk"":[""money""]},""links"":{}}";
        var asking = BorrowCopyUseCase.Replace(
            @"""fields"":{""risk"":[""money""]}",
            @"""fields"":{""questions"":[{""question"":""Who approves shared copies?"",""decider"":""Ada"",""decideBy"":""2999-01-01""}]}",
            StringComparison.Ordinal);
        var proves = @"[{""scenario"":""Library.Tests.BorrowTests.Borrowing"",""claim"":""scenario/borrow-copy.borrowing-succeeds"",""confirmed"":true,""passing"":true,""indirect"":false}]";
        var model = WriteModel(
            temp,
            string.Join(",", requirement, asking, BorrowingScenario),
            proves,
            @"[""scenario/borrow-copy.borrowing-succeeds""]");
        var result = Cli.Run("book", "--model", model, "--out", Out(temp));
        Assert.Equal(0, result.Status);
        var data = Path.Combine(Out(temp), "data");
        var queue = ReadShard(Path.Combine(data, "queue.js"), "queue");
        var order = queue["order"]!.AsArray().Select(node => node!.GetValue<string>()).ToList();
        Assert.Equal(new[] { "requirement/borrowing.late-fee", "use-case/lending.borrow-copy" }, order);
        var first = queue["batches"]!.AsObject()["requirement/borrowing.late-fee"]!.AsObject();
        Assert.Equal("record", Str(first, "kind"));
        Assert.Equal(1, first["tier"]?.GetValue<int>());
        Assert.Equal(1, first["n"]?.GetValue<int>());
        var second = queue["batches"]!.AsObject()["use-case/lending.borrow-copy"]!.AsObject();
        Assert.Equal("use-case", Str(second, "kind"));
        Assert.Equal(3, second["tier"]?.GetValue<int>());
        Assert.Equal(2, second["n"]?.GetValue<int>());

        var details = ReadRecords(data);
        Assert.Equal("stale", Str(Obj(details["requirement/borrowing.late-fee"], "fields"), "approvedHash"));
        var questions = Arr(Obj(details["use-case/lending.borrow-copy"], "fields"), "questions");
        Assert.Single(questions);
        Assert.Equal("Who approves shared copies?", Str(questions[0]!.AsObject(), "question"));
    }

    static string WriteModel(TempFolder temp, string records, string proves, string stale)
    {
        var path = Path.Combine(temp.Path, "model.json");
        File.WriteAllText(
            path,
            "{\"records\":[" + records + "],"
                + "\"links\":{\"proves\":" + proves + "},"
                + "\"status\":{},\"requirementStatus\":{},\"reachableStates\":{},\"stale\":" + stale + "}");
        return path;
    }

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

    static JsonObject Obj(JsonObject obj, string name) => obj[name] as JsonObject ?? new JsonObject();

    static JsonArray Arr(JsonObject obj, string name) => obj[name] as JsonArray ?? new JsonArray();
}
