using System.Text.Json.Nodes;
using LiteSpec.Cli.Book;
using LiteSpec.Cli.Book.Details;
using LiteSpec.Cli.Book.Outline;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Book;

/// <summary>Proves the book writer emits closed contract sets with stable shapes.</summary>
public sealed class BookContractTests
{
    /// <summary>Every detail carries lacks as an array: empty with nothing lacking, listing each missing mark otherwise.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void LacksIsAlwaysAnArray()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        foreach (var pair in details)
        {
            Assert.IsType<JsonArray>(pair.Value["lacks"]);
        }

        Assert.Empty(Arr(details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"], "lacks"));
        Assert.True(HasString(Arr(details["use-case/lending.borrow-copy"], "lacks"), "scenarios not tested"));
    }

    /// <summary>Proof states are the closed seven, and every scenario and rule proof reads one of them.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void ProofStatesAreTheClosedSeven()
    {
        string[] expected = ["awaiting", "failing", "indirect", "norun", "passing", "unconfirmed", "unproved"];
        Assert.Equal(expected, BookProofStates.All.OrderBy(state => state, StringComparer.Ordinal).ToList());
        var seen = 0;
        foreach (var details in new[] { DetailsOf(LoadFixture("Book/review.json")), DetailsOf(NorunFixture()) })
        {
            foreach (var pair in details)
            {
                if (Str(pair.Value, "type") is "scenario" or "rule")
                {
                    seen++;
                    Assert.Contains(Str(Obj(pair.Value, "proof"), "state") ?? string.Empty, BookProofStates.All);
                }
            }
        }

        Assert.True(seen > 0, "The fixture holds no scenario or rule.");
    }

    /// <summary>Outline kinds are the closed five, and every outline node reads one of them.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void OutlineKindsAreTheClosedFive()
    {
        string[] expected = ["code", "finding", "group", "record", "test"];
        Assert.Equal(expected, BookOutlineKinds.All.OrderBy(kind => kind, StringComparer.Ordinal).ToList());
        var outline = BookOutline.Build(LoadFixture("Book/review.json"));
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<BookOutlineNode>(outline.Layers);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            seen.Add(node.Kind);
            Assert.Contains(node.Kind, BookOutlineKinds.All);
            foreach (var child in node.Children)
            {
                stack.Push(child);
            }
        }

        Assert.Contains(BookOutlineKinds.Record, seen);
        Assert.Contains(BookOutlineKinds.Group, seen);
    }

