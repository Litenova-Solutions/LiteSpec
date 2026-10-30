using System.Text.Json.Nodes;
using LiteSpec.Book;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Book;

/// <summary>Proves the book outline places every record once under its parent.</summary>
public sealed class BookOutlineTests
{
    /// <summary>Every record type has a parent rule.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Every_Record_Type_Has_A_Parent_Rule()
    {
        foreach (var type in RecordTypes.All)
        {
            Assert.True(BookParents.Rules.ContainsKey(type), $"No parent rule for '{type}'.");
        }

        Assert.Equal(RecordTypes.All.Count, BookParents.Rules.Count);
    }

    /// <summary>The C# parent table matches the Parents table on the model page.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Parent_Rules_Match_The_Model_Page()
    {
        var table = ReadParentTable();
        Assert.Equal(BookParents.Rules.Count, table.Count);
        foreach (var pair in BookParents.Rules)
        {
            Assert.True(table.TryGetValue(pair.Key, out var row), $"The parent table names no '{pair.Key}'.");
            Assert.Equal(pair.Value.ParentTypes, row.Parents);
            Assert.Equal(pair.Value.FoundThrough, row.FoundThrough);
        }
    }

    /// <summary>The fixture model places every record exactly once under six layer roots.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Fixture_Model_Places_Every_Record_Exactly_Once()
    {
        var root = JsonNode.Parse(File.ReadAllText(Cli.Fixture("Book/review.json")))!;
        var model = root.AsObject();
        var outline = BookOutline.Build(model);

        var ids = new List<string>();
        if (model["records"] is JsonArray records)
        {
            foreach (var record in records.OfType<JsonObject>())
            {
                if (record["id"] is JsonValue scalar && scalar.TryGetValue<string>(out var id) && id is { Length: > 0 })
                {
                    ids.Add(id);
                }
            }
        }

        var placed = RecordIds(outline.Layers);
        Assert.Equal(ids.Count, placed.Count);
        Assert.Equal(ids.Count, new HashSet<string>(placed, StringComparer.Ordinal).Count);
        foreach (var id in ids)
        {
            Assert.Contains(id, placed);
        }

        foreach (var id in ids)
        {
            Assert.True(outline.ParentOf.ContainsKey(id), $"No parent entry for '{id}'.");
        }

        Assert.Equal(
            new[] { "intent", "behavior", "implementation", "proof", "governance", "glossary" },
            outline.Layers.Select(layer => layer.Id).ToArray());
    }

