using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Details;
using LiteSpec.Cli.Book.Outline;

namespace LiteSpec.Cli.Tests.Book;

/// <summary>Proves the book names the decision that chooses each external system's provider.</summary>
public sealed class BookDecisionTests
{
    /// <summary>An external system gains a decides edge from the decision a class implementing it follows, on both ends.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void ExternalSystemGainsDecidesEdgeFromTheFollowedDecision()
    {
        var model = Model(
            [
                Record("product", "product/library", "Library", "approved"),
                Record(
                    "decision",
                    "decision/0005-render-receipts-as-pdf",
                    "Render Receipts as PDF",
                    "approved",
                    file: ".litespec/implementation/decisions/0005-render-receipts-as-pdf.yaml"),
                Record(
                    "external-system",
                    "external-system/receipt-store",
                    "Receipt Store",
                    "reviewed",
                    file: ".litespec/behavior/external-systems/receipt-store.yaml"),
            ],
            implements: Links(Link("src/Infra/ReceiptStore.cs", "external-system/receipt-store")),
            follows: Links(Link("src/Infra/ReceiptStore.cs", "decision/0005-render-receipts-as-pdf")));

        var details = BookRecordDetails.Build(model, BookOutline.Build(model));

        Assert.True(HasEdge(details["decision/0005-render-receipts-as-pdf"], "out", "external-system/receipt-store", "decides"));
        Assert.True(HasEdge(details["external-system/receipt-store"], "in", "decision/0005-render-receipts-as-pdf", "decides"));
    }

    /// <summary>Two systems decide independently: each names only the decision its own implementers follow.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void EachSystemNamesOnlyItsOwnDecidingDecision()
    {
        var model = Model(
            [
                Record("product", "product/library", "Library", "approved"),
                Record("decision", "decision/0004-use-postgres", "Use Postgres", "approved"),
                Record("decision", "decision/0005-use-sqlite", "Use Sqlite", "approved"),
                Record("external-system", "external-system/main-store", "Main Store", "reviewed"),
                Record("external-system", "external-system/audit-store", "Audit Store", "reviewed"),
            ],
            implements: Links(
                Link("src/Infra/MainStore.cs", "external-system/main-store"),
                Link("src/Infra/AuditStore.cs", "external-system/audit-store")),
            follows: Links(
                Link("src/Infra/MainStore.cs", "decision/0004-use-postgres"),
                Link("src/Infra/AuditStore.cs", "decision/0005-use-sqlite")));

        var details = BookRecordDetails.Build(model, BookOutline.Build(model));

        Assert.True(HasEdge(details["external-system/main-store"], "in", "decision/0004-use-postgres", "decides"));
        Assert.False(HasEdge(details["external-system/main-store"], "in", "decision/0005-use-sqlite", "decides"));
        Assert.True(HasEdge(details["external-system/audit-store"], "in", "decision/0005-use-sqlite", "decides"));
        Assert.False(HasEdge(details["external-system/audit-store"], "in", "decision/0004-use-postgres", "decides"));
    }

    /// <summary>The first decision in identifier order decides when the implementers follow several.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void FirstDecisionInIdentifierOrderDecides()
    {
        var model = Model(
            [
                Record("product", "product/library", "Library", "approved"),
                Record("decision", "decision/0004-use-postgres", "Use Postgres", "approved"),
                Record("decision", "decision/0005-use-sqlite", "Use Sqlite", "approved"),
                Record("external-system", "external-system/main-store", "Main Store", "reviewed"),
            ],
            implements: Links(Link("src/Infra/MainStore.cs", "external-system/main-store")),
            follows: Links(
                Link("src/Infra/MainStore.cs", "decision/0005-use-sqlite"),
                Link("src/Infra/MainStore.cs", "decision/0004-use-postgres")));

        var details = BookRecordDetails.Build(model, BookOutline.Build(model));

        Assert.True(HasEdge(details["external-system/main-store"], "in", "decision/0004-use-postgres", "decides"));
        Assert.False(HasEdge(details["external-system/main-store"], "in", "decision/0005-use-sqlite", "decides"));
    }

    /// <summary>No decides edge appears when the implementers follow no decision: a provision, a departure, or an unknown id.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void NoDecidesEdgeWhenImplementersFollowNoDecision()
    {
        var model = Model(
            [
                Record("product", "product/library", "Library", "approved"),
                Record("decision", "decision/0004-use-postgres", "Use Postgres", "approved"),
                Record("departure", "departure/kiosk-cache", "Kiosk Cache", "approved"),
                Record("external-system", "external-system/main-store", "Main Store", "reviewed"),
            ],
            implements: Links(Link("src/Infra/MainStore.cs", "external-system/main-store")),
            follows: Links(
                Link("src/Infra/MainStore.cs", "litespec/backend/persistence#commit-each-command-pipeline-once"),
                Link("src/Infra/MainStore.cs", "departure/kiosk-cache"),
                Link("src/Infra/MainStore.cs", "decision/0099-missing")));

        var details = BookRecordDetails.Build(model, BookOutline.Build(model));

        Assert.Empty(Decides(details["external-system/main-store"], "in"));
        Assert.Empty(Decides(details["decision/0004-use-postgres"], "out"));
    }

