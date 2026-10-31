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

    /// <summary>The fixture model places every record with a parent exactly once under six layer roots, and only the parentless journey stays unplaced with no node.</summary>
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

        Assert.Equal(new[] { "journey/borrowing" }, outline.Unplaced.OrderBy(id => id, StringComparer.Ordinal).ToList());

        var placed = RecordIds(outline.Layers);
        Assert.Equal(ids.Count - outline.Unplaced.Count, placed.Count);
        Assert.Equal(placed.Count, new HashSet<string>(placed, StringComparer.Ordinal).Count);
        foreach (var id in ids)
        {
            if (outline.Unplaced.Contains(id))
            {
                Assert.DoesNotContain(id, placed);
                Assert.Null(Find(outline.Layers, id));
            }
            else
            {
                Assert.Contains(id, placed);
            }
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

    /// <summary>Records with no parent land only in Unplaced, with no outline node and no invented group.</summary>
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
        Assert.DoesNotContain("feature/orphan", placed);
        Assert.DoesNotContain("requirement/borrowing.orphan-need", placed);
        Assert.DoesNotContain("journey/nowhere", placed);
        Assert.Null(Find(outline.Layers, "feature/orphan"));
        Assert.Null(Find(outline.Layers, "requirement/borrowing.orphan-need"));
        Assert.Null(Find(outline.Layers, "journey/nowhere"));

        Assert.Null(Find(outline.Layers, "intent/feature"));
        Assert.Null(Find(outline.Layers, "intent/requirement"));
        Assert.Null(Find(outline.Layers, "intent/journey"));
    }

    /// <summary>A record whose parent is itself unplaced has no outline node either: it is only listed unplaced.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Child_Of_An_Unplaced_Parent_Has_No_Outline_Node()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("aggregate", "aggregate/missing.loan", "Loan", "reviewed"),
            Record("rule", "rule/loan.one-loan-per-copy", "One Loan Per Copy", "reviewed"));

        var outline = BookOutline.Build(model);

        Assert.Equal("aggregate/missing.loan", outline.ParentOf["rule/loan.one-loan-per-copy"]);
        Assert.Contains("aggregate/missing.loan", outline.Unplaced);
        Assert.Contains("rule/loan.one-loan-per-copy", outline.Unplaced);
        Assert.Null(Find(outline.Layers, "aggregate/missing.loan"));
        Assert.Null(Find(outline.Layers, "rule/loan.one-loan-per-copy"));
    }

    /// <summary>A requirement parents its topic by identifier home; its cites never move it.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Requirement_Parents_Its_Topic_By_Identifier_Home()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("topic", "topic/borrowing", "Borrowing", "approved"),
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
                new JsonObject { ["cites"] = Links("feature/self-checkout") }));

        Assert.Equal("product/library", BookParents.ParentOf(model, "topic/borrowing"));
        Assert.Equal("topic/borrowing", BookParents.ParentOf(model, "requirement/borrowing.max-wait"));

        var outline = BookOutline.Build(model);
        Assert.DoesNotContain("requirement/borrowing.max-wait", outline.Unplaced);
        var topic = Find(outline.Layers, "topic/borrowing");
        Assert.NotNull(topic);
        Assert.True(topic!.Children.Any(child => string.Equals(child.Id, "requirement/borrowing.max-wait", StringComparison.Ordinal)));

        var missing = Model(
            Record("product", "product/library", "Library", "approved"),
            Record(
                "requirement",
                "requirement/borrowing.max-wait",
                "Max Wait",
                "approved",
                new JsonObject { ["cites"] = Links("feature/self-checkout") }));

        Assert.Null(BookParents.ParentOf(missing, "requirement/borrowing.max-wait"));
        Assert.Contains("requirement/borrowing.max-wait", BookOutline.Build(missing).Unplaced);
    }

    /// <summary>A scenario parents only under the result it shows; a missing one is reported, never defaulted to the use case.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Scenario_Parents_Only_Under_Its_Shown_Result()
    {
        var shown = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record("use-case", "use-case/lending.borrow-copy", "Borrow Copy", "reviewed"),
            Record("result", "result/borrow-copy.borrowed", "Borrowed", "reviewed"),
            Record(
                "scenario",
                "scenario/borrow-copy.borrowing-succeeds",
                "Borrowing Succeeds",
                "reviewed",
                new JsonObject { ["result"] = Links("result/borrow-copy.borrowed") }));

        Assert.Equal("result/borrow-copy.borrowed", BookParents.ParentOf(shown, "scenario/borrow-copy.borrowing-succeeds"));
        var placed = BookOutline.Build(shown);
        Assert.DoesNotContain("scenario/borrow-copy.borrowing-succeeds", placed.Unplaced);
        var result = Find(placed.Layers, "result/borrow-copy.borrowed");
        Assert.NotNull(result);
        Assert.True(result!.Children.Any(child => string.Equals(child.Id, "scenario/borrow-copy.borrowing-succeeds", StringComparison.Ordinal)));

        var missing = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record("use-case", "use-case/lending.borrow-copy", "Borrow Copy", "reviewed"),
            Record(
                "scenario",
                "scenario/borrow-copy.borrowing-succeeds",
                "Borrowing Succeeds",
                "reviewed",
                new JsonObject { ["result"] = Links("result/borrow-copy.borrowed") }));

        Assert.Null(BookParents.ParentOf(missing, "scenario/borrow-copy.borrowing-succeeds"));
        var outline = BookOutline.Build(missing);
        Assert.Contains("scenario/borrow-copy.borrowing-succeeds", outline.Unplaced);
        Assert.Null(Find(outline.Layers, "scenario/borrow-copy.borrowing-succeeds"));
    }

    /// <summary>One code node per implementing file sits under the first mark target in identifier order.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Implementation_Nodes_Sit_Under_Their_First_Mark_Target()
    {
        var model = FullModel(
            [
                Record("product", "product/library", "Library", "approved"),
                Record("module", "module/lending", "Lending", "reviewed"),
                Record("use-case", "use-case/lending.borrow-copy", "Borrow Copy", "reviewed"),
                Record("external-system", "external-system/address-lookup", "Address Lookup", "reviewed"),
            ],
            links: new JsonObject
            {
                ["implements"] = new JsonArray(
                    new JsonObject
                    {
                        ["path"] = "src/App/Ports.cs",
                        ["to"] = "use-case/lending.borrow-copy",
                        ["kind"] = "port",
                    },
                    new JsonObject
                    {
                        ["path"] = "src/App/Ports.cs",
                        ["to"] = "external-system/address-lookup",
                        ["kind"] = "port",
                    }),
            });

        var outline = BookOutline.Build(model);

        // external-system/address-lookup sorts before use-case/lending.borrow-copy, so it parents the file.
        var system = Find(outline.Layers, "external-system/address-lookup");
        Assert.NotNull(system);
        var code = system!.Children.SingleOrDefault(child => string.Equals(child.Id, "code/src/App/Ports.cs", StringComparison.Ordinal));
        Assert.NotNull(code);
        Assert.Equal("code", code!.Kind);
        Assert.Equal("code", code.Type);
        Assert.Equal("Ports.cs", code.Label);
        Assert.Null(code.Status);
        Assert.Empty(code.Children);

        var useCase = Find(outline.Layers, "use-case/lending.borrow-copy");
        Assert.NotNull(useCase);
        Assert.DoesNotContain(useCase!.Children, child => string.Equals(child.Id, "code/src/App/Ports.cs", StringComparison.Ordinal));
    }

    /// <summary>One test node per unit and browser test sits under the lowest record it proves.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Proof_Tests_Sit_Under_The_Lowest_Proved_Record()
    {
        var model = FullModel(
            [
                Record("product", "product/library", "Library", "approved"),
                Record("module", "module/lending", "Lending", "reviewed"),
                Record("use-case", "use-case/lending.borrow-copy", "Borrow Copy", "reviewed"),
                Record("result", "result/borrow-copy.borrowed", "Borrowed", "reviewed"),
                Record(
                    "scenario",
                    "scenario/borrow-copy.borrowing-succeeds",
                    "Borrowing Succeeds",
                    "reviewed",
                    new JsonObject { ["result"] = Links("result/borrow-copy.borrowed") }),
                Record("rule", "rule/lending.one-loan-per-copy", "One Loan Per Copy", "reviewed"),
            ],
            tests: new JsonArray(
                new JsonObject
                {
                    ["scenario"] = "Library.Tests.BorrowTests.Borrowing_Succeeds",
                    ["class"] = "BorrowTests",
                    ["file"] = "tests/BorrowTests.cs",
                    ["line"] = 41,
                    ["claims"] = new JsonArray(
                        "scenario/borrow-copy.borrowing-succeeds",
                        "rule/lending.one-loan-per-copy"),
                }),
            browserTests: new JsonArray(
                new JsonObject
                {
                    ["frontend"] = "catalog",
                    ["title"] = "Borrowing works",
                    ["file"] = "apps/catalog/tests/borrow.spec.ts",
                    ["line"] = 7,
                    ["proves"] = new JsonArray("rule/lending.one-loan-per-copy"),
                }));

        var outline = BookOutline.Build(model);

        // rule/lending.one-loan-per-copy sorts before the scenario, so it parents the unit test.
        var rule = Find(outline.Layers, "rule/lending.one-loan-per-copy");
        Assert.NotNull(rule);
        var unit = rule!.Children.SingleOrDefault(child => string.Equals(child.Id, "test/Library.Tests.BorrowTests.Borrowing_Succeeds", StringComparison.Ordinal));
        Assert.NotNull(unit);
        Assert.Equal("test", unit!.Kind);
        Assert.Equal("test", unit.Type);
        Assert.Equal("Borrowing Succeeds", unit.Label);

        var browser = rule.Children.SingleOrDefault(child => string.Equals(child.Id, "test/apps/catalog/tests/borrow.spec.ts:7", StringComparison.Ordinal));
        Assert.NotNull(browser);
        Assert.Equal("test", browser!.Kind);
        Assert.Equal("Borrowing works", browser.Label);

        var scenario = Find(outline.Layers, "scenario/borrow-copy.borrowing-succeeds");
        Assert.NotNull(scenario);
        Assert.DoesNotContain(scenario!.Children, child => string.Equals(child.Kind, "test", StringComparison.Ordinal));
    }

    /// <summary>One finding node per open and known finding sits under its subject; unknown subjects place nothing.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Findings_Sit_Under_Their_Subject()
    {
        var model = FullModel(
            [
                Record("product", "product/library", "Library", "approved"),
                Record("module", "module/lending", "Lending", "reviewed"),
                Record("use-case", "use-case/lending.borrow-copy", "Borrow Copy", "reviewed"),
            ],
            findings: new JsonArray(
                new JsonObject
                {
                    ["code"] = "use-case-unimplemented",
                    ["tier"] = "G",
                    ["message"] = "Use case 'use-case/lending.borrow-copy' has no handler or automation.",
                    ["subject"] = "use-case/lending.borrow-copy",
                    ["path"] = "docs/borrow-copy.md",
                    ["line"] = 3,
                },
                new JsonObject
                {
                    ["code"] = "stray",
                    ["message"] = "Nowhere.",
                    ["subject"] = "use-case/missing.nope",
                }),
            known: new JsonArray(
                new JsonObject
                {
                    ["code"] = "step-screen-missing",
                    ["tier"] = "A",
                    ["message"] = "Step s2 has no screen.",
                    ["subject"] = "use-case/lending.borrow-copy#s2",
                    ["outcome"] = "accepted",
                    ["reason"] = "Later.",
                }));

        var outline = BookOutline.Build(model);

        var useCase = Find(outline.Layers, "use-case/lending.borrow-copy");
        Assert.NotNull(useCase);
        var open = useCase!.Children.SingleOrDefault(child => string.Equals(child.Id, "finding/1", StringComparison.Ordinal));
        Assert.NotNull(open);
        Assert.Equal("finding", open!.Kind);
        Assert.Equal("finding", open.Type);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' has no handler or automation.", open.Label);
        var accepted = useCase.Children.SingleOrDefault(child => string.Equals(child.Id, "finding/3", StringComparison.Ordinal));
        Assert.NotNull(accepted);
        Assert.Equal("finding", accepted!.Kind);

        Assert.Null(Find(outline.Layers, "finding/2"));
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

    /// <summary>An automation parents its module by identifier home and sorts with the module children.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Automation_Parents_Its_Module_By_Identifier_Home()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record("aggregate", "aggregate/lending.loan", "Loan", "reviewed"),
            Record("use-case", "use-case/lending.borrow-copy", "Borrow Copy", "reviewed", aggregate: "aggregate/lending.loan"),
            Record("automation", "automation/lending.send-due-reminder-on-loan-due", "Send Due Reminder", "reviewed"),
            Record("notification", "notification/lending.due-reminder", "Due Reminder", "reviewed"));

        Assert.Equal("module/lending", BookParents.ParentOf(model, "automation/lending.send-due-reminder-on-loan-due"));

        var outline = BookOutline.Build(model);
        Assert.DoesNotContain("automation/lending.send-due-reminder-on-loan-due", outline.Unplaced);
        var module = Find(outline.Layers, "module/lending");
        Assert.NotNull(module);
        Assert.Equal(
            new[] { "aggregate", "automation", "notification" },
            module!.Children
                .Where(child => string.Equals(child.Kind, "record", StringComparison.Ordinal))
                .Select(child => child.Type)
                .ToList());
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
            Record("topic", "topic/borrowing", "Borrowing", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record("term", "term/loan", "Loan", "approved"),
            Record("deviation", "deviation/kiosk-cache", "Kiosk Cache", "approved"));

        Assert.Null(BookParents.ParentOf(model, "product/library"));
        Assert.Equal("product/library", BookParents.ParentOf(model, "goal/faster-borrowing"));
        Assert.Equal("product/library", BookParents.ParentOf(model, "topic/borrowing"));
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

    static JsonObject FullModel(
        JsonObject[] records,
        JsonObject? code = null,
        JsonObject? links = null,
        JsonArray? tests = null,
        JsonArray? browserTests = null,
        JsonArray? findings = null,
        JsonArray? known = null)
    {
        var array = new JsonArray();
        foreach (var record in records)
        {
            array.Add(record);
        }

        return new JsonObject
        {
            ["records"] = array,
            ["code"] = code ?? new JsonObject(),
            ["links"] = links ?? new JsonObject(),
            ["tests"] = tests ?? new JsonArray(),
            ["browserTests"] = browserTests ?? new JsonArray(),
            ["findings"] = findings ?? new JsonArray(),
            ["known"] = known ?? new JsonArray(),
        };
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