    /// <summary>A feature with several solves and advances entries places once under its first solves entry.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Multi_Parent_Feature_Places_Once()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("problem", "problem/slow-queues", "Slow Queues", "approved"),
            Record("problem", "problem/lost-copies", "Lost Copies", "approved"),
            Record("goal", "goal/faster-borrowing", "Faster Borrowing", "approved"),
            Record("goal", "goal/quieter-study", "Quieter Study", "approved"),
            Record(
                "feature",
                "feature/self-checkout",
                "Self Checkout",
                "approved",
                new JsonObject
                {
                    ["solves"] = Links("problem/slow-queues", "problem/lost-copies"),
                    ["advances"] = Links("goal/faster-borrowing", "goal/quieter-study"),
                }));

        var outline = BookOutline.Build(model);

        Assert.Equal("problem/slow-queues", outline.ParentOf["feature/self-checkout"]);
        var placed = RecordIds(outline.Layers);
        Assert.Equal(1, placed.Count(id => string.Equals(id, "feature/self-checkout", StringComparison.Ordinal)));
        Assert.DoesNotContain("feature/self-checkout", outline.Unplaced);

        var first = Find(outline.Layers, "problem/slow-queues");
        Assert.NotNull(first);
        Assert.True(first!.Children.Any(child => string.Equals(child.Id, "feature/self-checkout", StringComparison.Ordinal)));

        var second = Find(outline.Layers, "problem/lost-copies");
        Assert.NotNull(second);
        Assert.False(second!.Children.Any(child => string.Equals(child.Id, "feature/self-checkout", StringComparison.Ordinal)));
    }

    /// <summary>Records with no parent land in Unplaced and still appear once under a type group.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Unplaced_Records_Land_In_Unplaced()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("problem", "problem/slow-queues", "Slow Queues", "approved"),
            Record("feature", "feature/orphan", "Orphan", "draft"),
            Record("requirement", "requirement/borrowing.orphan-need", "Orphan Need", "draft"),
            Record("journey", "journey/nowhere", "Nowhere", "draft"));

        var outline = BookOutline.Build(model);

        Assert.Null(outline.ParentOf["feature/orphan"]);
        Assert.Null(outline.ParentOf["requirement/borrowing.orphan-need"]);
        Assert.Null(outline.ParentOf["journey/nowhere"]);
        Assert.Contains("feature/orphan", outline.Unplaced);
        Assert.Contains("requirement/borrowing.orphan-need", outline.Unplaced);
        Assert.Contains("journey/nowhere", outline.Unplaced);
        Assert.DoesNotContain("product/library", outline.Unplaced);

        var placed = RecordIds(outline.Layers);
        Assert.Equal(1, placed.Count(id => string.Equals(id, "feature/orphan", StringComparison.Ordinal)));
        Assert.Equal(1, placed.Count(id => string.Equals(id, "requirement/borrowing.orphan-need", StringComparison.Ordinal)));
        Assert.Equal(1, placed.Count(id => string.Equals(id, "journey/nowhere", StringComparison.Ordinal)));

        Assert.NotNull(Find(outline.Layers, "intent/feature"));
        Assert.NotNull(Find(outline.Layers, "intent/requirement"));
        Assert.NotNull(Find(outline.Layers, "intent/journey"));
    }

    /// <summary>A requirement follows features before basedOn before the deciding decision.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Requirement_Uses_Features_Before_BasedOn_Before_Decides()
    {
        var carried = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("problem", "problem/slow-queues", "Slow Queues", "approved"),
            Record(
                "feature",
                "feature/self-checkout",
                "Self Checkout",
                "approved",
                new JsonObject { ["solves"] = Links("problem/slow-queues") }),
            Record("constraint", "constraint/retention", "Retention", "approved"),
            Record(
                "requirement",
                "requirement/borrowing.max-wait",
                "Max Wait",
                "approved",
                new JsonObject
                {
                    ["features"] = Links("feature/self-checkout"),
                    ["basedOn"] = Links("constraint/retention"),
                }));

        Assert.Equal("feature/self-checkout", BookParents.ParentOf(carried, "requirement/borrowing.max-wait"));

        var grounded = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("constraint", "constraint/retention", "Retention", "approved"),
            Record(
                "requirement",
                "requirement/borrowing.max-wait",
                "Max Wait",
                "approved",
                new JsonObject { ["basedOn"] = Links("constraint/retention") }));

        Assert.Equal("constraint/retention", BookParents.ParentOf(grounded, "requirement/borrowing.max-wait"));

        var decided = Model(
            Record("product", "product/library", "Library", "approved"),
            Record(
                "decision",
                "decision/0001-kiosk",
                "Kiosk",
                "approved",
                new JsonObject { ["decides"] = Links("requirement/borrowing.max-wait") },
                file: "docs/intent/decisions/0001-kiosk.md"),
            Record("requirement", "requirement/borrowing.max-wait", "Max Wait", "approved"));

        Assert.Equal("decision/0001-kiosk", BookParents.ParentOf(decided, "requirement/borrowing.max-wait"));

        var legacy = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("problem", "problem/slow-queues", "Slow Queues", "approved"),
            Record(
                "feature",
                "feature/self-checkout",
                "Self Checkout",
                "approved",
                new JsonObject { ["solves"] = Links("problem/slow-queues") }),
            Record(
                "requirement",
                "requirement/borrowing.max-wait",
                "Max Wait",
                "approved",
                new JsonObject { ["feature"] = Links("feature/self-checkout") }));

        Assert.Equal("feature/self-checkout", BookParents.ParentOf(legacy, "requirement/borrowing.max-wait"));
    }

    /// <summary>Containment follows the identifier home: aggregates, use cases, results and rules.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Containment_Places_Aggregates_UseCases_Results_And_Rules()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record("module", "module/copies", "Copies", "reviewed"),
            Record("aggregate", "aggregate/lending.loan", "Loan", "reviewed"),
            Record("use-case", "use-case/lending.borrow-copy", "Borrow Copy", "reviewed", aggregate: "aggregate/lending.loan"),
            Record("use-case", "use-case/lending.list-loans", "List Loans", "reviewed"),
            Record("result", "result/borrow-copy.borrowed", "Borrowed", "reviewed"),
            Record("rule", "rule/loan.one-loan-per-copy", "One Loan Per Copy", "reviewed"),
            Record("rule", "rule/copies.shelf-marks-face-out", "Shelf Marks Face Out", "reviewed"));

        Assert.Equal("module/lending", BookParents.ParentOf(model, "aggregate/lending.loan"));
        Assert.Equal("aggregate/lending.loan", BookParents.ParentOf(model, "use-case/lending.borrow-copy"));
        Assert.Equal("module/lending", BookParents.ParentOf(model, "use-case/lending.list-loans"));
        Assert.Equal("use-case/lending.borrow-copy", BookParents.ParentOf(model, "result/borrow-copy.borrowed"));
        Assert.Equal("aggregate/lending.loan", BookParents.ParentOf(model, "rule/loan.one-loan-per-copy"));
        Assert.Equal("module/copies", BookParents.ParentOf(model, "rule/copies.shelf-marks-face-out"));

        var outline = BookOutline.Build(model);
        var loan = Find(outline.Layers, "aggregate/lending.loan");
        Assert.NotNull(loan);
        Assert.True(loan!.Children.Any(child => string.Equals(child.Id, "use-case/lending.borrow-copy", StringComparison.Ordinal)));
    }

    /// <summary>A screen places only when its entryPoint field agrees with its identifier home.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Screen_Needs_An_Agreeing_EntryPoint()
    {
        var agreed = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("entry-point", "entry-point/library-app", "Library App", "reviewed"),
            Record(
                "screen",
                "screen/library-app.expense-list",
                "Expense List",
                "reviewed",
                new JsonObject { ["entryPoint"] = Links("entry-point/library-app") }));

        Assert.Equal("entry-point/library-app", BookParents.ParentOf(agreed, "screen/library-app.expense-list"));

        var split = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("entry-point", "entry-point/library-app", "Library App", "reviewed"),
            Record("entry-point", "entry-point/kiosk", "Kiosk", "reviewed"),
            Record(
                "screen",
                "screen/library-app.expense-list",
                "Expense List",
                "reviewed",
                new JsonObject { ["entryPoint"] = Links("entry-point/kiosk") }));

        Assert.Null(BookParents.ParentOf(split, "screen/library-app.expense-list"));
        Assert.Contains("screen/library-app.expense-list", BookOutline.Build(split).Unplaced);

        var missing = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("entry-point", "entry-point/library-app", "Library App", "reviewed"),
            Record("screen", "screen/library-app.expense-list", "Expense List", "reviewed"));

        Assert.Null(BookParents.ParentOf(missing, "screen/library-app.expense-list"));
    }

    /// <summary>Root types parent the product with no written field, and the product has no parent.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Root_Types_Parent_The_Product()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("goal", "goal/faster-borrowing", "Faster Borrowing", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record("term", "term/loan", "Loan", "approved"),
            Record("deviation", "deviation/kiosk-cache", "Kiosk Cache", "approved"));

        Assert.Null(BookParents.ParentOf(model, "product/library"));
        Assert.Equal("product/library", BookParents.ParentOf(model, "goal/faster-borrowing"));
        Assert.Equal("product/library", BookParents.ParentOf(model, "module/lending"));
        Assert.Equal("product/library", BookParents.ParentOf(model, "term/loan"));
        Assert.Equal("product/library", BookParents.ParentOf(model, "deviation/kiosk-cache"));

        var outline = BookOutline.Build(model);
        Assert.DoesNotContain("goal/faster-borrowing", outline.Unplaced);
        Assert.DoesNotContain("module/lending", outline.Unplaced);
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

        return new JsonObject { ["records"] = array };
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

    static List<string> RecordIds(IReadOnlyList<BookOutlineNode> layers)
    {
        var found = new List<string>();
        foreach (var layer in layers)
        {
            Collect(layer, found);
        }

        return found;
    }

    static void Collect(BookOutlineNode node, List<string> found)
    {
        if (string.Equals(node.Kind, "record", StringComparison.Ordinal))
        {
            found.Add(node.Id);
        }

        foreach (var child in node.Children)
        {
            Collect(child, found);
        }
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

    static Dictionary<string, (List<string> Parents, string FoundThrough)> ReadParentTable()
    {
        var lines = File.ReadAllLines(Path.Combine(Cli.Repository, "docs", "core", "model.md"));
        var start = -1;
        for (var index = 0; index < lines.Length; index++)
        {
            if (string.Equals(lines[index].Trim(), "### Parents", StringComparison.Ordinal))
            {
                start = index;
                break;
            }
        }

        Assert.True(start >= 0, "docs/core/model.md holds no ### Parents section.");

        var header = -1;
        for (var index = start; index < lines.Length; index++)
        {
            if (lines[index].Contains("Record type", StringComparison.Ordinal)
                && lines[index].Contains("Found through", StringComparison.Ordinal))
            {
                header = index;
                break;
            }
        }

        Assert.True(header >= 0, "The ### Parents section holds no parent table.");

        var table = new Dictionary<string, (List<string> Parents, string FoundThrough)>(StringComparer.Ordinal);
        for (var index = header + 2; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (!line.StartsWith("|", StringComparison.Ordinal))
            {
                break;
            }

            var cells = line.Split('|');
            Assert.True(cells.Length >= 4, $"Parent table row {index + 1} holds fewer than three cells: {line}.");
            var type = cells[1].Trim().Trim('`', ' ', '\t');
            var parentCell = cells[2].Trim();
            var foundCell = cells[3].Trim();
            Assert.True(type.Length > 0, $"Parent table row {index + 1} names no record type.");

            var parents = new List<string>();
            if (!string.Equals(parentCell, "none", StringComparison.Ordinal))
            {
                if (string.Equals(parentCell, "the product", StringComparison.Ordinal))
                {
                    parents.Add("product");
                }
                else
                {
                    var begin = -1;
                    for (var position = 0; position < parentCell.Length; position++)
                    {
                        if (parentCell[position] == '`')
                        {
                            if (begin < 0)
                            {
                                begin = position + 1;
                            }
                            else
                            {
                                parents.Add(parentCell.Substring(begin, position - begin));
                                begin = -1;
                            }
                        }
                    }

                    Assert.True(parents.Count > 0, $"Parent table row for '{type}' names no parent: {parentCell}.");
                }
            }

            Assert.True(!table.ContainsKey(type), $"The parent table names '{type}' twice.");
            table[type] = (parents, foundCell);
        }

        return table;
    }
}
