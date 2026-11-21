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

    /// <summary>A use case commanding a public command appears in its links and in the command's linkedBy.</summary>
    /// <proves>litespec/core/agent#select-task-context</proves>
    [Fact]
    public void CommandsEdgeAppearsInLinksAndLinkedBy()
    {
        using var temp = TestCli.Temp("query-linked-by");
        Directory.CreateDirectory(Path.Combine(temp.Path, ".litespec"));
        File.WriteAllText(Path.Combine(temp.Path, ".litespec", "project.yaml"), "{}");

        var model = new ProjectModel { Config = ConsumerConfig.Load(temp.Path) };
        model.Records.Add(Rec(
            "use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.yaml"),
            """{"summary": "Spending reimburses the expense.", "kind": "command", "reachedThrough": ["module/reporting"]}"""));
        model.Records.Add(Rec(
            "use-case", "use-case/reporting.confirm-payout", Path.Combine(temp.Path, "confirm.yaml"),
            """{"summary": "Reporting confirms the payout.", "kind": "command", "effects": [{"commands": "use-case/spending.reimburse-expense", "timing": "inline"}]}"""));

        var holder = RecordQuery.Query(model, "use-case/reporting.confirm-payout");

        Assert.NotNull(holder);
        using var holderParsed = JsonDocument.Parse(holder);
        var links = holderParsed.RootElement.GetProperty("links");
        Assert.True(links.TryGetProperty("effects", out var targets));
        Assert.Contains("use-case/spending.reimburse-expense", targets.EnumerateArray().Select(target => target.GetString()));

        var provider = RecordQuery.Query(model, "use-case/spending.reimburse-expense");

        Assert.NotNull(provider);
        using var providerParsed = JsonDocument.Parse(provider);
        var linkedBy = providerParsed.RootElement.GetProperty("linkedBy");
        Assert.True(linkedBy.TryGetProperty("use-case/reporting.confirm-payout", out var fields));
        Assert.Contains("effects", fields.EnumerateArray().Select(field => field.GetString()));
    }

    /// <summary>A permission naming its roles appears in each role's linkedBy through grantedTo.</summary>
    /// <proves>litespec/core/agent#select-task-context</proves>
    [Fact]
    public void GrantedPermissionAppearsInRoleLinkedBy()
    {
        using var temp = TestCli.Temp("query-linked-by");
        Directory.CreateDirectory(Path.Combine(temp.Path, ".litespec"));
        File.WriteAllText(Path.Combine(temp.Path, ".litespec", "project.yaml"), "{}");

        var model = new ProjectModel { Config = ConsumerConfig.Load(temp.Path) };
        model.Records.Add(Rec(
            "role", "role/reader", Path.Combine(temp.Path, "reader.yaml"),
            """{"summary": "Readers act on their own loans.", "scope": "self"}"""));
        model.Records.Add(Rec(
            "permission", "permission/read-loans", Path.Combine(temp.Path, "read-loans.yaml"),
            """{"meaning": "Read own loans.", "grantedTo": ["role/reader"]}"""));

        var output = RecordQuery.Query(model, "role/reader");

        Assert.NotNull(output);
        using var parsed = JsonDocument.Parse(output);
        var linkedBy = parsed.RootElement.GetProperty("linkedBy");
        Assert.True(linkedBy.TryGetProperty("permission/read-loans", out var fields));
        Assert.Contains("grantedTo", fields.EnumerateArray().Select(field => field.GetString()));
    }

    /// <summary>A rule naming its decision appears in the decision's linkedBy through basedOn.</summary>
    /// <proves>litespec/core/agent#select-task-context</proves>
    [Fact]
    public void SettledRuleAppearsInDecisionLinkedBy()
    {
        using var temp = TestCli.Temp("query-linked-by");
        Directory.CreateDirectory(Path.Combine(temp.Path, ".litespec"));
        File.WriteAllText(Path.Combine(temp.Path, ".litespec", "project.yaml"), "{}");

        var model = new ProjectModel { Config = ConsumerConfig.Load(temp.Path) };
        model.Records.Add(Rec(
            "decision", "decision/0001-loan-period", Path.Combine(temp.Path, "loan-period.yaml"),
            """{"title": "Loan period", "choice": "21 days"}"""));
        model.Records.Add(Rec(
            "rule", "rule/loan.extended-before-due", Path.Combine(temp.Path, "loan-rules.yaml"),
            """{"title": "Extended before due", "kind": "precondition", "statement": "A loan extends only before its due date.", "basedOn": ["decision/0001-loan-period"]}"""));

        var output = RecordQuery.Query(model, "decision/0001-loan-period");

        Assert.NotNull(output);
        using var parsed = JsonDocument.Parse(output);
        var linkedBy = parsed.RootElement.GetProperty("linkedBy");
        Assert.True(linkedBy.TryGetProperty("rule/loan.extended-before-due", out var fields));
        Assert.Contains("basedOn", fields.EnumerateArray().Select(field => field.GetString()));
    }

    /// <summary>An external system naming its actor appears in the actor's linkedBy through represents.</summary>
    /// <proves>litespec/core/agent#select-task-context</proves>
    [Fact]
    public void RepresentingSystemAppearsInActorLinkedBy()
    {
        using var temp = TestCli.Temp("query-linked-by");
        Directory.CreateDirectory(Path.Combine(temp.Path, ".litespec"));
        File.WriteAllText(Path.Combine(temp.Path, ".litespec", "project.yaml"), "{}");

        var model = new ProjectModel { Config = ConsumerConfig.Load(temp.Path) };
        model.Records.Add(Rec(
            "actor", "actor/text-gateway", Path.Combine(temp.Path, "gateway.yaml"),
            """{"summary": "The text gateway sends overdue notices.", "kind": "system"}"""));
        model.Records.Add(Rec(
            "external-system", "external-system/text-gateway", Path.Combine(temp.Path, "text-gateway.yaml"),
            """{"summary": "The text gateway sends overdue notices.", "vendor": "Messageline", "represents": "actor/text-gateway"}"""));

        var output = RecordQuery.Query(model, "actor/text-gateway");

        Assert.NotNull(output);
        using var parsed = JsonDocument.Parse(output);
        var linkedBy = parsed.RootElement.GetProperty("linkedBy");
        Assert.True(linkedBy.TryGetProperty("external-system/text-gateway", out var fields));
        Assert.Contains("represents", fields.EnumerateArray().Select(field => field.GetString()));
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
