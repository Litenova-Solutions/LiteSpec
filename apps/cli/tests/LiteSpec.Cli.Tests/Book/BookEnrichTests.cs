using System.Text.Json.Nodes;
using LiteSpec.Book;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Book;

/// <summary>Proves the book enrich step emits computed record details from the model only.</summary>
public sealed class BookEnrichTests
{
    /// <summary>Each detail keeps the mockup shape: identity, status, stripped fields and source.</summary>
    /// <proves>litespec/core/model#point-every-book-entry-to-its-source</proves>
    [Fact]
    public void Build_Keeps_The_Mockup_Record_Shape()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        var loan = details["aggregate/lending.loan"];
        Assert.Equal("aggregate", Str(loan, "type"));
        Assert.Equal("Loan", Str(loan, "title"));
        Assert.Equal("lending-team", Str(loan, "owner"));
        Assert.Equal("reviewed", Str(loan, "status"));
        Assert.Equal("approved", Str(loan, "effective"));
        var fields = Obj(loan, "fields");
        Assert.Null(fields["type"]);
        Assert.Null(fields["id"]);
        Assert.Null(fields["title"]);
        Assert.Null(fields["owner"]);
        Assert.Null(fields["status"]);
        Assert.NotNull(fields["summary"]);
        var src = Obj(loan, "src");
        Assert.Equal("docs/behavior/modules/lending/loans/README.md", Str(src, "path"));
        Assert.Equal(3, src["line"]?.GetValue<int>());
    }

    /// <summary>Every parent matches the outline parent for the same record.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Parent_Matches_The_Outline_Parent_For_Every_Record()
    {
        var model = LoadFixture("Book/review.json");
        var outline = BookOutline.Build(model);
        var details = BookEnrich.Build(model, outline);
        foreach (var pair in details)
        {
            var expected = outline.ParentOf.TryGetValue(pair.Key, out var parent) ? parent : null;
            Assert.Equal(expected, Str(pair.Value, "parent"));
        }
    }

    /// <summary>Layers follow the outline roots, one of the six known layer keys.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Layer_Follows_The_Outline_Layers()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        Assert.Equal("intent", Str(details["goal/faster-borrowing"], "layer"));
        Assert.Equal("behavior", Str(details["use-case/lending.borrow-copy"], "layer"));
        foreach (var pair in details)
        {
            var layer = Str(pair.Value, "layer");
            Assert.True(
                layer is null || layer is "intent" or "behavior" or "implementation" or "proof" or "governance" or "glossary",
                $"Unexpected layer {layer} for {pair.Key}.");
        }
    }

    /// <summary>Scenarios and rules read norun while the model holds no test run.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Proof_Is_Norun_Without_A_Test_Run()
    {
        var model = LoadFixture("Book/review.json");
        model["testRun"] = null;
        var details = DetailsOf(model);
        foreach (var id in new[]
        {
            "scenario/borrow-copy.borrowing-an-available-copy-succeeds",
            "scenario/borrow-copy.borrowing-a-loaned-copy-is-refused",
            "rule/loan.one-loan-per-copy",
            "rule/loan.late-fee",
        })
        {
            Assert.Equal("norun", Str(Obj(details[id], "proof"), "state"));
        }
    }

    /// <summary>Proof maps passing and unproved states, with the proving test file and line.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Proof_Maps_Passing_And_Unproved_With_Test_Lines()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        var passing = Obj(details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"], "proof");
        Assert.Equal("passing", Str(passing, "state"));
        Assert.Equal(1, passing["tests"]?.GetValue<int>());
        var test = Obj(passing, "test");
        Assert.Equal("Borrowing An Available Copy Succeeds", Str(test, "name"));
        Assert.Equal("tests/BorrowTests.cs", Str(test, "path"));
        Assert.Equal(41, test["line"]?.GetValue<int>());
        var unproved = Obj(details["scenario/borrow-copy.borrowing-a-loaned-copy-is-refused"], "proof");
        Assert.Equal("unproved", Str(unproved, "state"));
        Assert.Equal(0, unproved["tests"]?.GetValue<int>());
        Assert.Null(unproved["test"]);
        Assert.Null(details["use-case/lending.borrow-copy"]["proof"]);
    }

    /// <summary>Machine states come from the status maps the builder already writes.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Machine_States_Come_From_Status_Maps()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        var borrow = Obj(details["use-case/lending.borrow-copy"], "machine");
        Assert.True(BoolOf(borrow["implemented"]));
        Assert.False(BoolOf(borrow["verified"]));
        var returned = Obj(details["use-case/lending.return-copy"], "machine");
        Assert.False(BoolOf(returned["implemented"]));
        var shelf = Obj(details["requirement/borrowing.copy-shelf-home"], "machine");
        Assert.True(BoolOf(shelf["verified"]));
        Assert.Null(shelf["measurePassed"]);
        var wait = Obj(details["requirement/borrowing.max-wait"], "machine");
        Assert.False(BoolOf(wait["verified"]));
        Assert.Equal(false, wait["measurePassed"]?.GetValue<bool>());
        Assert.Null(details["goal/faster-borrowing"]["machine"]);
    }

    /// <summary>Aggregates list their reachable states; other types carry no reachable list.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Reachable_Lists_Aggregate_States()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        var reachable = Arr(details["aggregate/lending.loan"], "reachable");
        Assert.Equal(2, reachable.Count);
        Assert.True(HasString(reachable, "OnShelf"));
        Assert.True(HasString(reachable, "OnLoan"));
        Assert.Null(details["use-case/lending.borrow-copy"]["reachable"]);
    }

    /// <summary>Requirements count the fulfilling records by type.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void KeptBy_Counts_Fulfilling_Types()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        var kept = Obj(details["requirement/borrowing.max-wait"], "keptBy");
        Assert.Equal(1, kept["use-case"]?.GetValue<int>());
        Assert.Equal(1, kept["automation"]?.GetValue<int>());
        Assert.Equal(1, kept["notification"]?.GetValue<int>());
        Assert.Null(details["goal/faster-borrowing"]["keptBy"]);
    }

    /// <summary>Journey steps list their servers and screens, and the steps a test walked.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Journey_Steps_List_Servers_Screens_And_Exercised()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        var journey = details["journey/borrowing"];
        var servers = Obj(journey, "stepServers");
        Assert.True(HasString(servers["s1"] as JsonArray ?? new JsonArray(), "use-case/lending.borrow-copy"));
        Assert.Empty(servers["s2"] as JsonArray ?? new JsonArray());
        Assert.Empty(Obj(journey, "stepScreens")["s1"] as JsonArray ?? new JsonArray());
        Assert.Empty(Arr(journey, "exercised"));
        Assert.Null(details["goal/faster-borrowing"]["stepServers"]);
    }

    /// <summary>An automation serving a step reads as its server, and a walked step reads walked.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Automation_Served_And_Exercised_Steps()
    {
        var model = LoadFixture("Book/review.json");
        var automationId = "";
        foreach (var record in model["records"] as JsonArray ?? new JsonArray())
        {
            if (record is JsonObject obj && Str(obj, "type") == "automation" && automationId.Length == 0)
            {
                automationId = Str(obj, "id") ?? "";
                var fields = obj["fields"] as JsonObject ?? new JsonObject();
                fields["serves"] = new JsonArray("journey/borrowing#s2");
            }
        }

        Assert.True(automationId.Length > 0, "The fixture holds no automation.");
        model["exercisedSteps"] = new JsonArray("journey/borrowing#s1");
        var journey = DetailsOf(model)["journey/borrowing"];
        var servers = Obj(journey, "stepServers");
        Assert.True(HasString(servers["s2"] as JsonArray ?? new JsonArray(), automationId));
        var exercised = Arr(journey, "exercised");
        Assert.True(HasString(exercised, "s1"));
        Assert.False(HasString(exercised, "s2"));
    }

    /// <summary>Screens carry their route, called use cases and a route implementation entry.</summary>
    /// <proves>litespec/core/model#point-every-book-entry-to-its-source</proves>
    [Fact]
    public void Screen_Route_And_Calls_Come_From_Screens_And_Links()
    {
        var screen = Record("screen", "screen/library.search", "Search", "approved", "approved", """{"entryPoint": "entry-point/library-app"}""");
        var entry = Record("entry-point", "entry-point/library-app", "Library App", "approved", "approved");
        var useCase = Record("use-case", "use-case/lending.list-loans", "List Loans", "approved", "approved", """{"reachedThrough": ["entry-point/library-app"]}""");
        var model = NewModel(screen, entry, useCase);
        (model["screens"] as JsonArray)!.Add(new JsonObject
        {
            ["frontend"] = "catalog",
            ["route"] = "/search",
            ["file"] = "apps/catalog/src/routes/search/+page.svelte",
            ["screen"] = "screen/library.search",
            ["reader"] = "svelte",
        });
        (model["links"] as JsonObject)!["calls"] = new JsonArray(new JsonObject
        {
            ["screen"] = "catalog /search",
            ["to"] = "use-case/lending.list-loans",
            ["operation"] = "search",
        });
        var enriched = DetailsOf(model)["screen/library.search"];
        var route = Obj(enriched, "route");
        Assert.Equal("catalog", Str(route, "frontend"));
        Assert.Equal("/search", Str(route, "route"));
        Assert.True(HasString(Arr(enriched, "calls"), "use-case/lending.list-loans"));
        var impl = Arr(enriched, "impl");
        Assert.Equal(1, impl.Count);
        Assert.Equal("route", Str(impl[0] as JsonObject, "kind"));
        var bare = DetailsOf(NewModel(Record("screen", "screen/library.bare", "Bare")))["screen/library.bare"];
        Assert.Null(bare["route"]);
        Assert.Empty(Arr(bare, "calls"));
    }

    /// <summary>Mark lines come from model data only; implements links open at the file head.</summary>
    /// <proves>litespec/core/model#point-every-book-entry-to-its-source</proves>
    [Fact]
    public void Impl_Uses_Model_Lines_And_Null_For_Implements()
    {
        var useCase = Record("use-case", "use-case/lending.borrow-copy", "Borrow Copy", "approved", "approved", """{"reachedThrough": ["entry-point/library-app"]}""");
        var model = NewModel(useCase);
        (model["code"] as JsonObject)!["handlers"] = new JsonArray(new JsonObject
        {
            ["useCase"] = "use-case/lending.borrow-copy",
            ["path"] = "src/App/Borrow.cs",
            ["line"] = 12,
        });
        (model["links"] as JsonObject)!["implements"] = new JsonArray(
            new JsonObject
            {
                ["path"] = "src/App/Borrow.cs",
                ["to"] = "use-case/lending.borrow-copy",
                ["kind"] = "handler",
            },
            new JsonObject
            {
                ["path"] = "src/App/Extra.cs",
                ["to"] = "use-case/lending.borrow-copy",
                ["kind"] = "port",
            });
        var impl = Arr(DetailsOf(model)["use-case/lending.borrow-copy"], "impl");
        Assert.Equal(2, impl.Count);
        var first = impl[0] as JsonObject ?? new JsonObject();
        Assert.Equal("handler", Str(first, "kind"));
        Assert.Equal("src/App/Borrow.cs", Str(first, "path"));
        Assert.Equal(12, first["line"]?.GetValue<int>());
        var second = impl[1] as JsonObject ?? new JsonObject();
        Assert.Equal("port", Str(second, "kind"));
        Assert.Null(second["line"]);
    }

    /// <summary>Expectations and missing marks are computed once from code and tests.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Tests_Expect_And_Lacks_Are_Computed_Once()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        Assert.True(HasString(Arr(details["use-case/lending.borrow-copy"], "lacks"), "scenarios without a test"));
        Assert.True(HasString(Arr(details["use-case/lending.return-copy"], "lacks"), "scenarios without a test"));
        Assert.True(HasString(Arr(details["aggregate/lending.loan"], "lacks"), "no code"));
        Assert.Empty(Arr(details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"], "lacks"));
        Assert.True(HasString(Arr(details["scenario/borrow-copy.borrowing-a-loaned-copy-is-refused"], "lacks"), "no test"));
        var expect = Obj(details["use-case/lending.borrow-copy"], "expect");
        Assert.True(BoolOf(expect["impl"]));
        Assert.True(BoolOf(expect["proof"]));
        Assert.Equal(1, Arr(details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"], "tests").Count);
        var model = NewModel(Record("scenario", "scenario/borrow-copy.demo", "Demo"));
        (model["browserTests"] as JsonArray)!.Add(new JsonObject
        {
            ["frontend"] = "catalog",
            ["title"] = "Search works",
            ["file"] = "apps/catalog/tests/search.spec.ts",
            ["line"] = 7,
            ["proves"] = new JsonArray("scenario/borrow-copy.demo"),
        });
        var browserTests = Arr(DetailsOf(model)["scenario/borrow-copy.demo"], "tests");
        Assert.Equal(1, browserTests.Count);
        var browser = browserTests[0] as JsonObject ?? new JsonObject();
        Assert.Equal("Search works", Str(browser, "name"));
        Assert.True(BoolOf(browser["browser"]));
    }

    /// <summary>Settled records with no draft above hold an empty held-by list.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void HeldBy_Is_Empty_Without_Draft_Uplinks()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        Assert.Empty(Arr(details["scenario/borrow-copy.borrowing-a-loaned-copy-is-refused"], "heldBy"));
        Assert.Empty(Arr(details["result/borrow-copy.borrowed"], "heldBy"));
        Assert.Empty(Arr(details["feature/self-checkout"], "heldBy"));
    }

    /// <summary>Held-by names the draft parent, decisions and requirements above a record.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void HeldBy_Lists_Draft_Parent_Decisions_And_Requirements()
    {
        var problem = Record("problem", "problem/wait", "Wait");
        var feature = Record("feature", "feature/self-checkout", "Self Checkout", "approved", "draft", """{"solves": ["problem/wait"]}""");
        var requirement = Record("requirement", "requirement/wait.max", "Max");
        var useCase = Record("use-case", "use-case/lending.borrow-copy", "Borrow", "approved", "draft", """{"fulfills": ["requirement/wait.max"]}""");
        var decision = Record("decision", "decision/kiosk", "Kiosk", "draft", "draft", """{"decides": ["feature/self-checkout"]}""");
        var model = NewModel(problem, feature, requirement, useCase, decision);
        var outline = BookOutline.Build(model);
        var details = BookEnrich.Build(model, outline);
        var held = Arr(details["feature/self-checkout"], "heldBy");
        Assert.True(HasString(held, "problem/wait"));
        Assert.True(HasString(held, "decision/kiosk"));
        Assert.True(HasString(Arr(details["use-case/lending.borrow-copy"], "heldBy"), "requirement/wait.max"));
        foreach (var pair in details)
        {
            Assert.True(Arr(pair.Value, "heldBy").Count <= 6);
        }
    }

    /// <summary>Findings group by subject base id, open and known apart, unknown dropped.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Findings_Group_By_Subject_Base()
    {
        var model = LoadFixture("Book/review.json");
        model["findings"] = model["findings"] as JsonArray ?? new JsonArray();
        model["known"] = model["known"] as JsonArray ?? new JsonArray();
        (model["findings"] as JsonArray)!.Add(new JsonObject
        {
            ["code"] = "use-case-unimplemented",
            ["tier"] = "G",
            ["message"] = "Missing handler.",
            ["subject"] = "use-case/lending.borrow-copy#step",
            ["path"] = "docs/a.md",
            ["line"] = 3,
        });
        (model["known"] as JsonArray)!.Add(new JsonObject
        {
            ["code"] = "step-screen-missing",
            ["tier"] = "A",
            ["message"] = "No screen.",
            ["subject"] = "journey/borrowing",
            ["outcome"] = "accepted",
            ["reason"] = "Later.",
        });
        (model["findings"] as JsonArray)!.Add(new JsonObject
        {
            ["code"] = "stray",
            ["message"] = "Nowhere.",
            ["subject"] = "use-case/missing.nope",
        });
        var details = DetailsOf(model);
        var borrowFindings = Arr(details["use-case/lending.borrow-copy"], "findings");
        Assert.Equal(1, borrowFindings.Count);
        var finding = borrowFindings[0] as JsonObject ?? new JsonObject();
        Assert.Equal("use-case-unimplemented", Str(finding, "code"));
        Assert.False(BoolOf(finding["known"]));
        var journeyFindings = Arr(details["journey/borrowing"], "findings");
        Assert.Equal(1, journeyFindings.Count);
        Assert.True(BoolOf((journeyFindings[0] as JsonObject ?? new JsonObject())["known"]));
        Assert.False(details.ContainsKey("use-case/missing.nope"));
    }

    /// <summary>Bodies render to HTML with hash record links, split into intro and sections.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void BodyHtml_Renders_Markdown_With_Hash_Links_And_Sections()
    {
        var loan = DetailsOf(LoadFixture("Book/review.json"))["aggregate/lending.loan"];
        var html = Str(loan, "bodyHtml") ?? "";
        Assert.Contains("href=\"#use-case/lending.borrow-copy\"", html, StringComparison.Ordinal);
        Assert.Contains("Borrow Copy", html, StringComparison.Ordinal);
        var list = Obj(loan, "sections")["list"] as JsonArray ?? new JsonArray();
        Assert.True(list.Count >= 1);
        Assert.Equal("Bounds", Str(list[0] as JsonObject, "h"));
        Assert.Contains("one copy", Str(list[0] as JsonObject, "html") ?? "", StringComparison.Ordinal);
        var model = NewModel(Record("feature", "feature/demo", "Demo", "draft", "draft", "{}", "Intro words.\n\n## Why Now\n\nBecause demand.\n"));
        var sections = Obj(DetailsOf(model)["feature/demo"], "sections");
        Assert.Contains("Intro words", Str(sections, "intro") ?? "", StringComparison.Ordinal);
        var items = sections["list"] as JsonArray ?? new JsonArray();
        Assert.Equal(1, items.Count);
        Assert.Equal("Why Now", Str(items[0] as JsonObject, "h"));
    }

    /// <summary>Outgoing and incoming edges carry field keys with no wording.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Out_And_In_Edges_Use_Field_Keys()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        var useOut = Arr(details["use-case/lending.borrow-copy"], "out");
        Assert.True(HasEdge(useOut, "journey/borrowing", "use-case.serves"));
        Assert.True(HasEdge(useOut, "requirement/borrowing.max-wait", "use-case.fulfills"));
        Assert.True(HasEdge(Arr(details["journey/borrowing"], "in"), "use-case/lending.borrow-copy", "use-case.serves"));
        Assert.True(HasEdge(Arr(details["feature/self-checkout"], "out"), "goal/faster-borrowing", "feature.advances"));
        Assert.True(HasEdge(
            Arr(details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"], "in"),
            "test/Library.Tests.BorrowTests.Borrowing_An_Available_Copy_Succeeds",
            "proves"));
        Assert.True(HasEdge(
            Arr(details["use-case/lending.borrow-copy"], "in"),
            "code/src/Application/Lending/BorrowCopyHandler.cs",
            "implements"));
    }

    /// <summary>Nested link fields form dotted keys, and step fragments resolve to the record.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Nested_Effects_Form_Dotted_Keys()
    {
        var caller = Record("use-case", "use-case/lending.borrow-copy", "Borrow", "approved", "approved", """{"effects": [{"calls": "external-system/address-lookup#validate"}]}""");
        var external = Record("external-system", "external-system/address-lookup", "Address Lookup", "approved", "approved");
        var details = DetailsOf(NewModel(caller, external));
        Assert.True(HasEdge(Arr(details["use-case/lending.borrow-copy"], "out"), "external-system/address-lookup", "use-case.effects.calls"));
        Assert.True(HasEdge(Arr(details["external-system/address-lookup"], "in"), "use-case/lending.borrow-copy", "use-case.effects.calls"));
    }

    /// <summary>Wave-5 link objects resolve to dotted edges, and a mode-only object resolves to none while reaching the client intact.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Wave5_Link_Objects_Form_Record_Edges()
    {
        var saga = Record("aggregate", "aggregate/lending.hold-queue", "Hold Queue", "approved", "approved", """{"kind": "saga", "steps": [{"id": "hold-place", "runs": "use-case/lending.return-copy", "compensation": {"mode": "use-case", "useCase": "use-case/lending.borrow-copy"}, "guard": "the hold exists"}]}""");
        var useCase = Record("use-case", "use-case/lending.borrow-copy", "Borrow Copy", "approved", "approved");
        var runner = Record("use-case", "use-case/lending.return-copy", "Return Copy", "approved", "approved");
        var rule = Record("rule", "rule/loan.hold-release", "Hold Release", "approved", "approved", """{"releasedBy": "use-case/lending.borrow-copy"}""");
        var kept = Record("rule", "rule/loan.ever-held", "Ever Held", "approved", "approved", """{"releasedBy": {"mode": "never", "reason": "the hold lasts"}}""");
        var details = DetailsOf(NewModel(saga, useCase, runner, rule, kept));
        Assert.True(HasEdge(Arr(details["aggregate/lending.hold-queue"], "out"), "use-case/lending.borrow-copy", "aggregate.steps.compensation.useCase"));
        Assert.True(HasEdge(Arr(details["aggregate/lending.hold-queue"], "out"), "use-case/lending.return-copy", "aggregate.steps.runs"));
        Assert.True(HasEdge(Arr(details["use-case/lending.borrow-copy"], "in"), "aggregate/lending.hold-queue", "aggregate.steps.compensation.useCase"));
        Assert.True(HasEdge(Arr(details["rule/loan.hold-release"], "out"), "use-case/lending.borrow-copy", "rule.releasedBy"));
        Assert.Empty(Arr(details["rule/loan.ever-held"], "out"));
        Assert.Equal("never", Str(Obj(Obj(details["rule/loan.ever-held"], "fields"), "releasedBy"), "mode"));
    }

    /// <summary>Wave-6 link shapes resolve to dotted edges: automation triggers and effects, saga runs and drivers, binds, exercises and typed references.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Wave6_Link_Shapes_Form_Record_Edges()
    {
        var loan = Record("aggregate", "aggregate/lending.loan", "Loan", "approved", "approved");
        var room = Record("aggregate", "aggregate/inventory.room", "Room", "approved", "approved");
        var useCase = Record(
            "use-case",
            "use-case/lending.borrow-copy",
            "Borrow Copy",
            "approved",
            "approved",
            """{"input": [{"name": "roomId", "type": "id", "of": "aggregate/inventory.room", "required": true}], "effects": [{"runs": "use-case/lending.return-copy"}]}""");
        var runner = Record("use-case", "use-case/lending.return-copy", "Return Copy", "approved", "approved");
        var due = Record("event", "event/loan.loan-due", "Loan Due", "approved", "approved");
        var automation = Record(
            "automation",
            "automation/lending.send-due-reminder-on-loan-due",
            "Send Due Reminder",
            "approved",
            "approved",
            """{"triggeredBy": {"event": "event/loan.loan-due"}, "effects": [{"sends": "notification/lending.due-reminder"}, {"emits": "integration-event/loan-borrowed"}], "onFailure": {"mode": "retry", "retry": {"maxAttempts": 5}, "then": "dead-letter", "surfacesIn": "entry-point/library-app", "runbook": "runbook/overdue-notice"}}""");
        var notification = Record("notification", "notification/lending.due-reminder", "Due Reminder", "approved", "approved");
        var integration = Record(
            "integration-event",
            "integration-event/loan-borrowed",
            "Loan Borrowed",
            "approved",
            "approved",
            """{"dataschema": "https://example.com/schemas/loan-borrowed.json", "payload": [{"name": "loanId", "type": "id", "of": "aggregate/lending.loan", "required": true}]}""");
        var saga = Record(
            "aggregate",
            "aggregate/lending.refund-stay",
            "Refund Stay",
            "approved",
            "approved",
            """{"kind": "saga", "steps": [{"id": "release-room", "runs": "use-case/lending.return-copy", "compensation": {"mode": "none", "reason": "the room returns"}, "guard": "the stay is cancelled"}], "exits": ["refunded"], "drivenBy": {"automation": "automation/lending.send-due-reminder-on-loan-due"}}""");
        var rule = Record("rule", "rule/loan.one-loan-per-copy", "One Loan Per Copy", "approved", "approved", """{"binds": ["aggregate/lending.loan", "aggregate/inventory.room"]}""");
        var result = Record("result", "result/borrow-copy.borrowed", "Borrowed", "approved", "approved");
        var scenario = Record(
            "scenario",
            "scenario/borrow-copy.borrowing-succeeds",
            "Borrowing Succeeds",
            "approved",
            "approved",
            """{"result": "result/borrow-copy.borrowed", "exercises": ["rule/loan.one-loan-per-copy"], "given": "A copy sits available.", "when": "The member borrows.", "then": "The loan is active."}""");
        var door = Record("entry-point", "entry-point/library-app", "Library App", "approved", "approved");
        var runbook = Record("runbook", "runbook/overdue-notice", "Overdue Notice", "approved", "approved");
        var details = DetailsOf(NewModel(loan, room, useCase, runner, due, automation, notification, integration, saga, rule, result, scenario, door, runbook));

        Assert.True(HasEdge(Arr(details["automation/lending.send-due-reminder-on-loan-due"], "out"), "event/loan.loan-due", "automation.triggeredBy.event"));
        Assert.True(HasEdge(Arr(details["use-case/lending.borrow-copy"], "out"), "use-case/lending.return-copy", "use-case.effects.runs"));
        Assert.True(HasEdge(Arr(details["automation/lending.send-due-reminder-on-loan-due"], "out"), "notification/lending.due-reminder", "automation.effects.sends"));
        Assert.True(HasEdge(Arr(details["automation/lending.send-due-reminder-on-loan-due"], "out"), "integration-event/loan-borrowed", "automation.effects.emits"));
        Assert.True(HasEdge(Arr(details["automation/lending.send-due-reminder-on-loan-due"], "out"), "entry-point/library-app", "automation.onFailure.surfacesIn"));
        Assert.True(HasEdge(Arr(details["automation/lending.send-due-reminder-on-loan-due"], "out"), "runbook/overdue-notice", "automation.onFailure.runbook"));
        Assert.True(HasEdge(Arr(details["aggregate/lending.refund-stay"], "out"), "use-case/lending.return-copy", "aggregate.steps.runs"));
        Assert.True(HasEdge(Arr(details["aggregate/lending.refund-stay"], "out"), "automation/lending.send-due-reminder-on-loan-due", "aggregate.drivenBy.automation"));
        Assert.True(HasEdge(Arr(details["rule/loan.one-loan-per-copy"], "out"), "aggregate/lending.loan", "rule.binds"));
        Assert.True(HasEdge(Arr(details["scenario/borrow-copy.borrowing-succeeds"], "out"), "rule/loan.one-loan-per-copy", "scenario.exercises"));
        Assert.True(HasEdge(Arr(details["use-case/lending.borrow-copy"], "out"), "aggregate/inventory.room", "use-case.input.of"));
        Assert.True(HasEdge(Arr(details["integration-event/loan-borrowed"], "out"), "aggregate/lending.loan", "integration-event.payload.of"));
        Assert.Equal("https://example.com/schemas/loan-borrowed.json", Str(Obj(details["integration-event/loan-borrowed"], "fields"), "dataschema"));
    }

    static JsonObject LoadFixture(string relative) =>
        (JsonObject)JsonNode.Parse(File.ReadAllText(Cli.Fixture(relative)))!;

    static IReadOnlyDictionary<string, JsonObject> DetailsOf(JsonObject model) =>
        BookEnrich.Build(model, BookOutline.Build(model));

    static JsonObject NewModel(params JsonObject[] records)
    {
        var list = new JsonArray();
        foreach (var record in records)
        {
            list.Add(record.DeepClone());
        }

        return new JsonObject
        {
            ["records"] = list,
            ["code"] = new JsonObject
            {
                ["handlers"] = new JsonArray(),
                ["endpoints"] = new JsonArray(),
                ["aggregateMethods"] = new JsonArray(),
                ["automations"] = new JsonArray(),
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
            ["stale"] = new JsonArray(),
            ["tests"] = new JsonArray(),
            ["browserTests"] = new JsonArray(),
            ["screens"] = new JsonArray(),
            ["exercisedSteps"] = new JsonArray(),
            ["findings"] = new JsonArray(),
            ["known"] = new JsonArray(),
            ["testRun"] = null,
        };
    }

    static JsonObject Record(string type, string id, string title, string status = "draft", string? effective = null, string fieldsJson = "{}", string? body = null)
    {
        var record = new JsonObject
        {
            ["type"] = type,
            ["id"] = id,
            ["title"] = title,
            ["owner"] = "Ada",
            ["status"] = status,
            ["effectiveStatus"] = effective ?? status,
            ["decider"] = null,
            ["decideBy"] = null,
            ["file"] = "docs/test.md",
            ["line"] = 3,
            ["fields"] = (JsonObject)JsonNode.Parse(fieldsJson)!,
            ["links"] = new JsonObject(),
        };
        if (body is not null)
        {
            record["body"] = body;
        }

        return record;
    }

    static string? Str(JsonObject? obj, string name) =>
        obj?[name] is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? text : null;

    static bool BoolOf(JsonNode? node) =>
        node is JsonValue scalar && scalar.TryGetValue<bool>(out var flag) && flag;

    static JsonArray Arr(JsonObject obj, string name) => obj[name] as JsonArray ?? new JsonArray();

    static JsonObject Obj(JsonObject obj, string name) => obj[name] as JsonObject ?? new JsonObject();

    static bool HasEdge(JsonArray edges, string id, string key)
    {
        foreach (var edge in edges.OfType<JsonObject>())
        {
            if (Str(edge, "id") == id && Str(edge, "key") == key)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasString(JsonArray list, string value)
    {
        foreach (var item in list)
        {
            if (item is JsonValue scalar && scalar.TryGetValue<string>(out var text) && text == value)
            {
                return true;
            }
        }

        return false;
    }
}
