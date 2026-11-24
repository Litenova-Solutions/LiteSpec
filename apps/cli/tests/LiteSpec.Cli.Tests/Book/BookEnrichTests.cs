using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Details;
using LiteSpec.Cli.Book.Outline;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Book;

/// <summary>Proves the book enrich step emits computed record details from the model only.</summary>
public sealed class BookEnrichTests
{
    /// <summary>Each detail keeps the mockup shape: identity, stripped fields and source, with no status, owner or decider.</summary>
    /// <proves>litespec/core/model#point-every-book-entry-to-its-source</proves>
    [Fact]
    public void BuildKeepsTheMockupRecordShape()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        var loan = details["aggregate/lending.loan"];
        Assert.Equal("aggregate", Str(loan, "type"));
        Assert.Equal("Loan", Str(loan, "title"));
        Assert.Null(loan["owner"]);
        Assert.Null(loan["status"]);
        Assert.Null(loan["effective"]);
        Assert.Null(loan["decider"]);
        Assert.Null(loan["decideBy"]);
        var fields = Obj(loan, "fields");
        Assert.Null(fields["type"]);
        Assert.Null(fields["id"]);
        Assert.Null(fields["title"]);
        Assert.Null(fields["owner"]);
        Assert.Null(fields["status"]);
        Assert.Null(fields["decider"]);
        Assert.Null(fields["decideBy"]);
        Assert.NotNull(fields["summary"]);
        var src = Obj(loan, "src");
        Assert.Equal(".litespec/behavior/modules/lending/aggregates/loans/aggregate.yaml", Str(src, "path"));
        Assert.Equal(3, src["line"]?.GetValue<int>());
    }

    /// <summary>Each detail carries only its own fields and explanation: the product's boundary, excludes and prose never leak onto another record's page.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void EachDetailCarriesOnlyItsOwnFieldsAndExplanation()
    {
        var model = NewModel(
            Record("product", "product/library", "Library", fieldsJson: """{"summary":"Books lend.","boundary":"Lending only.","excludes":["No sales."]}""", explanation: "# Library\n\nLibrary lends books."),
            Record("entry-point", "entry-point/library-web", "Library Web", fieldsJson: """{"summary":"The web app.","kind":"browser"}""", explanation: "# Library Web\n\nThe web app."));
        var entry = DetailsOf(model)["entry-point/library-web"];
        var fields = Obj(entry, "fields");
        Assert.Equal("The web app.", Str(fields, "summary"));
        Assert.Null(fields["boundary"]);
        Assert.Null(fields["excludes"]);
        var intro = Str(Obj(entry, "sections"), "intro") ?? "";
        Assert.Contains("The web app.", intro, StringComparison.Ordinal);
        Assert.DoesNotContain("Library lends books.", intro, StringComparison.Ordinal);
    }

    /// <summary>Every parent matches the outline parent for the same record.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void ParentMatchesTheOutlineParentForEveryRecord()
    {
        var model = LoadFixture("Book/review.json");
        var outline = BookOutline.Build(model);
        var details = BookRecordDetails.Build(model, outline);
        foreach (var pair in details)
        {
            var expected = outline.ParentOf.TryGetValue(pair.Key, out var parent) ? parent : null;
            Assert.Equal(expected, Str(pair.Value, "parent"));
        }
    }

    /// <summary>Layers follow the outline roots, one of the six known layer keys, and root for the product.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void LayerFollowsTheOutlineLayers()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        Assert.Equal("intent", Str(details["goal/faster-borrowing"], "layer"));
        Assert.Equal("behavior", Str(details["use-case/lending.borrow-copy"], "layer"));
        Assert.Equal("root", Str(details["product/library"], "layer"));
        foreach (var pair in details)
        {
            var layer = Str(pair.Value, "layer");
            Assert.True(
                layer is null || layer is "intent" or "behavior" or "implementation" or "proof" or "governance" or "glossary" or "cast" or "root",
                $"Unexpected layer {layer} for {pair.Key}.");
        }
    }

    /// <summary>Scenarios and rules read norun while the model holds no test run.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void ProofIsNorunWithoutATestRun()
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
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void ProofMapsPassingAndUnprovedWithTestLines()
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

    /// <summary>Machine states come from the status maps the builder already writes: verified use cases, met requirements.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void MachineStatesComeFromStatusMaps()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        var borrow = Obj(details["use-case/lending.borrow-copy"], "machine");
        Assert.True(BoolOf(borrow["coded"]));
        Assert.False(BoolOf(borrow["verified"]));
        var returned = Obj(details["use-case/lending.return-copy"], "machine");
        Assert.False(BoolOf(returned["coded"]));
        var shelf = Obj(details["requirement/self-checkout.copy-shelf-home"], "machine");
        Assert.True(BoolOf(shelf["met"]));
        Assert.Null(shelf["verified"]);
        Assert.Null(shelf["measurePassed"]);
        var wait = Obj(details["requirement/self-checkout.max-wait"], "machine");
        Assert.False(BoolOf(wait["met"]));
        Assert.Null(wait["verified"]);
        Assert.Equal(false, wait["measurePassed"]?.GetValue<bool>());
        Assert.Null(details["goal/faster-borrowing"]["machine"]);
    }

    /// <summary>A stored scenario state wins over the proves links; a scenario without one still reads its links.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void StoredScenarioStatusWinsOverProvesLinks()
    {
        var model = NewModel(
            Record("scenario", "scenario/borrow-copy.stored", "Stored"),
            Record("scenario", "scenario/borrow-copy.derived", "Derived"));
        model["testRun"] = new JsonObject { ["date"] = "2026-10-20" };
        (model["links"] as JsonObject)!["proves"] = new JsonArray(
            new JsonObject
            {
                ["scenario"] = "Library.Tests.BorrowTests.Stored",
                ["claim"] = "scenario/borrow-copy.stored",
                ["confirmed"] = true,
                ["passing"] = true,
                ["indirect"] = false,
            },
            new JsonObject
            {
                ["scenario"] = "Library.Tests.BorrowTests.Derived",
                ["claim"] = "scenario/borrow-copy.derived",
                ["confirmed"] = true,
                ["passing"] = true,
                ["indirect"] = false,
            });
        model["scenarioStatus"] = new JsonObject { ["scenario/borrow-copy.stored"] = "failing" };
        var details = DetailsOf(model);
        Assert.Equal("failing", Str(Obj(details["scenario/borrow-copy.stored"], "proof"), "state"));
        Assert.Equal("passing", Str(Obj(details["scenario/borrow-copy.derived"], "proof"), "state"));
    }

    /// <summary>An indirect proof reads indirect, never passing.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void IndirectProofReadsIndirect()
    {
        var model = NewModel(Record("scenario", "scenario/borrow-copy.background", "Background"));
        model["testRun"] = new JsonObject { ["date"] = "2026-10-20" };
        (model["links"] as JsonObject)!["proves"] = new JsonArray(
            new JsonObject
            {
                ["scenario"] = "Library.Tests.BorrowTests.Background",
                ["claim"] = "scenario/borrow-copy.background",
                ["confirmed"] = true,
                ["passing"] = true,
                ["indirect"] = true,
            });
        var proof = Obj(DetailsOf(model)["scenario/borrow-copy.background"], "proof");
        Assert.Equal("indirect", Str(proof, "state"));
    }

    /// <summary>Tested proof targets read their tested state from the tested map the builder wrote.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void TestedComesFromTestedMap()
    {
        var model = NewModel(
            Record("requirement", "requirement/borrowing.fast-desk", "Fast Desk", "approved", "approved", """{"kind": "quality"}"""),
            Record("decision", "decision/0012-use-postgres", "Use Postgres", "approved", "approved"),
            Record("external-system", "external-system/address-lookup", "Address Lookup", "approved", "approved"),
            Record("integration-event", "integration-event/loan-borrowed", "Loan Borrowed", "approved", "approved"),
            Record("decision", "decision/0013-use-sqlite", "Use Sqlite", "draft", "draft"));
        model["tested"] = new JsonObject
        {
            ["requirement/borrowing.fast-desk"] = true,
            ["decision/0012-use-postgres"] = true,
            ["external-system/address-lookup"] = false,
            ["integration-event/loan-borrowed"] = true,
        };
        (model["requirementStatus"] as JsonObject)!["requirement/borrowing.fast-desk"] = new JsonObject
        {
            ["verified"] = true,
        };
        var details = DetailsOf(model);
        var quality = Obj(details["requirement/borrowing.fast-desk"], "machine");
        Assert.True(BoolOf(quality["met"]));
        Assert.True(BoolOf(quality["tested"]));
        Assert.True(BoolOf(Obj(details["decision/0012-use-postgres"], "machine")["tested"]));
        Assert.False(BoolOf(Obj(details["external-system/address-lookup"], "machine")["tested"]));
        Assert.True(BoolOf(Obj(details["integration-event/loan-borrowed"], "machine")["tested"]));
        Assert.Null(details["decision/0013-use-sqlite"]["machine"]);
    }

    /// <summary>Aggregates list their reachable states; other types carry no reachable list.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void ReachableListsAggregateStates()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        var reachable = Arr(details["aggregate/lending.loan"], "reachable");
        Assert.Equal(2, reachable.Count);
        Assert.True(HasString(reachable, "OnShelf"));
        Assert.True(HasString(reachable, "OnLoan"));
        Assert.Null(details["use-case/lending.borrow-copy"]["reachable"]);
    }

    /// <summary>Requirements count the fulfilling records by type.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void KeptByCountsFulfillingTypes()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        var kept = Obj(details["requirement/self-checkout.max-wait"], "keptBy");
        Assert.Equal(1, kept["use-case"]?.GetValue<int>());
        Assert.Equal(1, kept["reaction"]?.GetValue<int>());
        Assert.Equal(1, kept["notification"]?.GetValue<int>());
        Assert.Null(details["goal/faster-borrowing"]["keptBy"]);
    }

    /// <summary>Flow steps list their servers and screens, and the steps a test walked.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void FlowStepsListServersScreensAndExercised()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        var flow = details["flow/borrowing"];
        var servers = Obj(flow, "stepServers");
        Assert.True(HasString(servers["step/borrowing.borrow-copy"] as JsonArray ?? new JsonArray(), "use-case/lending.borrow-copy"));
        Assert.Empty(servers["step/borrowing.return-copy"] as JsonArray ?? new JsonArray());
        Assert.Empty(Obj(flow, "stepScreens")["step/borrowing.borrow-copy"] as JsonArray ?? new JsonArray());
        Assert.Empty(Arr(flow, "exercised"));
        Assert.Null(details["goal/faster-borrowing"]["stepServers"]);
    }

    /// <summary>A reaction and a job serving steps read as their servers, and a walked step reads walked.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void ReactionAndJobServedAndExercisedSteps()
    {
        var model = LoadFixture("Book/review.json");
        var reactionId = "";
        var jobId = "";
        foreach (var record in model["records"] as JsonArray ?? new JsonArray())
        {
            if (record is not JsonObject obj)
            {
                continue;
            }

            if (Str(obj, "type") == "reaction" && reactionId.Length == 0)
            {
                reactionId = Str(obj, "id") ?? "";
                var fields = obj["fields"] as JsonObject ?? new JsonObject();
                fields["serves"] = new JsonArray("step/borrowing.return-copy");
            }
            else if (Str(obj, "type") == "job" && jobId.Length == 0)
            {
                jobId = Str(obj, "id") ?? "";
                var fields = obj["fields"] as JsonObject ?? new JsonObject();
                fields["serves"] = new JsonArray("step/borrowing.borrow-copy");
            }
        }

        Assert.True(reactionId.Length > 0, "The fixture holds no reaction.");
        Assert.True(jobId.Length > 0, "The fixture holds no job.");
        model["exercisedSteps"] = new JsonArray("step/borrowing.borrow-copy");
        var flow = DetailsOf(model)["flow/borrowing"];
        var servers = Obj(flow, "stepServers");
        Assert.True(HasString(servers["step/borrowing.return-copy"] as JsonArray ?? new JsonArray(), reactionId));
        Assert.True(HasString(servers["step/borrowing.borrow-copy"] as JsonArray ?? new JsonArray(), jobId));
        var exercised = Arr(flow, "exercised");
        Assert.True(HasString(exercised, "step/borrowing.borrow-copy"));
        Assert.False(HasString(exercised, "step/borrowing.return-copy"));
    }

    /// <summary>Screens carry their route, called use cases and a route implementation entry.</summary>
    /// <proves>litespec/core/model#point-every-book-entry-to-its-source</proves>
    [Fact]
    public void ScreenRouteAndCallsComeFromScreensAndLinks()
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
        Assert.Single(impl);
        Assert.Equal("route", Str(impl[0] as JsonObject, "kind"));
        var bare = DetailsOf(NewModel(Record("screen", "screen/library.bare", "Bare")))["screen/library.bare"];
        Assert.Null(bare["route"]);
        Assert.Empty(Arr(bare, "calls"));
    }

    /// <summary>Mark lines come from model data only; implements links open at the file head.</summary>
    /// <proves>litespec/core/model#point-every-book-entry-to-its-source</proves>
    [Fact]
    public void ImplUsesModelLinesAndNullForImplements()
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
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void TestsExpectAndLacksAreComputedOnce()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        Assert.True(HasString(Arr(details["use-case/lending.borrow-copy"], "lacks"), "scenarios not tested"));
        Assert.True(HasString(Arr(details["use-case/lending.return-copy"], "lacks"), "scenarios not tested"));
        Assert.True(HasString(Arr(details["aggregate/lending.loan"], "lacks"), "not coded"));
        Assert.Empty(Arr(details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"], "lacks"));
        Assert.True(HasString(Arr(details["scenario/borrow-copy.borrowing-a-loaned-copy-is-refused"], "lacks"), "not tested"));
        var expect = Obj(details["use-case/lending.borrow-copy"], "expect");
        Assert.True(BoolOf(expect["impl"]));
        Assert.True(BoolOf(expect["proof"]));
        Assert.Single(Arr(details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"], "tests"));
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
        Assert.Single(browserTests);
        var browser = browserTests[0] as JsonObject ?? new JsonObject();
        Assert.Equal("Search works", Str(browser, "name"));
        Assert.True(BoolOf(browser["browser"]));
    }

    /// <summary>Findings group by subject base id, open and known apart, unknown dropped.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void FindingsGroupBySubjectBase()
    {
        var model = LoadFixture("Book/review.json");
        model["findings"] = model["findings"] as JsonArray ?? new JsonArray();
        model["covered"] = model["covered"] as JsonArray ?? new JsonArray();
        (model["findings"] as JsonArray)!.Add(new JsonObject
        {
            ["code"] = "use-case-unimplemented",
            ["tier"] = "G",
            ["message"] = "Missing handler for 'missing/x.y'.",
            ["subject"] = "use-case/lending.borrow-copy#step",
            ["path"] = ".litespec/a.yaml",
            ["line"] = 3,
        });
        (model["covered"] as JsonArray)!.Add(new JsonObject
        {
            ["code"] = "step-screen-missing",
            ["tier"] = "G",
            ["message"] = "No screen.",
            ["subject"] = "flow/borrowing",
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
        Assert.Single(borrowFindings);
        var finding = borrowFindings[0] as JsonObject ?? new JsonObject();
        Assert.Equal("use-case-unimplemented", Str(finding, "code"));
        Assert.False(BoolOf(finding["covered"]));
        Assert.Equal("Missing handler for <code>missing/x.y</code>.", Str(finding, "messageHtml"));
        var flowFindings = Arr(details["flow/borrowing"], "findings");
        Assert.Single(flowFindings);
        Assert.True(BoolOf((flowFindings[0] as JsonObject ?? new JsonObject())["covered"]));
        Assert.False(details.ContainsKey("use-case/missing.nope"));
    }

    /// <summary>Explanations render to HTML with hash record links, split into intro and sections.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void BodyHtmlRendersMarkdownWithHashLinksAndSections()
    {
        var loan = DetailsOf(LoadFixture("Book/review.json"))["aggregate/lending.loan"];
        var html = BodyOf(loan);
        Assert.Contains("href=\"#use-case/lending.borrow-copy\"", html, StringComparison.Ordinal);
        Assert.Contains("Borrow Copy", html, StringComparison.Ordinal);
        var list = Obj(loan, "sections")["list"] as JsonArray ?? new JsonArray();
        Assert.True(list.Count >= 1);
        Assert.Equal("Bounds", Str(list[0] as JsonObject, "h"));
        Assert.Contains("one copy", Str(list[0] as JsonObject, "html") ?? "", StringComparison.Ordinal);
        var model = NewModel(Record("feature", "feature/demo", "Demo", "draft", "draft", "{}", "# Demo\n\nIntro words.\n\n## Why Now\n\nBecause demand.\n"));
        var sections = Obj(DetailsOf(model)["feature/demo"], "sections");
        Assert.Contains("Intro words", Str(sections, "intro") ?? "", StringComparison.Ordinal);
        var items = sections["list"] as JsonArray ?? new JsonArray();
        Assert.Single(items);
        Assert.Equal("Why Now", Str(items[0] as JsonObject, "h"));
    }

    /// <summary>Outgoing and incoming edges carry field keys with no wording.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void OutAndInEdgesUseFieldKeys()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        var useOut = Arr(details["use-case/lending.borrow-copy"], "out");
        Assert.True(HasEdge(useOut, "requirement/self-checkout.max-wait", "use-case.fulfills"));
        Assert.True(HasEdge(Arr(details["feature/self-checkout"], "out"), "goal/faster-borrowing", "feature.advances"));
        Assert.True(HasEdge(
            Arr(details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"], "in"),
            "test/Library.Tests.BorrowTests.Borrowing_An_Available_Copy_Succeeds",
            "proves"));
        Assert.True(HasEdge(
            Arr(details["use-case/lending.borrow-copy"], "in"),
            "code/src/Application/Lending/BorrowCopyHandler.cs",
            "implements"));

        var stepped = DetailsOf(NewModel(
            Record("flow", "flow/borrowing", "Borrowing", fieldsJson: """{"steps": [{"id": "step/borrowing.borrow-copy", "statement": "The reader borrows the copy."}]}"""),
            Record("step", "step/borrowing.borrow-copy", "step/borrowing.borrow-copy", fieldsJson: """{"statement": "The reader borrows the copy."}"""),
            Record("use-case", "use-case/lending.borrow-copy", "Borrow Copy", fieldsJson: """{"serves": ["step/borrowing.borrow-copy"]}""")));
        Assert.True(HasEdge(Arr(stepped["use-case/lending.borrow-copy"], "out"), "step/borrowing.borrow-copy", "use-case.serves"));
        Assert.True(HasEdge(Arr(stepped["step/borrowing.borrow-copy"], "in"), "use-case/lending.borrow-copy", "use-case.serves"));
    }

    /// <summary>Nested link fields form dotted keys, and operation fragments resolve to the record.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void NestedEffectsFormDottedKeys()
    {
        var caller = Record("use-case", "use-case/lending.borrow-copy", "Borrow", "approved", "approved", """{"effects": [{"calls": "external-system/address-lookup#validate"}]}""");
        var external = Record("external-system", "external-system/address-lookup", "Address Lookup", "approved", "approved");
        var details = DetailsOf(NewModel(caller, external));
        Assert.True(HasEdge(Arr(details["use-case/lending.borrow-copy"], "out"), "external-system/address-lookup", "use-case.effects.calls"));
        Assert.True(HasEdge(Arr(details["external-system/address-lookup"], "in"), "use-case/lending.borrow-copy", "use-case.effects.calls"));
    }

    /// <summary>Wave-5 link objects resolve to dotted edges, and a mode-only object resolves to none while reaching the client intact.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void Wave5LinkObjectsFormRecordEdges()
    {
        var useCase = Record("use-case", "use-case/lending.borrow-copy", "Borrow Copy", "approved", "approved");
        var rule = Record("rule", "rule/loan.hold-release", "Hold Release", "approved", "approved", """{"releasedBy": "use-case/lending.borrow-copy"}""");
        var kept = Record("rule", "rule/loan.ever-held", "Ever Held", "approved", "approved", """{"releasedBy": {"mode": "never", "reason": "the hold lasts"}}""");
        var details = DetailsOf(NewModel(useCase, rule, kept));
        Assert.True(HasEdge(Arr(details["rule/loan.hold-release"], "out"), "use-case/lending.borrow-copy", "rule.releasedBy"));
        Assert.Empty(Arr(details["rule/loan.ever-held"], "out"));
        Assert.Equal("never", Str(Obj(Obj(details["rule/loan.ever-held"], "fields"), "releasedBy"), "mode"));
    }

    /// <summary>Wave-6 link shapes resolve to dotted edges: reaction triggers and effects, job schedules, binds, exercises and typed references.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void Wave6LinkShapesFormRecordEdges()
    {
        var loan = Record("aggregate", "aggregate/lending.loan", "Loan", "approved", "approved");
        var room = Record("aggregate", "aggregate/inventory.room", "Room", "approved", "approved");
        var useCase = Record(
            "use-case",
            "use-case/lending.borrow-copy",
            "Borrow Copy",
            "approved",
            "approved",
            """{"input": [{"name": "roomId", "type": "id", "of": "aggregate/inventory.room", "required": true}]}""");
        var runner = Record("use-case", "use-case/lending.return-copy", "Return Copy", "approved", "approved");
        var due = Record("domain-event", "domain-event/loan.loan-due", "Loan Due", "approved", "approved");
        var reaction = Record(
            "reaction",
            "reaction/lending.send-due-reminder-on-loan-due",
            "Send Due Reminder",
            "approved",
            "approved",
            """{"triggeredBy": ["domain-event/loan.loan-due"], "effects": [{"runs": "use-case/lending.return-copy"}, {"sends": "notification/lending.due-reminder"}], "onFailure": {"mode": "retry", "retry": {"maxAttempts": 5}, "ifExhausted": "dead-letter", "surfacesIn": "entry-point/library-app", "runbook": "runbook/overdue-notice"}}""");
        var job = Record(
            "job",
            "job/lending.send-overdue-notices",
            "Send Overdue Notices",
            "approved",
            "approved",
            """{"kind": "scheduled", "schedule": {"cron": "0 6 * * *"}, "runbook": "runbook/overdue-notice", "effects": [{"sends": "notification/lending.due-reminder"}]}""");
        var notification = Record("notification", "notification/lending.due-reminder", "Due Reminder", "approved", "approved");
        var integration = Record(
            "integration-event",
            "integration-event/loan-borrowed",
            "Loan Borrowed",
            "approved",
            "approved",
            """{"dataschema": "https://example.com/schemas/loan-borrowed.json", "payload": [{"name": "loanId", "type": "id", "of": "aggregate/lending.loan", "required": true}]}""");
        var rule = Record("rule", "rule/loan.one-loan-per-copy", "One Loan Per Copy", "approved", "approved", """{"binds": ["aggregate/lending.loan", "aggregate/inventory.room"]}""");
        var result = Record("result", "result/borrow-copy.borrowed", "Borrowed", "approved", "approved");
        var scenario = Record(
            "scenario",
            "scenario/borrow-copy.borrowing-succeeds",
            "Borrowing Succeeds",
            "approved",
            "approved",
            """{"result": "result/borrow-copy.borrowed", "exercises": ["rule/loan.one-loan-per-copy"], "given": "A copy sits available.", "when": "The member borrows.", "then": "The loan is active."}""");
        var entryPoint = Record("entry-point", "entry-point/library-app", "Library App", "approved", "approved");
        var runbook = Record("runbook", "runbook/overdue-notice", "Overdue Notice", "approved", "approved");
        var details = DetailsOf(NewModel(loan, room, useCase, runner, due, reaction, job, notification, integration, rule, result, scenario, entryPoint, runbook));

        Assert.True(HasEdge(Arr(details["reaction/lending.send-due-reminder-on-loan-due"], "out"), "domain-event/loan.loan-due", "reaction.triggeredBy"));
        Assert.True(HasEdge(Arr(details["reaction/lending.send-due-reminder-on-loan-due"], "out"), "use-case/lending.return-copy", "reaction.effects.runs"));
        Assert.True(HasEdge(Arr(details["reaction/lending.send-due-reminder-on-loan-due"], "out"), "notification/lending.due-reminder", "reaction.effects.sends"));
        Assert.True(HasEdge(Arr(details["reaction/lending.send-due-reminder-on-loan-due"], "out"), "entry-point/library-app", "reaction.onFailure.surfacesIn"));
        Assert.True(HasEdge(Arr(details["reaction/lending.send-due-reminder-on-loan-due"], "out"), "runbook/overdue-notice", "reaction.onFailure.runbook"));
        Assert.True(HasEdge(Arr(details["job/lending.send-overdue-notices"], "out"), "notification/lending.due-reminder", "job.effects.sends"));
        Assert.True(HasEdge(Arr(details["job/lending.send-overdue-notices"], "out"), "runbook/overdue-notice", "job.runbook"));
        Assert.True(HasEdge(Arr(details["rule/loan.one-loan-per-copy"], "out"), "aggregate/lending.loan", "rule.binds"));
        Assert.True(HasEdge(Arr(details["scenario/borrow-copy.borrowing-succeeds"], "out"), "rule/loan.one-loan-per-copy", "scenario.exercises"));
        Assert.True(HasEdge(Arr(details["use-case/lending.borrow-copy"], "out"), "aggregate/inventory.room", "use-case.input.of"));
        Assert.True(HasEdge(Arr(details["integration-event/loan-borrowed"], "out"), "aggregate/lending.loan", "integration-event.payload.of"));
        Assert.Equal("https://example.com/schemas/loan-borrowed.json", Str(Obj(details["integration-event/loan-borrowed"], "fields"), "dataschema"));
    }

    /// <summary>A use case publishes each outbound integration event that translates a domain event it raises, and the edge is computed from raises and translates.</summary>
    /// <proves>litespec/core/edges#publish-every-outbound-integration-event-from-the-event-it-translates</proves>
    [Fact]
    public void PublishEdgeComesFromRaisesAndTranslates()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));

        Assert.True(HasEdge(Arr(details["use-case/lending.borrow-copy"], "out"), "integration-event/loan-borrowed", "publishes"));
        Assert.True(HasEdge(Arr(details["integration-event/loan-borrowed"], "in"), "use-case/lending.borrow-copy", "publishes"));
    }

    /// <summary>A read model reads its key aggregate, takes each field from an integration event, and is read by the use case that names it.</summary>
    /// <proves>litespec/backend/application#read-another-modules-state-through-a-read-model</proves>
    [Fact]
    public void ReadModelEdgesReadKeyFromAndReads()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        var readModel = "read-model/reporting.loan-borrowing";

        Assert.Equal("read-model", Str(details[readModel], "type"));
        Assert.True(HasEdge(Arr(details[readModel], "out"), "aggregate/lending.loan", "read-model.keyedBy"));
        Assert.True(HasEdge(Arr(details[readModel], "out"), "aggregate/lending.loan", "read-model.fields.of"));
        Assert.True(HasEdge(Arr(details[readModel], "out"), "integration-event/loan-borrowed", "read-model.fields.from"));
        Assert.True(HasEdge(Arr(details["integration-event/loan-borrowed"], "in"), readModel, "read-model.fields.from"));
        Assert.True(HasEdge(Arr(details["use-case/lending.return-copy"], "out"), readModel, "use-case.reads"));
        Assert.True(HasEdge(Arr(details[readModel], "in"), "use-case/lending.return-copy", "use-case.reads"));
    }

    /// <summary>A refusal result joins the meaning its module's refusal set declares and the problem type from the project's base; an unknown code joins no meaning and a success keeps its own.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void RefusalResultJoinsMeaningAndProblemType()
    {
        var refused = Record("result", "result/borrow-copy.copy-unavailable", "Copy Unavailable", "approved", "approved", """{"outcome": "refusal", "code": "lending.copy-not-available", "sentence": "This copy is on loan to another member."}""");
        var unknown = Record("result", "result/borrow-copy.card-expired", "Card Expired", "approved", "approved", """{"outcome": "refusal", "code": "lending.card-expired"}""");
        var success = Record("result", "result/borrow-copy.borrowed", "Borrowed", "approved", "approved", """{"outcome": "success", "meaning": "The copy is lent."}""");
        var model = NewModel(refused, unknown, success);
        model["refusals"] = new JsonArray(new JsonObject
        {
            ["code"] = "lending.copy-not-available",
            ["meaning"] = "The copy stays unavailable and the member is told it is on loan.",
        });
        model["problemTypeBase"] = "https://library.example/problems/";

        var details = DetailsOf(model);

        var joined = Obj(details["result/borrow-copy.copy-unavailable"], "fields");
        Assert.Equal("The copy stays unavailable and the member is told it is on loan.", Str(joined, "meaning"));
        Assert.Equal("https://library.example/problems/copy-not-available", Str(joined, "problemType"));
        Assert.Equal("This copy is on loan to another member.", Str(joined, "sentence"));

        var missing = Obj(details["result/borrow-copy.card-expired"], "fields");
        Assert.Null(missing["meaning"]);
        Assert.Equal("https://library.example/problems/card-expired", Str(missing, "problemType"));

        var kept = Obj(details["result/borrow-copy.borrowed"], "fields");
        Assert.Equal("The copy is lent.", Str(kept, "meaning"));
        Assert.Null(kept["problemType"]);
    }

    /// <summary>Without a problem type base a refusal result carries the short problem type: the code's part after its module.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void RefusalProblemTypeIsShortWithoutABase()
    {
        var refused = Record("result", "result/borrow-copy.copy-unavailable", "Copy Unavailable", "approved", "approved", """{"outcome": "refusal", "code": "lending.copy-not-available"}""");
        var model = NewModel(refused);
        model["refusals"] = new JsonArray(new JsonObject
        {
            ["code"] = "lending.copy-not-available",
            ["meaning"] = "The copy stays unavailable.",
        });

        var fields = Obj(DetailsOf(model)["result/borrow-copy.copy-unavailable"], "fields");
        Assert.Equal("The copy stays unavailable.", Str(fields, "meaning"));
        Assert.Equal("copy-not-available", Str(fields, "problemType"));
    }

    /// <summary>A screen fulfills its requirements and a served content entry fulfills its own: both read as fulfilling edges, kept-by counts and coverage reach.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void ScreenAndServedContentFulfillRequirements()
    {
        var requirement = Record("requirement", "requirement/self-checkout.late-fee", "Late Fee", "approved", "approved", """{"kind": "policy"}""");
        var screen = Record("screen", "screen/library-app.search", "Search", "approved", "approved", """{"address": "/search", "fulfills": ["requirement/self-checkout.late-fee"]}""");
        var entryPoint = Record("entry-point", "entry-point/library-app", "Library App", "approved", "approved", """{"kind": "browser", "servedContent": [{"path": "/terms", "contentType": "text/html", "cacheControl": "public, max-age=3600", "versioning": "content-hash", "fulfills": ["requirement/self-checkout.late-fee"]}]}""");
        var details = DetailsOf(NewModel(requirement, screen, entryPoint));

        Assert.True(HasEdge(Arr(details["screen/library-app.search"], "out"), "requirement/self-checkout.late-fee", "screen.fulfills"));
        Assert.True(HasEdge(Arr(details["entry-point/library-app"], "out"), "requirement/self-checkout.late-fee", "entry-point.servedContent.fulfills"));
        Assert.True(HasEdge(Arr(details["requirement/self-checkout.late-fee"], "in"), "screen/library-app.search", "screen.fulfills"));
        Assert.True(HasEdge(Arr(details["requirement/self-checkout.late-fee"], "in"), "entry-point/library-app", "entry-point.servedContent.fulfills"));

        var kept = Obj(details["requirement/self-checkout.late-fee"], "keptBy");
        Assert.Equal(1, kept["screen"]?.GetValue<int>());
        Assert.Equal(1, kept["entry-point"]?.GetValue<int>());

        var coverage = Obj(details["requirement/self-checkout.late-fee"], "intentCoverage");
        Assert.True(BoolOf(coverage["reached"]));
        Assert.Equal(
            new[] { "entry-point/library-app", "screen/library-app.search" },
            Arr(coverage, "reachedBy").Select(node => node!.GetValue<string>()).ToList());
    }

    /// <summary>A record detail carries the captures, evidence strength and model completion the model states beside the record, and computes code completion from proof.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void DetailsCarryCapturesAndEvidenceStrengthWithComputedCompletion()
    {
        var screen = Record("screen", "screen/home", "Home", fieldsJson: """{"address":"/home"}""");
        screen["completion"] = JsonNode.Parse("""{"stage":"coded","proof":"unknown","heldBy":[],"parkedBy":[],"inScope":1,"parked":0,"total":1,"counts":{}}""");
        screen["approval"] = JsonNode.Parse("""{"by":"Ada","rule":"product owner"}""");
        screen["captures"] = JsonNode.Parse("""[{"path":"shots/home.png","route":"/home","theme":"light","width":1440,"capturedOn":"2026-11-01"}]""");
        screen["captureState"] = "captured";
        var problem = Record("problem", "problem/slow-desk", "Slow Desk", fieldsJson: """{"statement":"The desk is slow.","evidence":[{"claim":"Desk takes a minute.","kind":"observation","source":"notes.md","readOn":"2026-11-01"}]}""");
        problem["evidenceStrength"] = "observed";

        var details = DetailsOf(NewModel(screen, problem));

        var screenDetail = details["screen/home"];
        Assert.Null(screenDetail["approval"]);
        var completion = Obj(screenDetail, "completion");
        Assert.False(BoolOf(completion["hasCode"]));
        Assert.Equal("coded", Str(completion, "stage"));
        Assert.Equal("unknown", Str(completion, "proof"));
        Assert.NotNull(completion["heldBy"]);
        Assert.Empty(Arr(completion, "heldBy"));
        Assert.NotNull(completion["parkedBy"]);
        Assert.Empty(Arr(completion, "parkedBy"));
        Assert.Equal(1, completion["inScope"]?.GetValue<int>());
        Assert.Equal(0, completion["parked"]?.GetValue<int>());
        Assert.Equal(1, completion["total"]?.GetValue<int>());
        Assert.NotNull(completion["counts"]);
        Assert.Empty(Obj(completion, "counts"));
        Assert.Single(Arr(screenDetail, "captures"));
        Assert.Equal("captured", Str(screenDetail, "captureState"));
        Assert.Equal("observed", Str(details["problem/slow-desk"], "evidenceStrength"));
    }

    /// <summary>Each detail carries the folder path and module the model states beside the record.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void DetailsCarryTheFolderAndTheModule()
    {
        var placed = Record("problem", "problem/slow-desk", "Slow Desk");
        placed["folder"] = "docs/intent/lending";
        placed["module"] = "lending";

        var details = DetailsOf(NewModel(placed));

        Assert.Equal("docs/intent/lending", Str(details["problem/slow-desk"], "folder"));
        Assert.Equal("lending", Str(details["problem/slow-desk"], "module"));
    }

    /// <summary>Completion reads from proof: code, tests and passing scenarios, each null when the record expects nothing there.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgments-in-the-book</proves>
    [Fact]
    public void CompletionIsComputedFromProof()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        var borrow = Obj(details["use-case/lending.borrow-copy"], "completion");
        Assert.True(BoolOf(borrow["hasCode"]));
        Assert.False(BoolOf(borrow["hasTests"]));
        var scenarios = Obj(borrow, "scenarios");
        Assert.Equal(2, scenarios["of"]?.GetValue<int>());
        Assert.Equal(1, scenarios["pass"]?.GetValue<int>());

        var proved = Obj(details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"], "completion");
        Assert.True(BoolOf(proved["hasTests"]));
        Assert.Null(proved["hasCode"]);
        Assert.Null(proved["scenarios"]);

        var goal = Obj(details["goal/faster-borrowing"], "completion");
        Assert.Null(goal["hasCode"]);
        Assert.Null(goal["hasTests"]);
        Assert.Null(goal["scenarios"]);
    }

    static JsonObject LoadFixture(string relative) =>
        (JsonObject)RecordText.Read(TestCli.Fixture(relative))!;

    static IReadOnlyDictionary<string, JsonObject> DetailsOf(JsonObject model) =>
        BookRecordDetails.Build(model, BookOutline.Build(model));

    static JsonObject NewModel(params JsonObject[] records)
    {
        var list = new JsonArray();
        foreach (var record in records)
        {
            list.Add(record.DeepClone());
        }

        var store = BookStores.WithIntentCoverage(new JsonObject
        {
            ["records"] = list,
            ["code"] = new JsonObject
            {
                ["handlers"] = new JsonArray(),
                ["endpoints"] = new JsonArray(),
                ["aggregateMethods"] = new JsonArray(),
                ["automations"] = new JsonArray(),
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
            ["stale"] = new JsonArray(),
            ["tests"] = new JsonArray(),
            ["browserTests"] = new JsonArray(),
            ["screens"] = new JsonArray(),
            ["exercisedSteps"] = new JsonArray(),
            ["findings"] = new JsonArray(),
            ["covered"] = new JsonArray(),
            ["testRun"] = null,
        });
        AddServedContentReach(store);
        return store;
    }

    /// <summary>Adds each entry point to the Intent coverage entries of the requirements its served content fulfills.</summary>
    /// <param name="store">The store object that holds the records and their Intent coverage.</param>
    static void AddServedContentReach(JsonObject store)
    {
        var servedBy = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        foreach (var record in (store["records"] as JsonArray ?? []).OfType<JsonObject>())
        {
            if (Str(record, "type") != "entry-point" || Str(record, "id") is not { Length: > 0 } id)
            {
                continue;
            }

            foreach (var entry in (record["fields"]?["servedContent"] as JsonArray ?? []).OfType<JsonObject>())
            {
                foreach (var target in EntryFulfills(entry["fulfills"]))
                {
                    if (!servedBy.TryGetValue(target, out var sources))
                    {
                        sources = new SortedSet<string>(StringComparer.Ordinal);
                        servedBy[target] = sources;
                    }

                    sources.Add(id);
                }
            }
        }

        if (servedBy.Count == 0 || store["intentCoverage"] is not JsonObject coverage)
        {
            return;
        }

        var entries = (coverage["entries"] as JsonArray ?? []).OfType<JsonObject>().ToList();

        foreach (var entry in entries)
        {
            if (Str(entry, "kind") != "requirement"
                || Str(entry, "id") is not { Length: > 0 } requirement
                || !servedBy.TryGetValue(requirement, out var sources))
            {
                continue;
            }

            var reachedBy = new SortedSet<string>(
                Arr(entry, "reachedBy").OfType<JsonValue>()
                    .Select(value => value.TryGetValue<string>(out var text) ? text : "")
                    .Where(text => text.Length > 0),
                StringComparer.Ordinal);
            reachedBy.UnionWith(sources);

            var array = new JsonArray();
            foreach (var source in reachedBy)
            {
                array.Add((JsonNode)source);
            }

            entry["reachedBy"] = array;
            entry["reached"] = reachedBy.Count > 0;
        }

        var reached = entries.Count(entry => BoolOf(entry["reached"]));
        coverage["reached"] = reached;
        coverage["total"] = entries.Count;
        coverage["share"] = JsonValue.Create(entries.Count == 0 ? 1.0 : (double)reached / entries.Count);
        var requirements = Obj(coverage, "requirements");
        requirements["reached"] = entries.Count(entry => Str(entry, "kind") == "requirement" && BoolOf(entry["reached"]));
        requirements["total"] = entries.Count(entry => Str(entry, "kind") == "requirement");
    }

    static List<string> EntryFulfills(JsonNode? value)
    {
        if (value is JsonValue scalar && scalar.TryGetValue<string>(out var text))
        {
            return text.Length > 0 ? [text] : [];
        }

        return (value as JsonArray ?? [])
            .OfType<JsonValue>()
            .Select(item => item.TryGetValue<string>(out var id) ? id : "")
            .Where(id => id.Length > 0)
            .ToList();
    }

    static JsonObject Record(string type, string id, string title, string status = "draft", string? effective = null, string fieldsJson = "{}", string? explanation = null)
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
            ["file"] = ".litespec/test.yaml",
            ["line"] = 3,
            ["fields"] = (JsonObject)JsonNode.Parse(fieldsJson)!,
            ["links"] = new JsonObject(),
        };
        if (explanation is not null)
        {
            record["explanation"] = explanation;
        }

        return record;
    }

    /// <summary>The explanation HTML of a record detail: its intro and each section body, in order.</summary>
    /// <param name="detail">The record detail that holds the sections.</param>
    /// <returns>The intro HTML followed by each section body.</returns>
    static string BodyOf(JsonObject? detail)
    {
        var sections = detail?["sections"] as JsonObject;
        var bodies = (sections?["list"] as JsonArray ?? new JsonArray()).Select(item => Str(item as JsonObject, "html") ?? "");
        return (Str(sections, "intro") ?? "") + string.Concat(bodies);
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
