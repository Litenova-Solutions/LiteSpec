using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.CompletionStages;
using LiteSpec.Cli.Tests.Model.Checks;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves parked requirements stay out of the stage and visible with their waiver and expiry.</summary>
/// <follows>litespec/core/model#compute-completion-in-the-builder</follows>
public sealed class CompletionParkedTests
{
    /// <summary>Fourteen requirements with two parked read twelve in scope, two parked, fourteen total, with held-by free of parked ids.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void FourteenRequirementsWithTwoParkedReadTwelveInScope()
    {
        using var temp = TestCli.Temp("completion");
        var records = new List<LiteSpec.Cli.Model.Records.Record> { Feature(temp.Path) };

        for (var index = 1; index <= 14; index++)
        {
            records.Add(Requirement(temp.Path, $"requirement/borrowing.req-{index:00}"));
        }

        records.Add(Waiver(temp.Path, "waiver/borrowing.req-13-gap", "requirement/borrowing.req-13", "2099-01-01"));
        records.Add(Waiver(temp.Path, "waiver/borrowing.req-14-gap", "requirement/borrowing.req-14", "2099-06-01"));

        var model = TestModel.Model(TestModel.Config(temp.Path), [.. records]);
        CompletionCalculator.Compute(model);

        var completion = model.Completion["feature/borrowing"];

        Assert.Equal("unfulfilled", completion.Stage);
        Assert.Equal(12, completion.InScope);
        Assert.Equal(2, completion.Parked);
        Assert.Equal(14, completion.Total);
        Assert.Equal(10, completion.HeldBy.Count);
        Assert.DoesNotContain("requirement/borrowing.req-13", completion.HeldBy);
        Assert.DoesNotContain("requirement/borrowing.req-14", completion.HeldBy);
        Assert.Equal(10, completion.Counts["held by"].Done);
        Assert.Equal(12, completion.Counts["held by"].Of);

        var parked = completion.ParkedBy;

        Assert.Equal(2, parked.Count);
        Assert.Equal("requirement/borrowing.req-13", parked[0].Id);
        Assert.Equal("waiver/borrowing.req-13-gap", parked[0].Waiver);
        Assert.Equal("2099-01-01", parked[0].ExpiresOn);
        Assert.Equal("requirement/borrowing.req-14", parked[1].Id);
        Assert.Equal("waiver/borrowing.req-14-gap", parked[1].Waiver);
        Assert.Equal("2099-06-01", parked[1].ExpiresOn);

        using var json = JsonDocument.Parse(model.ToJson());
        var stored = CompletionOf(json.RootElement, "feature/borrowing");

        Assert.Equal(12, stored.GetProperty("inScope").GetInt32());
        Assert.Equal(2, stored.GetProperty("parked").GetInt32());
        Assert.Equal(14, stored.GetProperty("total").GetInt32());

        var held = stored.GetProperty("heldBy").EnumerateArray().Select(entry => entry.GetString()).ToList();

        Assert.Equal(10, held.Count);
        Assert.DoesNotContain("requirement/borrowing.req-13", held);
        Assert.DoesNotContain("requirement/borrowing.req-14", held);

        var storedParked = stored.GetProperty("parkedBy").EnumerateArray().ToList();

        Assert.Equal(2, storedParked.Count);
        Assert.Equal("requirement/borrowing.req-13", storedParked[0].GetProperty("id").GetString());
        Assert.Equal("waiver/borrowing.req-13-gap", storedParked[0].GetProperty("waiver").GetString());
        Assert.Equal("2099-01-01", storedParked[0].GetProperty("expiresOn").GetString());
        Assert.Equal("requirement/borrowing.req-14", storedParked[1].GetProperty("id").GetString());
        Assert.Equal("waiver/borrowing.req-14-gap", storedParked[1].GetProperty("waiver").GetString());
        Assert.Equal("2099-06-01", storedParked[1].GetProperty("expiresOn").GetString());

        var answer = RecordQuery.Query(model, "feature/borrowing");

        Assert.NotNull(answer);

        using var queried = JsonDocument.Parse(answer);
        var shown = queried.RootElement.GetProperty("computed").GetProperty("completion");

        Assert.Equal(12, shown.GetProperty("inScope").GetInt32());
        Assert.Equal(2, shown.GetProperty("parked").GetInt32());
        Assert.Equal(14, shown.GetProperty("total").GetInt32());
        Assert.Equal(2, shown.GetProperty("parkedBy").EnumerateArray().Count());
    }

    /// <summary>An expired waiver parks nothing: its subject stays in scope and holds the stage.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void ExpiredWaiverParksNothing()
    {
        using var temp = TestCli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Feature(temp.Path),
            Requirement(temp.Path, "requirement/borrowing.max-loans"),
            Waiver(temp.Path, "waiver/borrowing.max-loans-gap", "requirement/borrowing.max-loans", "2000-01-01"));

