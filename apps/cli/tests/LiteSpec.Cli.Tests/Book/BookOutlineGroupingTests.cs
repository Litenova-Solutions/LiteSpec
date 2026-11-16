using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Outline;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Book;

/// <summary>Proves the outline groups records by record type in reading order.</summary>
public sealed class BookOutlineGroupingTests
{
    /// <summary>A goal, a problem solved by a feature with two requirements, and a technical decision produce the Goals, Problems and Decisions groups, with the feature only under its problem.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void GroupsGoalsProblemsAndDecisionsByRecordType()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("goal", "goal/faster-borrowing", "Faster Borrowing", "approved"),
            Record("problem", "problem/slow-desk-queues", "Slow Desk Queues", "approved"),
            Record(
                "feature",
                "feature/self-checkout",
                "Self Checkout",
                "approved",
                fields: new JsonObject { ["solves"] = Links("problem/slow-desk-queues") }),
            Record("requirement", "requirement/self-checkout.max-wait", "Max Wait", "approved"),
            Record("requirement", "requirement/self-checkout.copy-shelf-home", "Copy Shelf Home", "approved"),
            Record(
                "decision",
                "decision/0004-serve-receipts",
                "Serve Receipts",
                "approved",
                file: ".litespec/implementation/decisions/0004-serve-receipts.yaml"));

        var outline = BookOutline.Build(model);

        Assert.Empty(outline.Unplaced);

        var intent = Find(outline.Layers, "intent");
        Assert.NotNull(intent);
        Assert.Equal(
            new[] { "intent/goal", "intent/problem" },
            intent!.Children.Select(child => child.Id).ToList());

        var goals = Find(outline.Layers, "intent/goal");
        Assert.NotNull(goals);
        Assert.Equal("Goals", goals!.Label);
        Assert.Contains(goals.Children, child => string.Equals(child.Id, "goal/faster-borrowing", StringComparison.Ordinal));

        var problems = Find(outline.Layers, "intent/problem");
        Assert.NotNull(problems);
        Assert.Equal("Problems", problems!.Label);
        Assert.Contains(problems.Children, child => string.Equals(child.Id, "problem/slow-desk-queues", StringComparison.Ordinal));

        var problem = Find(outline.Layers, "problem/slow-desk-queues");
        Assert.NotNull(problem);
        Assert.Contains(problem!.Children, child => string.Equals(child.Id, "feature/self-checkout", StringComparison.Ordinal));

        var feature = Find(outline.Layers, "feature/self-checkout");
        Assert.NotNull(feature);
        Assert.Contains(feature!.Children, child => string.Equals(child.Id, "requirement/self-checkout.max-wait", StringComparison.Ordinal));
        Assert.Contains(feature.Children, child => string.Equals(child.Id, "requirement/self-checkout.copy-shelf-home", StringComparison.Ordinal));

        // The only feature solves a problem, so no top-level Features group exists: the feature sits only under its problem.
        Assert.Null(Find(outline.Layers, "intent/feature"));

        var decisions = Find(outline.Layers, "implementation/decision");
        Assert.NotNull(decisions);
        Assert.Equal("Decisions", decisions!.Label);
        Assert.Contains(decisions.Children, child => string.Equals(child.Id, "decision/0004-serve-receipts", StringComparison.Ordinal));
    }

    /// <summary>A feature solving no problem sits only under Features, and a product-level requirement sits under Product requirements.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void FeatureWithoutSolvedProblemsSitsOnlyUnderFeatures()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("problem", "problem/slow-desk-queues", "Slow Desk Queues", "approved"),
            Record("feature", "feature/kiosk-pickup", "Kiosk Pickup", "approved"),
            Record("requirement", "requirement/evening-access", "Evening Access", "approved"));

        var outline = BookOutline.Build(model);

        Assert.Empty(outline.Unplaced);

        var problem = Find(outline.Layers, "problem/slow-desk-queues");
        Assert.NotNull(problem);
        Assert.DoesNotContain(problem!.Children, child => string.Equals(child.Kind, "record", StringComparison.Ordinal));

        var features = Find(outline.Layers, "intent/feature");
        Assert.NotNull(features);
        Assert.Contains(features!.Children, child => string.Equals(child.Id, "feature/kiosk-pickup", StringComparison.Ordinal));

        var requirements = Find(outline.Layers, "intent/requirement");
        Assert.NotNull(requirements);
        Assert.Equal("Product requirements", requirements!.Label);
        Assert.Contains(requirements.Children, child => string.Equals(child.Id, "requirement/evening-access", StringComparison.Ordinal));
    }

    /// <summary>A technical decision filed under a module still tops the implementation Decisions group, never the module.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void ModuleLevelTechnicalDecisionTopsTheImplementationDecisions()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record(
                "decision",
                "decision/0005-render-receipts",
                "Render Receipts",
                "approved",
                file: ".litespec/implementation/modules/lending/decisions/0005-render-receipts.yaml"));

        Assert.Equal("module/lending", BookParents.ParentOf(model, "decision/0005-render-receipts"));

        var outline = BookOutline.Build(model);
        Assert.DoesNotContain("decision/0005-render-receipts", outline.Unplaced);

        var decisions = Find(outline.Layers, "implementation/decision");
        Assert.NotNull(decisions);
        Assert.Contains(decisions!.Children, child => string.Equals(child.Id, "decision/0005-render-receipts", StringComparison.Ordinal));

        var module = Find(outline.Layers, "module/lending");
        Assert.NotNull(module);
        Assert.DoesNotContain(module!.Children, child => string.Equals(child.Id, "decision/0005-render-receipts", StringComparison.Ordinal));
    }

    /// <summary>A store has no parent rule and stays unplaced with no outline node.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void StoreHasNoParentRuleAndStaysUnplaced()
    {
        Assert.False(BookParents.Rules.ContainsKey("store"));

        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record("store", "store/lending.hold-cache", "Hold Cache", "reviewed"));

        Assert.Null(BookParents.ParentOf(model, "store/lending.hold-cache"));

        var outline = BookOutline.Build(model);
        Assert.Contains("store/lending.hold-cache", outline.Unplaced);
        Assert.Null(Find(outline.Layers, "store/lending.hold-cache"));
    }

    static JsonObject Record(
        string type,
        string id,
        string title = "",
        string? status = null,
        JsonObject? links = null,
        JsonObject? fields = null,
        string? file = null,
        string? aggregate = null)
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

        if (aggregate is not null)
        {
            record["aggregate"] = aggregate;
        }

        record["links"] = links ?? new JsonObject();
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

        return BookStores.WithIntentCoverage(new JsonObject { ["records"] = array });
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
}
