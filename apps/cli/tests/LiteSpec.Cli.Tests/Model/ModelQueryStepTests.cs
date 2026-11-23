using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Links;
using LiteSpec.Cli.Tests.Support;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>model query</c> on a step id returns the step with its trace, and that search finds steps.</summary>
public sealed class ModelQueryStepTests
{
    /// <summary>A step query returns the step with its branches, servers, carried requirements with fulfillers, and the code and tests behind them.</summary>
    /// <proves>litespec/core/agent#select-task-context</proves>
    [Fact]
    public void StepQueryReturnsTheStepWithItsTrace()
    {
        using var temp = TestCli.Temp("query-step");
        Directory.CreateDirectory(Path.Combine(temp.Path, ".litespec"));
        File.WriteAllText(Path.Combine(temp.Path, ".litespec", "project.yaml"), "{}");

        var model = new ProjectModel { Config = ConsumerConfig.Load(temp.Path) };
        model.Records.Add(Rec(
            "flow", "flow/borrow-and-return", Path.Combine(temp.Path, "borrow-and-return.yaml"),
            """{"title": "Borrow and return", "steps": [{"id": "step/borrow-and-return.borrow-copy", "statement": "The reader borrows the copy."}]}"""));
        model.Records.Add(Rec(
            "step", "step/borrow-and-return.borrow-copy", Path.Combine(temp.Path, "borrow-and-return.yaml"),
            """{"statement": "The reader borrows the copy.", "failure": "The copy is lent out, so the reader places a hold instead.", "carries": ["requirement/borrowing.max-active-loans"], "branches": [{"when": "The copy is lent out.", "then": "step/borrow-and-return.place-hold"}]}"""));
        model.Records.Add(Rec(
            "step", "step/borrow-and-return.place-hold", Path.Combine(temp.Path, "borrow-and-return.yaml"),
            """{"statement": "The reader places a hold on a lent copy."}"""));
        model.Records.Add(Rec(
            "use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"),
            """{"kind": "command", "serves": ["step/borrow-and-return.borrow-copy"], "fulfills": ["requirement/borrowing.max-active-loans"]}"""));
        model.Records.Add(Rec(
            "screen", "screen/library-app.loans", Path.Combine(temp.Path, "loans.yaml"),
            """{"route": "/loans", "pageFrame": {"pattern": "list-page", "option": "search-and-a-state-filter-only"}, "serves": ["step/borrow-and-return.borrow-copy"]}"""));
        model.Records.Add(Rec(
            "requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "max-active-loans.yaml"),
            """{"statement": "A reader holds at most five active loans."}"""));
        model.Implements.Add(new ImplementsLink("src/Lending/BorrowCopyHandler.cs", "use-case/lending.borrow-copy", "handler"));
        model.Proves.Add(new ProvesLink("BorrowCopyTests.Borrowing_An_Available_Copy_Succeeds", "scenario/borrow-copy.borrowing-an-available-copy-succeeds", true, true, false));

        var output = RecordQuery.Query(model, "step/borrow-and-return.borrow-copy");

        Assert.NotNull(output);
        using var parsed = JsonDocument.Parse(output);
        var root = parsed.RootElement;
        Assert.Equal("step", root.GetProperty("type").GetString());
        Assert.Equal("The reader borrows the copy.", root.GetProperty("fields").GetProperty("statement").GetString());
        Assert.Equal("step/borrow-and-return.place-hold", root.GetProperty("fields").GetProperty("branches")[0].GetProperty("then").GetString());

        var trace = root.GetProperty("computed").GetProperty("trace");
        Assert.Equal(
            ["screen/library-app.loans", "use-case/lending.borrow-copy"],
            trace.GetProperty("servedBy").EnumerateArray().Select(entry => entry.GetString()).ToList());

        var carried = Assert.Single(trace.GetProperty("carried").EnumerateArray());
        Assert.Equal("requirement/borrowing.max-active-loans", carried.GetProperty("requirement").GetString());
        Assert.Equal(
            ["use-case/lending.borrow-copy"],
            carried.GetProperty("fulfilledBy").EnumerateArray().Select(entry => entry.GetString()).ToList());

        Assert.Contains(
            "src/Lending/BorrowCopyHandler.cs",
            trace.GetProperty("implements").GetProperty("use-case/lending.borrow-copy").EnumerateArray().Select(entry => entry.GetString()));

        var proves = trace.GetProperty("proves").GetProperty("use-case/lending.borrow-copy").EnumerateArray().ToList();
        Assert.Contains(proves, entry => entry.GetProperty("scenario").GetString() == "BorrowCopyTests.Borrowing_An_Available_Copy_Succeeds");
        Assert.Empty(trace.GetProperty("proves").GetProperty("screen/library-app.loans").EnumerateArray());
    }

    /// <summary>Search finds a step by its identifier.</summary>
    /// <proves>litespec/core/agent#select-task-context</proves>
    [Fact]
    public void SearchFindsAStepByItsIdentifier()
    {
        using var temp = TestCli.Temp("query-step");
        Directory.CreateDirectory(Path.Combine(temp.Path, ".litespec"));
        File.WriteAllText(Path.Combine(temp.Path, ".litespec", "project.yaml"), "{}");

        var model = new ProjectModel { Config = ConsumerConfig.Load(temp.Path) };
        model.Records.Add(Rec(
            "step", "step/borrow-and-return.borrow-copy", Path.Combine(temp.Path, "borrow-and-return.yaml"),
            """{"statement": "The reader borrows the copy."}"""));

        Assert.Contains(RecordQuery.Search(model, "step/borrow-and-return.borrow-copy"), record => record.Id == "step/borrow-and-return.borrow-copy");
    }

    /// <summary>Builds one in-memory record from its JSON body.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record identifier.</param>
    /// <param name="file">The file the record sits in.</param>
    /// <param name="json">The record body.</param>
    /// <returns>The record.</returns>
    static Record Rec(string type, string id, string file, string json)
    {
        using var document = JsonDocument.Parse(json);

        return new Record
        {
            Type = type,
            Id = id,
            Title = id,
            File = file,
            Line = 1,
            Raw = document.RootElement.Clone(),
        };
    }
}