    /// <summary>No decides edge appears when nothing implements the system, even when code follows a decision.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void NoDecidesEdgeWhenNothingImplementsTheSystem()
    {
        var model = Model(
            [
                Record("product", "product/library", "Library", "approved"),
                Record("decision", "decision/0004-use-postgres", "Use Postgres", "approved"),
                Record("external-system", "external-system/main-store", "Main Store", "reviewed"),
            ],
            follows: Links(Link("src/Infra/Renderer.cs", "decision/0004-use-postgres")));

        var details = BookRecordDetails.Build(model, BookOutline.Build(model));

        Assert.Empty(Decides(details["external-system/main-store"], "in"));
        Assert.Empty(Decides(details["decision/0004-use-postgres"], "out"));
    }

    /// <summary>A decision that already writes decides to the system gains no second, computed edge.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void WrittenDecidesEdgeIsNotDuplicated()
    {
        var model = Model(
            [
                Record("product", "product/library", "Library", "approved"),
                Record(
                    "decision",
                    "decision/0004-use-postgres",
                    "Use Postgres",
                    "approved",
                    fields: new JsonObject { ["decides"] = Links("external-system/main-store") }),
                Record("external-system", "external-system/main-store", "Main Store", "reviewed"),
            ],
            implements: Links(Link("src/Infra/MainStore.cs", "external-system/main-store")),
            follows: Links(Link("src/Infra/MainStore.cs", "decision/0004-use-postgres")));

        var details = BookRecordDetails.Build(model, BookOutline.Build(model));

        var outs = Decides(details["decision/0004-use-postgres"], "out");
        Assert.Single(outs);
        Assert.Equal("external-system/main-store", outs[0].Id);
        Assert.Equal("decision.decides", outs[0].Key);

        var ins = Decides(details["external-system/main-store"], "in");
        Assert.Single(ins);
        Assert.Equal("decision/0004-use-postgres", ins[0].Id);
        Assert.Equal("decision.decides", ins[0].Key);
    }

    static JsonObject Record(
        string type,
        string id,
        string title = "",
        string? status = null,
        JsonObject? fields = null,
        string? file = null)
    {
        var record = new JsonObject
        {
            ["type"] = type,
            ["id"] = id,
            ["title"] = title.Length > 0 ? title : id,
        };

        if (status is not null)
        {
            record["status"] = status;
        }

        if (file is not null)
        {
            record["file"] = file;
        }

        record["links"] = new JsonObject();
        record["fields"] = fields ?? new JsonObject();
        return record;
    }

    static JsonObject Model(JsonObject[] records, JsonArray? implements = null, JsonArray? follows = null)
    {
        var array = new JsonArray();
        foreach (var record in records)
        {
            array.Add(record);
        }

        return new JsonObject
        {
            ["records"] = array,
            ["links"] = new JsonObject
            {
                ["implements"] = implements ?? new JsonArray(),
                ["follows"] = follows ?? new JsonArray(),
            },
        };
    }

    static JsonObject Link(string path, string to) =>
        new()
        {
            ["path"] = path,
            ["to"] = to,
            ["kind"] = "adapter",
        };

    static JsonArray Links(params JsonNode[] nodes)
    {
        var array = new JsonArray();
        foreach (var node in nodes)
        {
            array.Add(node);
        }

        return array;
    }

    static JsonArray Links(params string[] ids)
    {
        var array = new JsonArray();
        foreach (var id in ids)
        {
            array.Add(id);
        }

        return array;
    }

    static bool HasEdge(JsonObject? detail, string direction, string id, string key) =>
        Decides(detail, direction).Contains((id, key));

    static List<(string Id, string Key)> Decides(JsonObject? detail, string direction)
    {
        var edges = new List<(string Id, string Key)>();
        foreach (var entry in detail?[direction] as JsonArray ?? [])
        {
            if (entry is JsonObject edge
                && edge["id"] is JsonValue to
                && to.TryGetValue<string>(out var id)
                && edge["key"] is JsonValue kind
                && kind.TryGetValue<string>(out var key)
                && (string.Equals(key, "decides", StringComparison.Ordinal) || key.EndsWith(".decides", StringComparison.Ordinal)))
            {
                edges.Add((id, key));
            }
        }

        return edges;
    }
}