    /// <summary>Implementation kinds are the closed fourteen, and every impl entry reads one of them.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void ImplKindsAreTheClosedFourteen()
    {
        string[] expected = ["adapter", "aggregate-method", "aggregate-root", "automation", "domain-service", "endpoint", "event", "handler", "job", "port", "reaction", "result", "route", "rule-exception"];
        Assert.Equal(expected, BookImplKinds.All.OrderBy(kind => kind, StringComparer.Ordinal).ToList());
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
                ["path"] = "src/App/Port.cs",
                ["to"] = "use-case/lending.borrow-copy",
                ["kind"] = "port",
            },
            new JsonObject
            {
                ["path"] = "src/App/Root.cs",
                ["to"] = "use-case/lending.borrow-copy",
                ["kind"] = "aggregate-root",
            });
        var impl = Arr(DetailsOf(model)["use-case/lending.borrow-copy"], "impl");
        Assert.Equal(3, impl.Count);
        foreach (var entry in impl.OfType<JsonObject>())
        {
            Assert.Contains(Str(entry, "kind") ?? string.Empty, BookImplKinds.All);
        }

        foreach (var pair in DetailsOf(LoadFixture("Book/review.json")))
        {
            foreach (var entry in Arr(pair.Value, "impl").OfType<JsonObject>())
            {
                Assert.Contains(Str(entry, "kind") ?? string.Empty, BookImplKinds.All);
            }
        }
    }

    /// <summary>Check levels are G, S and A, and every written finding tier and level reads one of them.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void CheckLevelsAreGSAAndFindingsCarryThem()
    {
        string[] expected = ["A", "G", "S"];
        Assert.Equal(expected, BookCheckLevels.All.OrderBy(level => level, StringComparer.Ordinal).ToList());
        var model = NewModel(Record("use-case", "use-case/lending.borrow-copy", "Borrow Copy", "approved", "approved"));
        model["findings"] = new JsonArray(
            new JsonObject
            {
                ["code"] = "sample-g",
                ["tier"] = "G",
                ["message"] = "A G finding.",
                ["subject"] = "use-case/lending.borrow-copy",
            },
            new JsonObject
            {
                ["code"] = "sample-s",
                ["tier"] = "S",
                ["message"] = "An S finding.",
                ["subject"] = "use-case/lending.borrow-copy",
            },
            new JsonObject
            {
                ["code"] = "sample-a",
                ["level"] = "A",
                ["message"] = "An A finding.",
                ["subject"] = "use-case/lending.borrow-copy",
            });
        var findings = Arr(DetailsOf(model)["use-case/lending.borrow-copy"], "findings");
        Assert.Equal(3, findings.Count);
        foreach (var finding in findings.OfType<JsonObject>())
        {
            Assert.Contains(Str(finding, "tier") ?? string.Empty, BookCheckLevels.All);
            Assert.Contains(Str(finding, "level") ?? string.Empty, BookCheckLevels.All);
        }
    }

    /// <summary>Capture states are the closed three, carried through from the model onto the screen detail.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void CaptureStatesAreCarriedThrough()
    {
        string[] expected = ["captured", "no-route", "route-not-captured"];
        Assert.Equal(expected, BookCaptureStates.All.OrderBy(state => state, StringComparer.Ordinal).ToList());
        var screen = Record("screen", "screen/library.search", "Search");
        screen["captures"] = new JsonArray(new JsonObject
        {
            ["path"] = "shots/search-light-1280.png",
            ["theme"] = "light",
            ["width"] = 1280,
            ["capturedOn"] = "2026-11-01",
        });
        screen["captureState"] = "captured";
        var detail = DetailsOf(NewModel(screen))["screen/library.search"];
        Assert.Contains(Str(detail, "captureState") ?? string.Empty, BookCaptureStates.All);
        Assert.IsType<JsonArray>(detail["captures"]);
    }

    /// <summary>Computed fields keep one shape each: objects stay objects, arrays stay arrays, and a route is an object or absent.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void ComputedFieldsKeepTheirShapes()
    {
        var details = DetailsOf(LoadFixture("Book/review.json"));
        var journey = details["journey/borrowing"];
        var servers = Assert.IsType<JsonObject>(journey["stepServers"]);
        var screens = Assert.IsType<JsonObject>(journey["stepScreens"]);
        foreach (var steps in new[] { servers, screens })
        {
            foreach (var pair in steps)
            {
                Assert.IsType<JsonArray>(pair.Value);
            }
        }

        Assert.IsType<JsonArray>(journey["exercised"]);
        Assert.IsType<JsonObject>(details["requirement/self-checkout.max-wait"]["keptBy"]);
        var seenScreen = false;
        var seenConstraint = false;
        foreach (var pair in details)
        {
            if (Str(pair.Value, "type") == "screen")
            {
                seenScreen = true;
                Assert.True(pair.Value["route"] is null || pair.Value["route"] is JsonObject, $"Unexpected route shape for {pair.Key}.");
                Assert.IsType<JsonArray>(pair.Value["calls"]);
            }

            if (Str(pair.Value, "type") == "constraint")
            {
                seenConstraint = true;
                Assert.IsType<JsonArray>(pair.Value["requires"]);
            }
        }

        Assert.True(seenScreen, "The fixture holds no screen.");
        Assert.True(seenConstraint, "The fixture holds no constraint.");
    }

    /// <summary>Every written type is a record type and every written layer is a layer key or null.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void TypeAndLayerAreClosed()
    {
        foreach (var pair in DetailsOf(LoadFixture("Book/review.json")))
        {
            Assert.Contains(Str(pair.Value, "type") ?? string.Empty, RecordTypes.All);
            var layer = Str(pair.Value, "layer");
            Assert.True(
                layer is null || BookOutline.LayerKeys.Contains(layer),
                $"Unexpected layer {layer} for {pair.Key}.");
        }
    }

    static JsonObject LoadFixture(string relative) =>
        (JsonObject)RecordText.Read(TestCli.Fixture(relative))!;

    static JsonObject NorunFixture()
    {
        var model = LoadFixture("Book/review.json");
        model["testRun"] = null;
        return model;
    }

    static IReadOnlyDictionary<string, JsonObject> DetailsOf(JsonObject model) =>
        BookRecordDetails.Build(model, BookOutline.Build(model));

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
        };
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

    static string? Str(JsonObject? obj, string name) =>
        obj?[name] is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? text : null;

    static JsonArray Arr(JsonObject obj, string name) => obj[name] as JsonArray ?? new JsonArray();

    static JsonObject Obj(JsonObject obj, string name) => obj[name] as JsonObject ?? new JsonObject();

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
