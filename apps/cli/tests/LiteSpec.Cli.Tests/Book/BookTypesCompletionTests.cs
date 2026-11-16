using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Details;
using LiteSpec.Cli.Book.Outline;

namespace LiteSpec.Cli.Tests.Book;

/// <summary>Proves the book carries the new record types, completion stages and proof freshness.</summary>
public sealed class BookTypesCompletionTests
{
    /// <summary>A module event parents its module by identifier home and file, sorts before its integration events, and stays unplaced without a module.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void ModuleEventParentsItsModuleBeforeItsIntegrationEvents()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record(
                "module-event",
                "module-event/lending.loan-borrowed",
                "Loan Borrowed",
                "reviewed"),
            Record(
                "integration-event",
                "integration-event/loan-borrowed",
                "Loan Borrowed",
                "reviewed",
                file: ".litespec/behavior/modules/lending/integration-events/loan-borrowed/loan-borrowed.yaml"));

        Assert.Equal("module/lending", BookParents.ParentOf(model, "module-event/lending.loan-borrowed"));

        var outline = BookOutline.Build(model);
        Assert.DoesNotContain("module-event/lending.loan-borrowed", outline.Unplaced);
        var module = Find(outline.Layers, "module/lending");
        Assert.NotNull(module);
        Assert.Equal(
            new[] { "module-event", "integration-event" },
            module!.Children
                .Where(child => string.Equals(child.Kind, "record", StringComparison.Ordinal))
                .Select(child => child.Type)
                .ToList());

        var filed = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record("module", "module/copies", "Copies", "reviewed"),
            Record(
                "module-event",
                "module-event/copies.loan-borrowed",
                "Loan Borrowed",
                "reviewed",
                file: ".litespec/behavior/modules/lending/module-events/loan-borrowed/loan-borrowed.yaml"));

        Assert.Equal("module/lending", BookParents.ParentOf(filed, "module-event/copies.loan-borrowed"));
        Assert.DoesNotContain("module-event/copies.loan-borrowed", BookOutline.Build(filed).Unplaced);

        var missing = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module-event", "module-event/lending.loan-borrowed", "Loan Borrowed", "reviewed"));

        Assert.Null(BookParents.ParentOf(missing, "module-event/lending.loan-borrowed"));
        Assert.Contains("module-event/lending.loan-borrowed", BookOutline.Build(missing).Unplaced);
    }

    /// <summary>A detail carries the model completion stage with its parked children and scope counts beside the code completion.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void DetailCarriesCompletionStageAndParkedChildren()
    {
        var requirement = Record("requirement", "requirement/self-checkout.max-wait", "Max Wait", "approved");
        requirement["completion"] = new JsonObject
        {
            ["stage"] = "verified",
            ["proof"] = "known",
            ["heldBy"] = new JsonArray(),
            ["parkedBy"] = new JsonArray(
                new JsonObject
                {
                    ["id"] = "requirement/self-checkout.late-fee",
                    ["waiver"] = "waiver/late-fee-hold",
                    ["expiresOn"] = "2026-12-31",
                },
                new JsonObject
                {
                    ["id"] = "requirement/self-checkout.quiet-hours",
                    ["waiver"] = "waiver/quiet-hold",
                    ["expiresOn"] = null,
                }),
            ["inScope"] = 12,
            ["parked"] = 2,
            ["total"] = 14,
            ["counts"] = new JsonObject
            {
                ["requirements verified"] = new JsonObject { ["done"] = 12, ["of"] = 12 },
            },
        };
        var bare = Record("requirement", "requirement/self-checkout.other", "Other", "approved");
        var model = Model(requirement, bare);
        var details = BookRecordDetails.Build(model, BookOutline.Build(model));

        var completion = Obj(details["requirement/self-checkout.max-wait"], "completion");
        Assert.Equal("verified", Str(completion, "stage"));
        Assert.Equal("known", Str(completion, "proof"));
        Assert.Empty(Arr(completion, "heldBy"));
        var parked = Arr(completion, "parkedBy");
        Assert.Equal(2, parked.Count);
        Assert.Equal("requirement/self-checkout.late-fee", Str(parked[0] as JsonObject, "id"));
        Assert.Equal("waiver/late-fee-hold", Str(parked[0] as JsonObject, "waiver"));
        Assert.Equal("2026-12-31", Str(parked[0] as JsonObject, "expiresOn"));
        Assert.Null((parked[1] as JsonObject)!["expiresOn"]);
        Assert.Equal(12, completion["inScope"]?.GetValue<int>());
        Assert.Equal(2, completion["parked"]?.GetValue<int>());
        Assert.Equal(14, completion["total"]?.GetValue<int>());
        var counts = Obj(completion, "counts");
        var verified = counts["requirements verified"] as JsonObject;
        Assert.Equal(12, verified?["done"]?.GetValue<int>());
        Assert.Equal(12, verified?["of"]?.GetValue<int>());

        var missing = Obj(details["requirement/self-checkout.other"], "completion");
        Assert.Null(missing["stage"]);
        Assert.Null(missing["parkedBy"]);
        Assert.Null(missing["inScope"]);
    }

    /// <summary>A scenario proof carries the run freshness the model computed, with the stale reason only when stale.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void ScenarioProofCarriesRunFreshness()
    {
        var stale = Record("scenario", "scenario/borrow-copy.stale", "Stale");
        stale["proof"] = new JsonObject
        {
            ["commit"] = "abc1234567890",
            ["lastPassedAt"] = "abc1234",
            ["status"] = "stale",
            ["commitsBehind"] = 3,
            ["staleReason"] = "files outside the record and documentation paths changed",
        };
        var fresh = Record("scenario", "scenario/borrow-copy.fresh", "Fresh");
        fresh["proof"] = new JsonObject
        {
            ["commit"] = "def1234567890",
            ["lastPassedAt"] = "def1234",
            ["status"] = "fresh",
            ["commitsBehind"] = 0,
        };
        var bare = Record("scenario", "scenario/borrow-copy.bare", "Bare");
        var model = Model(stale, fresh, bare);
        model["testRun"] = new JsonObject { ["date"] = "2026-10-20" };
        var details = BookRecordDetails.Build(model, BookOutline.Build(model));

        var staleProof = Obj(details["scenario/borrow-copy.stale"], "proof");
        Assert.Equal("abc1234", Str(staleProof, "lastPassedAt"));
        Assert.Equal("stale", Str(staleProof, "status"));
        Assert.Equal(3, staleProof["commitsBehind"]?.GetValue<int>());
        Assert.True((Str(staleProof, "staleReason") ?? "").Length > 0);

        var freshProof = Obj(details["scenario/borrow-copy.fresh"], "proof");
        Assert.Equal("fresh", Str(freshProof, "status"));
        Assert.Null(freshProof["staleReason"]);

        var bareProof = Obj(details["scenario/borrow-copy.bare"], "proof");
        Assert.Null(bareProof["status"]);
        Assert.Null(bareProof["lastPassedAt"]);
    }

    /// <summary>A decision links what it supersedes outward and what supersedes it inward; unknown and self targets add no edge.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void DecisionSupersedesEdgesReadBothDirections()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record(
                "decision",
                "decision/0004-use-postgres",
                "Use Postgres",
                "approved",
                fields: new JsonObject { ["supersedes"] = Links("decision/0003-use-sqlite") }),
            Record("decision", "decision/0003-use-sqlite", "Use Sqlite", "approved"),
            Record(
                "decision",
                "decision/0005-unknown",
                "Unknown",
                "approved",
                fields: new JsonObject { ["supersedes"] = Links("decision/0099-missing") }),
            Record(
                "decision",
                "decision/0006-self",
                "Self",
                "approved",
                fields: new JsonObject { ["supersedes"] = Links("decision/0006-self") }));

        var details = BookRecordDetails.Build(model, BookOutline.Build(model));

        Assert.True(HasEdge(
            Arr(details["decision/0004-use-postgres"], "out"),
            "decision/0003-use-sqlite",
            "decision.supersedes"));
        Assert.True(HasEdge(
            Arr(details["decision/0003-use-sqlite"], "in"),
            "decision/0004-use-postgres",
            "decision.supersedes"));
        Assert.Empty(Supersedes(Arr(details["decision/0005-unknown"], "out")));
        Assert.Empty(Supersedes(Arr(details["decision/0006-self"], "out")));
    }

    /// <summary>A use case publishes the module events translating the domain events it raises, like its integration events.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void UseCasePublishesTranslatedModuleEvents()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record(
                "use-case",
                "use-case/lending.borrow-copy",
                "Borrow Copy",
                "reviewed",
                fields: new JsonObject { ["raises"] = Links("domain-event/loan.loan-borrowed") }),
            Record("domain-event", "domain-event/loan.loan-borrowed", "Loan Borrowed", "reviewed"),
            Record(
                "module-event",
                "module-event/lending.loan-borrowed",
                "Loan Borrowed",
                "reviewed",
                fields: new JsonObject { ["translates"] = Links("domain-event/loan.loan-borrowed") }),
            Record(
                "module-event",
                "module-event/lending.untranslated",
                "Untranslated",
                "reviewed"),
            Record(
                "module-event",
                "module-event/lending.unknown-source",
                "Unknown Source",
                "reviewed",
                fields: new JsonObject { ["translates"] = Links("domain-event/loan.missing") }));

        var details = BookRecordDetails.Build(model, BookOutline.Build(model));

        Assert.True(HasEdge(
            Arr(details["use-case/lending.borrow-copy"], "out"),
            "module-event/lending.loan-borrowed",
            "publishes"));
        Assert.True(HasEdge(
            Arr(details["module-event/lending.loan-borrowed"], "in"),
            "use-case/lending.borrow-copy",
            "publishes"));
        Assert.False(HasEdge(
            Arr(details["use-case/lending.borrow-copy"], "out"),
            "module-event/lending.untranslated",
            "publishes"));
        Assert.False(HasEdge(
            Arr(details["use-case/lending.borrow-copy"], "out"),
            "module-event/lending.unknown-source",
            "publishes"));
    }

    /// <summary>A module event reads its tested state from the tested map the builder wrote, like an integration event.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void ModuleEventTestedComesFromTestedMap()
    {
        var model = Model(
            Record("module-event", "module-event/lending.loan-borrowed", "Loan Borrowed", "reviewed"),
            Record("module-event", "module-event/lending.unread", "Unread", "reviewed"));
        model["tested"] = new JsonObject { ["module-event/lending.loan-borrowed"] = true };
        var details = BookRecordDetails.Build(model, BookOutline.Build(model));

        Assert.True(BoolOf(Obj(details["module-event/lending.loan-borrowed"], "machine")["tested"]));
        Assert.Null(details["module-event/lending.unread"]["machine"]);
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

    static JsonObject Model(params JsonObject[] records)
    {
        var array = new JsonArray();
        foreach (var record in records)
        {
            array.Add(record);
        }

        return BookStores.WithIntentCoverage(new JsonObject
        {
            ["records"] = array,
            ["code"] = new JsonObject
            {
                ["handlers"] = new JsonArray(),
                ["endpoints"] = new JsonArray(),
                ["aggregateMethods"] = new JsonArray(),
                ["reactions"] = new JsonArray(),
                ["jobs"] = new JsonArray(),
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
            ["tests"] = new JsonArray(),
            ["browserTests"] = new JsonArray(),
            ["screens"] = new JsonArray(),
            ["exercisedSteps"] = new JsonArray(),
            ["findings"] = new JsonArray(),
            ["covered"] = new JsonArray(),
            ["testRun"] = null,
        });
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

    static BookOutlineNode? Find(IReadOnlyList<BookOutlineNode> layers, string id)
    {
        foreach (var layer in layers)
        {
            if (FindIn(layer, id) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    static BookOutlineNode? FindIn(BookOutlineNode node, string id)
    {
        if (string.Equals(node.Id, id, StringComparison.Ordinal))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            if (FindIn(child, id) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    static bool HasEdge(JsonArray edges, string id, string key)
    {
        foreach (var entry in edges.OfType<JsonObject>())
        {
            if (string.Equals(Str(entry, "id"), id, StringComparison.Ordinal)
                && string.Equals(Str(entry, "key"), key, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    static List<JsonObject> Supersedes(JsonArray edges)
    {
        var found = new List<JsonObject>();
        foreach (var entry in edges.OfType<JsonObject>())
        {
            var key = Str(entry, "key") ?? "";
            if (string.Equals(key, "supersedes", StringComparison.Ordinal) || key.EndsWith(".supersedes", StringComparison.Ordinal))
            {
                found.Add(entry);
            }
        }

        return found;
    }

    static string? Str(JsonObject? obj, string name) =>
        obj?[name] is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? text : null;

    static JsonArray Arr(JsonObject obj, string name) => obj[name] as JsonArray ?? new JsonArray();

    static JsonObject Obj(JsonObject obj, string name) => obj[name] as JsonObject ?? new JsonObject();

    static bool BoolOf(JsonNode? node) =>
        node is JsonValue scalar && scalar.TryGetValue<bool>(out var flag) && flag;
}
