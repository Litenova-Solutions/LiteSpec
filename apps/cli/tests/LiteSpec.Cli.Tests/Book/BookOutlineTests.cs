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

    /// <summary>The C# parent table matches the Placement Table on the model page.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Parent_Rules_Match_The_Model_Page()
    {
        var table = ReadPlacementTable();
        Assert.Equal(BookParents.Rules.Count, table.Count);
        foreach (var pair in BookParents.Rules)
        {
            Assert.True(table.TryGetValue(pair.Key, out var row), $"The Placement Table names no '{pair.Key}'.");
            Assert.Equal(pair.Value.ParentTypes, row.Parents);
            Assert.Equal(pair.Value.FoundThrough, row.ParentCell);
        }
    }

    /// <summary>The fixture model places every record exactly once under six layer roots, with none unplaced.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Fixture_Model_Places_Every_Record_Exactly_Once()
    {
        var root = DataFile.Read(Cli.Fixture("Book/review.json"))!;
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

        Assert.Empty(outline.Unplaced);

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

    /// <summary>A feature parents its module by file; its solves and advances entries never move it, and a feature with no module file stays unplaced.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Feature_Parents_Its_Module_By_File()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record("problem", "problem/slow-queues", "Slow Queues", "approved"),
            Record(
                "feature",
                "feature/self-checkout",
                "Self Checkout",
                "approved",
                new JsonObject
                {
                    ["solves"] = Links("problem/slow-queues"),
                    ["advances"] = Links("goal/faster-borrowing"),
                },
                file: ".litespec/intent/modules/lending/features/self-checkout.yaml"));

        var outline = BookOutline.Build(model);

        Assert.Equal("module/lending", outline.ParentOf["feature/self-checkout"]);
        var placed = RecordIds(outline.Layers);
        Assert.Equal(1, placed.Count(id => string.Equals(id, "feature/self-checkout", StringComparison.Ordinal)));
        Assert.DoesNotContain("feature/self-checkout", outline.Unplaced);

        var module = Find(outline.Layers, "module/lending");
        Assert.NotNull(module);
        Assert.Contains(module!.Children, child => string.Equals(child.Id, "feature/self-checkout", StringComparison.Ordinal));

        var homeless = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record(
                "feature",
                "feature/self-checkout",
                "Self Checkout",
                "approved",
                new JsonObject { ["solves"] = Links("problem/slow-queues") }));

        Assert.Null(BookParents.ParentOf(homeless, "feature/self-checkout"));
        Assert.Contains("feature/self-checkout", BookOutline.Build(homeless).Unplaced);
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
            Record("requirement", "requirement/nowhere.orphan-need", "Orphan Need", "draft"),
            Record("screen", "screen/kiosk.lone", "Lone", "draft"));

        var outline = BookOutline.Build(model);

        Assert.Null(outline.ParentOf["feature/orphan"]);
        Assert.Null(outline.ParentOf["requirement/nowhere.orphan-need"]);
        Assert.Null(outline.ParentOf["screen/kiosk.lone"]);
        Assert.Contains("feature/orphan", outline.Unplaced);
        Assert.Contains("requirement/nowhere.orphan-need", outline.Unplaced);
        Assert.Contains("screen/kiosk.lone", outline.Unplaced);
        Assert.DoesNotContain("product/library", outline.Unplaced);

        var placed = RecordIds(outline.Layers);
        Assert.DoesNotContain("feature/orphan", placed);
        Assert.DoesNotContain("requirement/nowhere.orphan-need", placed);
        Assert.DoesNotContain("screen/kiosk.lone", placed);
        Assert.Null(Find(outline.Layers, "feature/orphan"));
        Assert.Null(Find(outline.Layers, "requirement/nowhere.orphan-need"));
        Assert.Null(Find(outline.Layers, "screen/kiosk.lone"));

        Assert.Null(Find(outline.Layers, "intent/feature"));
        Assert.Null(Find(outline.Layers, "intent/requirement"));
        Assert.Null(Find(outline.Layers, "behavior/screen"));
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

    /// <summary>A requirement parents its feature by identifier home; its cites never move it.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Requirement_Parents_Its_Feature_By_Identifier_Home()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record(
                "feature",
                "feature/self-checkout",
                "Self Checkout",
                "approved",
                file: ".litespec/intent/modules/lending/features/self-checkout.yaml"),
            Record(
                "requirement",
                "requirement/self-checkout.max-wait",
                "Max Wait",
                "approved",
                new JsonObject { ["cites"] = Links("goal/faster-borrowing") }));

        Assert.Equal("feature/self-checkout", BookParents.ParentOf(model, "requirement/self-checkout.max-wait"));

        var outline = BookOutline.Build(model);
        Assert.DoesNotContain("requirement/self-checkout.max-wait", outline.Unplaced);
        var feature = Find(outline.Layers, "feature/self-checkout");
        Assert.NotNull(feature);
        Assert.Contains(feature!.Children, child => string.Equals(child.Id, "requirement/self-checkout.max-wait", StringComparison.Ordinal));

        var missing = Model(
            Record("product", "product/library", "Library", "approved"),
            Record(
                "requirement",
                "requirement/self-checkout.max-wait",
                "Max Wait",
                "approved",
                new JsonObject { ["cites"] = Links("feature/self-checkout") }));

        Assert.Null(BookParents.ParentOf(missing, "requirement/self-checkout.max-wait"));
        Assert.Contains("requirement/self-checkout.max-wait", BookOutline.Build(missing).Unplaced);
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
        Assert.Contains(result!.Children, child => string.Equals(child.Id, "scenario/borrow-copy.borrowing-succeeds", StringComparison.Ordinal));

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
                    ["message"] = "Use case 'use-case/lending.borrow-copy' has no handler, reaction or job.",
                    ["subject"] = "use-case/lending.borrow-copy",
                    ["path"] = ".litespec/borrow-copy.yaml",
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
        Assert.Equal("Use case 'use-case/lending.borrow-copy' has no handler, reaction or job.", open.Label);
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
        Assert.Contains(loan!.Children, child => string.Equals(child.Id, "use-case/lending.borrow-copy", StringComparison.Ordinal));
    }

    /// <summary>A reaction and a job parent their module by identifier home and sort with the module children.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Reactions_And_Jobs_Parent_Their_Module_By_Identifier_Home()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record("aggregate", "aggregate/lending.loan", "Loan", "reviewed"),
            Record("use-case", "use-case/lending.borrow-copy", "Borrow Copy", "reviewed", aggregate: "aggregate/lending.loan"),
            Record("reaction", "reaction/lending.send-due-reminder-on-loan-due", "Send Due Reminder", "reviewed"),
            Record("job", "job/lending.send-overdue-notices", "Send Overdue Notices", "reviewed"),
            Record("notification", "notification/lending.due-reminder", "Due Reminder", "reviewed"));

        Assert.Equal("module/lending", BookParents.ParentOf(model, "reaction/lending.send-due-reminder-on-loan-due"));
        Assert.Equal("module/lending", BookParents.ParentOf(model, "job/lending.send-overdue-notices"));

        var outline = BookOutline.Build(model);
        Assert.DoesNotContain("reaction/lending.send-due-reminder-on-loan-due", outline.Unplaced);
        Assert.DoesNotContain("job/lending.send-overdue-notices", outline.Unplaced);
        var module = Find(outline.Layers, "module/lending");
        Assert.NotNull(module);
        Assert.Equal(
            new[] { "aggregate", "reaction", "job", "notification" },
            module!.Children
                .Where(child => string.Equals(child.Kind, "record", StringComparison.Ordinal))
                .Select(child => child.Type)
                .ToList());
    }

    /// <summary>A screen parents its entry point by identifier home; a home naming no entry point stays unplaced.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Screen_Parents_Its_Entry_Point_By_Identifier_Home()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("entry-point", "entry-point/library-app", "Library App", "reviewed"),
            Record("screen", "screen/library-app.expense-list", "Expense List", "reviewed"));

        Assert.Equal("entry-point/library-app", BookParents.ParentOf(model, "screen/library-app.expense-list"));
        var outline = BookOutline.Build(model);
        Assert.DoesNotContain("screen/library-app.expense-list", outline.Unplaced);
        var entryPoint = Find(outline.Layers, "entry-point/library-app");
        Assert.NotNull(entryPoint);
        Assert.Contains(entryPoint!.Children, child => string.Equals(child.Id, "screen/library-app.expense-list", StringComparison.Ordinal));

        var missing = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("entry-point", "entry-point/library-app", "Library App", "reviewed"),
            Record("screen", "screen/kiosk.expense-list", "Expense List", "reviewed"));

        Assert.Null(BookParents.ParentOf(missing, "screen/kiosk.expense-list"));
        Assert.Contains("screen/kiosk.expense-list", BookOutline.Build(missing).Unplaced);
    }

    /// <summary>Root types parent the product with no written field, and the product has no parent.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Root_Types_Parent_The_Product()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("goal", "goal/faster-borrowing", "Faster Borrowing", "approved"),
            Record("journey", "journey/borrowing", "Borrowing", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record("term", "term/loan", "Loan", "approved"),
            Record("departure", "departure/kiosk-cache", "Kiosk Cache", "approved"));

        Assert.Null(BookParents.ParentOf(model, "product/library"));
        Assert.Equal("product/library", BookParents.ParentOf(model, "goal/faster-borrowing"));
        Assert.Equal("product/library", BookParents.ParentOf(model, "journey/borrowing"));
        Assert.Equal("product/library", BookParents.ParentOf(model, "module/lending"));
        Assert.Equal("product/library", BookParents.ParentOf(model, "term/loan"));
        Assert.Equal("product/library", BookParents.ParentOf(model, "departure/kiosk-cache"));

        var outline = BookOutline.Build(model);
        Assert.DoesNotContain("goal/faster-borrowing", outline.Unplaced);
        Assert.DoesNotContain("module/lending", outline.Unplaced);
    }

    /// <summary>A goal in a module folder parents that module; a goal at product level parents the product.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Goal_Parents_Its_Module_File_Else_The_Product()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record(
                "goal",
                "goal/faster-borrowing",
                "Faster Borrowing",
                "approved",
                file: ".litespec/intent/modules/lending/goals/faster-borrowing.yaml"),
            Record("goal", "goal/quiet-study", "Quiet Study", "approved"));

        Assert.Equal("module/lending", BookParents.ParentOf(model, "goal/faster-borrowing"));
        Assert.Equal("product/library", BookParents.ParentOf(model, "goal/quiet-study"));

        var outline = BookOutline.Build(model);
        Assert.DoesNotContain("goal/faster-borrowing", outline.Unplaced);
        var module = Find(outline.Layers, "module/lending");
        Assert.NotNull(module);
        Assert.Contains(module!.Children, child => string.Equals(child.Id, "goal/faster-borrowing", StringComparison.Ordinal));
    }

    /// <summary>A rule whose home names no aggregate or module parents the product as a product-level invariant.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Product_Level_Rule_Parents_The_Product()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record("rule", "rule/everything.shelf-marks-face-out", "Shelf Marks Face Out", "reviewed"));

        Assert.Equal("product/library", BookParents.ParentOf(model, "rule/everything.shelf-marks-face-out"));
        Assert.DoesNotContain("rule/everything.shelf-marks-face-out", BookOutline.Build(model).Unplaced);
    }

    /// <summary>A use case parents its folder node: the aggregate beside its file, or its module from the file when its home names another module.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Use_Case_Parents_Its_Aggregate_Folder()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record("module", "module/copies", "Copies", "reviewed"),
            Record(
                "aggregate",
                "aggregate/lending.loan",
                "Loan",
                "reviewed",
                file: ".litespec/behavior/modules/lending/loans/aggregate.yaml"),
            Record(
                "use-case",
                "use-case/lending.borrow-copy",
                "Borrow Copy",
                "reviewed",
                file: ".litespec/behavior/modules/lending/loans/borrow-copy.yaml"),
            Record(
                "use-case",
                "use-case/copies.list-loans",
                "List Loans",
                "reviewed",
                file: ".litespec/behavior/modules/lending/use-cases/list-loans.yaml"));

        Assert.Equal("aggregate/lending.loan", BookParents.ParentOf(model, "use-case/lending.borrow-copy"));
        Assert.Equal("module/lending", BookParents.ParentOf(model, "use-case/copies.list-loans"));

        var outline = BookOutline.Build(model);
        Assert.DoesNotContain("use-case/lending.borrow-copy", outline.Unplaced);
        Assert.DoesNotContain("use-case/copies.list-loans", outline.Unplaced);
        var loan = Find(outline.Layers, "aggregate/lending.loan");
        Assert.NotNull(loan);
        Assert.Contains(loan!.Children, child => string.Equals(child.Id, "use-case/lending.borrow-copy", StringComparison.Ordinal));
        var module = Find(outline.Layers, "module/lending");
        Assert.NotNull(module);
        Assert.Contains(module!.Children, child => string.Equals(child.Id, "use-case/copies.list-loans", StringComparison.Ordinal));
    }

    /// <summary>A rule parents its folder node: the aggregate above its rules folder, its module from the file, or the product under behavior rules.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Rule_Parents_Its_Folder_Node()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record(
                "aggregate",
                "aggregate/lending.loan",
                "Loan",
                "reviewed",
                file: ".litespec/behavior/modules/lending/loans/aggregate.yaml"),
            Record(
                "rule",
                "rule/lending.discount-rounds-down",
                "Discount Rounds Down",
                "reviewed",
                file: ".litespec/behavior/modules/lending/loans/rules/discount-rounds-down.yaml"),
            Record(
                "rule",
                "rule/loan.shelf-marks-face-out",
                "Shelf Marks Face Out",
                "reviewed",
                file: ".litespec/behavior/modules/lending/rules/shelf-marks-face-out.yaml"),
            Record(
                "rule",
                "rule/everything.cross-module-cap",
                "Cross Module Cap",
                "reviewed",
                file: ".litespec/behavior/rules/cross-module-cap.yaml"));

        Assert.Equal("aggregate/lending.loan", BookParents.ParentOf(model, "rule/lending.discount-rounds-down"));
        Assert.Equal("module/lending", BookParents.ParentOf(model, "rule/loan.shelf-marks-face-out"));
        Assert.Equal("product/library", BookParents.ParentOf(model, "rule/everything.cross-module-cap"));

        var outline = BookOutline.Build(model);
        Assert.DoesNotContain("rule/lending.discount-rounds-down", outline.Unplaced);
        Assert.DoesNotContain("rule/loan.shelf-marks-face-out", outline.Unplaced);
        Assert.DoesNotContain("rule/everything.cross-module-cap", outline.Unplaced);
    }

    /// <summary>A screen parents its entry-point folder: the entry point beside its screens folder, or the one its folder names.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Screen_Parents_Its_Entry_Point_Folder()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record(
                "entry-point",
                "entry-point/kiosk",
                "Kiosk",
                "reviewed",
                file: ".litespec/behavior/entry-points/kiosk/entry-point.yaml"),
            Record("entry-point", "entry-point/library-app", "Library App", "reviewed"),
            Record(
                "screen",
                "screen/other.search",
                "Search",
                "reviewed",
                file: ".litespec/behavior/entry-points/kiosk/screens/search.yaml"),
            Record(
                "screen",
                "screen/other.list",
                "List",
                "reviewed",
                file: ".litespec/behavior/entry-points/library-app/screens/list.yaml"));

        Assert.Equal("entry-point/kiosk", BookParents.ParentOf(model, "screen/other.search"));
        Assert.Equal("entry-point/library-app", BookParents.ParentOf(model, "screen/other.list"));

        var outline = BookOutline.Build(model);
        Assert.DoesNotContain("screen/other.search", outline.Unplaced);
        Assert.DoesNotContain("screen/other.list", outline.Unplaced);
        var kiosk = Find(outline.Layers, "entry-point/kiosk");
        Assert.NotNull(kiosk);
        Assert.Contains(kiosk!.Children, child => string.Equals(child.Id, "screen/other.search", StringComparison.Ordinal));
    }

    /// <summary>An aggregate and a reaction parent their module folder when their home names another module.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Aggregate_And_Module_Members_Parent_Their_Module_Folder()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record("module", "module/copies", "Copies", "reviewed"),
            Record(
                "aggregate",
                "aggregate/copies.loan",
                "Loan",
                "reviewed",
                file: ".litespec/behavior/modules/lending/loans/aggregate.yaml"),
            Record(
                "reaction",
                "reaction/copies.send-due-reminder",
                "Send Due Reminder",
                "reviewed",
                file: ".litespec/behavior/modules/lending/reactions/send-due-reminder.yaml"));

        Assert.Equal("module/lending", BookParents.ParentOf(model, "aggregate/copies.loan"));
        Assert.Equal("module/lending", BookParents.ParentOf(model, "reaction/copies.send-due-reminder"));

        var outline = BookOutline.Build(model);
        Assert.DoesNotContain("aggregate/copies.loan", outline.Unplaced);
        Assert.DoesNotContain("reaction/copies.send-due-reminder", outline.Unplaced);
    }

    /// <summary>An event parents the aggregate whose folder holds its file, even when its home names no aggregate.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Event_Parents_Its_Aggregate_Folder()
    {
        var model = Model(
            Record("product", "product/library", "Library", "approved"),
            Record("module", "module/lending", "Lending", "reviewed"),
            Record(
                "aggregate",
                "aggregate/lending.loan",
                "Loan",
                "reviewed",
                file: ".litespec/behavior/modules/lending/loans/aggregate.yaml"),
            Record(
                "event",
                "event/other.loan-borrowed",
                "Loan Borrowed",
                "reviewed",
                file: ".litespec/behavior/modules/lending/loans/events/loan-borrowed.yaml"));

        Assert.Equal("aggregate/lending.loan", BookParents.ParentOf(model, "event/other.loan-borrowed"));

        var outline = BookOutline.Build(model);
        Assert.DoesNotContain("event/other.loan-borrowed", outline.Unplaced);
        var loan = Find(outline.Layers, "aggregate/lending.loan");
        Assert.NotNull(loan);
        Assert.Contains(loan!.Children, child => string.Equals(child.Id, "event/other.loan-borrowed", StringComparison.Ordinal));
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
            ["covered"] = known ?? new JsonArray(),
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

    static Dictionary<string, (List<string> Parents, string ParentCell)> ReadPlacementTable()
    {
        var lines = File.ReadAllLines(Path.Combine(Cli.Repository, "docs", "core", "model.md"));
        var start = -1;
        for (var index = 0; index < lines.Length; index++)
        {
            if (string.Equals(lines[index].Trim(), "### The Placement Table", StringComparison.Ordinal))
            {
                start = index;
                break;
            }
        }

        Assert.True(start >= 0, "docs/core/model.md holds no ### The Placement Table section.");

        var header = -1;
        for (var index = start; index < lines.Length; index++)
        {
            if (lines[index].Contains("| Type |", StringComparison.Ordinal)
                && lines[index].Contains("| Parent |", StringComparison.Ordinal)
                && lines[index].Contains("Qualified Form", StringComparison.Ordinal))
            {
                header = index;
                break;
            }
        }

        Assert.True(header >= 0, "The ### The Placement Table section holds no placement table.");

        var heads = lines[header].Split('|').Select(cell => cell.Trim()).ToList();
        var typeAt = heads.IndexOf("Type");
        var parentAt = heads.IndexOf("Parent");
        Assert.True(typeAt >= 0 && parentAt >= 0, "The placement table names no Type or Parent column.");

        var table = new Dictionary<string, (List<string> Parents, string ParentCell)>(StringComparer.Ordinal);
        for (var index = header + 2; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (!line.StartsWith('|'))
            {
                break;
            }

            var cells = line.Split('|').Select(cell => cell.Trim()).ToList();
            Assert.True(cells.Count > Math.Max(typeAt, parentAt), $"Placement table row {index + 1} holds too few cells: {line}.");
            var type = cells[typeAt].Trim('`', ' ', '\t');
            var parentCell = cells[parentAt];
            Assert.True(type.Length > 0, $"Placement table row {index + 1} names no record type.");

            Assert.True(!table.ContainsKey(type), $"The placement table names '{type}' twice.");
            table[type] = (ParentsFor(type, parentCell, index), parentCell);
        }

        return table;
    }

    static List<string> ParentsFor(string type, string parentCell, int index)
    {
        // The folder node depends on the type: a rule sits under an aggregate, a
        // module or the product, and an external system under a module or the product.
        if (string.Equals(parentCell, "its folder node", StringComparison.Ordinal))
        {
            return string.Equals(type, "rule", StringComparison.Ordinal)
                ? ["aggregate", "module", "product"]
                : ["product", "module"];
        }

        var parents = new List<string>();
        if (!string.Equals(parentCell, "none", StringComparison.Ordinal))
        {
            foreach (var part in parentCell.Split(" or ", StringSplitOptions.TrimEntries))
            {
                Assert.True(part.Length > 0, $"Placement table row {index + 1} names no parent: {parentCell}.");
                parents.Add(part.Replace(' ', '-'));
            }
        }

        return parents;
    }
}
