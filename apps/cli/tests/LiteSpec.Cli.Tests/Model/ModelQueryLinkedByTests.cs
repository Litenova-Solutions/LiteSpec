using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>model query</c> lists the records that link to the queried record through a fragment link.</summary>
public sealed class ModelQueryLinkedByTests
{
    /// <summary>A use case calling an external-system operation appears in the system's linkedBy.</summary>
    /// <proves>litespec/core/agent#select-task-context</proves>
    [Fact]
    public void FragmentLinkAppearsInLinkedBy()
    {
        using var temp = TestCli.Temp("query-linked-by");
        Directory.CreateDirectory(Path.Combine(temp.Path, ".litespec"));
        File.WriteAllText(Path.Combine(temp.Path, ".litespec", "project.yaml"), "{}");

        var model = new ProjectModel { Config = ConsumerConfig.Load(temp.Path) };
        model.Records.Add(Rec(
            "external-system", "external-system/billing.gateway", Path.Combine(temp.Path, "gateway.yaml"),
            """{"summary": "The card gateway."}"""));
        model.Records.Add(Rec(
            "use-case", "use-case/billing.charge-card", Path.Combine(temp.Path, "charge.yaml"),
            """{"summary": "Billing charges the card.", "kind": "command", "calls": "external-system/billing.gateway#charge"}"""));

        var output = RecordQuery.Query(model, "external-system/billing.gateway");

        Assert.NotNull(output);
        using var parsed = JsonDocument.Parse(output);
        var linkedBy = parsed.RootElement.GetProperty("linkedBy");
        Assert.True(linkedBy.TryGetProperty("use-case/billing.charge-card", out var fields));
        Assert.Contains("calls", fields.EnumerateArray().Select(field => field.GetString()));
    }

    /// <summary>A use case querying a public query appears in its links and in the query's linkedBy.</summary>
    /// <proves>litespec/core/agent#select-task-context</proves>
    [Fact]
    public void QueriesEdgeAppearsInLinksAndLinkedBy()
    {
        using var temp = TestCli.Temp("query-linked-by");
        Directory.CreateDirectory(Path.Combine(temp.Path, ".litespec"));
        File.WriteAllText(Path.Combine(temp.Path, ".litespec", "project.yaml"), "{}");

        var model = new ProjectModel { Config = ConsumerConfig.Load(temp.Path) };
        model.Records.Add(Rec(
            "use-case", "use-case/spending.read-expense", Path.Combine(temp.Path, "read.yaml"),
            """{"summary": "Spending reads the expense.", "kind": "query", "reachedThrough": ["module/reporting"]}"""));
        model.Records.Add(Rec(
            "use-case", "use-case/reporting.record-reimbursement", Path.Combine(temp.Path, "record.yaml"),
            """{"summary": "Reporting records the reimbursement.", "kind": "command", "effects": [{"queries": "use-case/spending.read-expense", "timing": "inline"}]}"""));

        var holder = RecordQuery.Query(model, "use-case/reporting.record-reimbursement");

        Assert.NotNull(holder);
        using var holderParsed = JsonDocument.Parse(holder);
        var links = holderParsed.RootElement.GetProperty("links");
        Assert.True(links.TryGetProperty("effects", out var targets));
        Assert.Contains("use-case/spending.read-expense", targets.EnumerateArray().Select(target => target.GetString()));

        var provider = RecordQuery.Query(model, "use-case/spending.read-expense");

        Assert.NotNull(provider);
        using var providerParsed = JsonDocument.Parse(provider);
        var linkedBy = providerParsed.RootElement.GetProperty("linkedBy");
        Assert.True(linkedBy.TryGetProperty("use-case/reporting.record-reimbursement", out var fields));
        Assert.Contains("effects", fields.EnumerateArray().Select(field => field.GetString()));
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