        CompletionCalculator.Compute(model);

        var completion = model.Completion["feature/borrowing"];

        Assert.Equal(1, completion.InScope);
        Assert.Equal(0, completion.Parked);
        Assert.Equal(1, completion.Total);
        Assert.Empty(completion.ParkedBy);
        Assert.Equal(["requirement/borrowing.max-loans"], completion.HeldBy);
    }

    /// <summary>A lasting waiver parks with no expiry, and a parent with every child parked reads unfulfilled.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void LastingWaiverParksWithNoExpiry()
    {
        using var temp = TestCli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Feature(temp.Path),
            Requirement(temp.Path, "requirement/borrowing.max-loans"),
            TestModel.Record(
                "waiver",
                "waiver/borrowing.max-loans-standing",
                Path.Combine(temp.Path, "standing.yaml"),
                """{"kind": "lasting", "appliesTo": ["requirement/borrowing.max-loans"], "waives": ["requirement-fulfillment-missing"], "reason": "The check never ships.", "consequences": "Loans run without the check."}"""));

        CompletionCalculator.Compute(model);

        var completion = model.Completion["feature/borrowing"];

        Assert.Equal("unfulfilled", completion.Stage);
        Assert.Equal(["feature/borrowing"], completion.HeldBy);
        Assert.Equal(0, completion.InScope);
        Assert.Equal(1, completion.Parked);
        Assert.Equal(1, completion.Total);

        var parked = Assert.Single(completion.ParkedBy);

        Assert.Equal("requirement/borrowing.max-loans", parked.Id);
        Assert.Equal("waiver/borrowing.max-loans-standing", parked.Waiver);
        Assert.Null(parked.ExpiresOn);

        using var json = JsonDocument.Parse(model.ToJson());
        var storedParked = CompletionOf(json.RootElement, "feature/borrowing").GetProperty("parkedBy").EnumerateArray().ToList();
        var entry = Assert.Single(storedParked);

        Assert.Equal(JsonValueKind.Null, entry.GetProperty("expiresOn").ValueKind);
    }

    /// <summary>A use case holds a parked scenario out of its verified count.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void UseCaseWithParkedScenarioExcludesItFromCounts()
    {
        using var temp = TestCli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow-copy.yaml"), """{"kind": "command"}"""),
            TestModel.Record("scenario", "scenario/borrow-copy.available-succeeds", Path.Combine(temp.Path, "first.yaml"), """{"result": "result/borrow-copy.borrowed"}"""),
            TestModel.Record("scenario", "scenario/borrow-copy.reserved-waits", Path.Combine(temp.Path, "second.yaml"), """{"result": "result/borrow-copy.borrowed"}"""),
            Waiver(temp.Path, "waiver/borrow-copy.reserved-gap", "scenario/borrow-copy.reserved-waits", "2099-01-01"));

        CompletionCalculator.Compute(model);

        var completion = model.Completion["use-case/lending.borrow-copy"];

        Assert.Equal("specified", completion.Stage);
        Assert.Equal(1, completion.InScope);
        Assert.Equal(1, completion.Parked);
        Assert.Equal(2, completion.Total);
        Assert.Equal(0, completion.Counts["scenarios verified"].Done);
        Assert.Equal(1, completion.Counts["scenarios verified"].Of);

        var parked = Assert.Single(completion.ParkedBy);

        Assert.Equal("scenario/borrow-copy.reserved-waits", parked.Id);
    }

    static JsonElement CompletionOf(JsonElement root, string id) =>
        root.GetProperty("records").EnumerateArray()
            .Single(record => record.GetProperty("id").GetString() == id)
            .GetProperty("completion");

    static LiteSpec.Cli.Model.Records.Record Requirement(string root, string id) =>
        TestModel.Record(
            "requirement",
            id,
            Path.Combine(root, id.Replace('/', '.') + ".yaml"),
            """{"kind": "functional", "statement": "A reader borrows one copy.", "basedOn": ["feature/borrowing"]}""");

    static LiteSpec.Cli.Model.Records.Record Feature(string root) =>
        TestModel.Record("feature", "feature/borrowing", Path.Combine(root, "feature.yaml"), """{"advances": ["goal/borrow"]}""");

    static LiteSpec.Cli.Model.Records.Record Waiver(string root, string id, string subject, string expiresOn) =>
        TestModel.Record(
            "waiver",
            id,
            Path.Combine(root, id.Replace('/', '.') + ".yaml"),
            $$"""{"kind": "gap", "appliesTo": ["{{subject}}"], "waives": ["requirement-fulfillment-missing"], "reason": "The work ships later.", "closedBy": ["{{subject}}"], "exit": "The work ships.", "expiresOn": "{{expiresOn}}"}""");
}
