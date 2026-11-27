using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Details;
using LiteSpec.Cli.Book.Lexicon;
using LiteSpec.Cli.Book.Outline;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Book;

/// <summary>Proves the records-complete fields reach the book shards and per-type labels win.</summary>
public sealed class BookRecordsCompleteTests
{
    /// <summary>A problem detail carries failureCases from its record fields.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void ProblemDetailCarriesFailureCases()
    {
        var model = NewModel(
            Record(
                "problem",
                "problem/expenses-wait-weeks",
                "Expenses Wait Weeks",
                fieldsJson: """{"statement":"Expenses wait weeks.","failureCases":["An expense waits without a reminder."]}""",
                explanation: "# Expenses Wait Weeks\n\nExpenses wait weeks.\n"));
        var detail = DetailsOf(model)["problem/expenses-wait-weeks"];
        var fields = detail["fields"] as JsonObject;
        var cases = fields!["failureCases"] as JsonArray;
        Assert.NotNull(cases);
        Assert.Single(cases!);
        Assert.Equal("An expense waits without a reminder.", cases![0]!.GetValue<string>());
    }

    /// <summary>A problem detail without failureCases carries none, with no placeholder.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void ProblemDetailWithoutFailureCasesCarriesNone()
    {
        var model = NewModel(
            Record("problem", "problem/slow-desk", "Slow Desk", fieldsJson: """{"statement":"The desk is slow."}"""));
        var detail = DetailsOf(model)["problem/slow-desk"];
        Assert.Null((detail["fields"] as JsonObject)?["failureCases"]);
    }

    /// <summary>A typed field label wins over the plain entry of the same field.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void TypedLabelWinsOverPlainLabel()
    {
        using var temp = TestCli.Temp("book-typed-label");
        var lexicon = BookLexicon.Load(WriteHome(temp, ModelPage(), ToolsPage(), GlossaryPage()));

        Assert.Equal("Scope", lexicon.Fields["scope"]);
        Assert.Equal("Scope", lexicon.Fields["constraint.scope"]);
        Assert.Equal("Scope", BookLexicon.FieldLabel("scope", "constraint"));
        Assert.Equal("Scope", BookLexicon.FieldLabel("scope", "unknown-type"));

        Assert.Equal("States", lexicon.Fields["screen.states"]);
        Assert.Equal("States", BookLexicon.FieldLabel("states", "screen"));
        Assert.Equal("Fields", lexicon.Fields["domain-event.fields"]);
        Assert.Equal("Fields", BookLexicon.FieldLabel("fields", "domain-event"));

        var json = lexicon.ToJson()["fields"]!.AsObject();
        Assert.Equal("Scope", json["constraint.scope"]!.GetValue<string>());
        Assert.Equal("Scope", json["role.scope"]!.GetValue<string>());

        Assert.False(lexicon.Fields.ContainsKey("constraint.coverage"));
        Assert.False(lexicon.Fields.ContainsKey("screen.displayStates"));
        Assert.False(lexicon.Fields.ContainsKey("domain-event.carries"));
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
        return store;
    }

    static JsonObject Record(string type, string id, string title, string fieldsJson = "{}", string? explanation = null)
    {
        var record = new JsonObject
        {
            ["type"] = type,
            ["id"] = id,
            ["title"] = title,
            ["owner"] = "Ada",
            ["status"] = "draft",
            ["effectiveStatus"] = "draft",
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

    static string WriteHome(TempFolder temp, string? model, string? tools, string? glossary)
    {
        WriteFile(temp, ["docs", "core", "model.md"], model);
        WriteFile(temp, ["docs", "reference", "finding-codes.md"], tools);
        WriteFile(temp, ["docs", "reference", "glossary.md"], glossary);
        var schema = Path.Combine(temp.Path, "schemas", "record.schema.yaml");
        Directory.CreateDirectory(Path.GetDirectoryName(schema)!);
        File.Copy(Path.Combine(TestCli.Repository, "schemas", "record.schema.yaml"), schema);
        return temp.Path;
    }

    static void WriteFile(TempFolder temp, string[] segments, string? content)
    {
        if (content is null)
        {
            return;
        }

        var path = temp.Path;
        foreach (var segment in segments)
        {
            path = Path.Combine(path, segment);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    static string ModelPage() =>
        """
        # Project Model

        ## Concepts

        ### The Four Record Layers

        Each record layer answers one question.

        | Record layer | Question | Writes | Records |
        |:--|:--|:--|:--|
        | Intent | Why build it? | agents draft | product, goal |
        | Behavior | How does it behave? | agents | module, use-case |
        | Implementation | How is it built? | agents | code only |
        | Proof | How do we know? | the builder | tests |

        Governance records attach to any record layer.

        ### Record Types and Page Styles

        Each type answers one question.

        | Type | Record layer | Question |
        |:--|:--|:--|
        | `product` | intent | What is built? |

        ### Completion Stages

        Completion is computed from code, tests and passing scenarios.

        ### Check Levels

        Every check has a tier.

        ### Links

        A link is a typed reference from one record to another.

        ### The Book

        The book is the model made readable: it renders the in-memory model for people, and judges nothing itself. It lives in the Proof record layer.

        The book leads with completion, and it says `claimed` for a link and `verified` for proof: a `fulfills` link reads as a claim until its scenarios pass.
        """;

    static string ToolsPage() =>
        """
        # Model Command

        ## Description

        A change breaks intent in these ways:

        | Code | Meaning |
        |:---|:---|
        | `requirement-removed` | A requirement no longer exists. |

        The finding codes are:

        | Code | Level | Meaning |
        |:---|:---|:---|
        | `record-invalid` | G | A record fails the shape its type requires. |
        | `term-clash` | A | Two terms differ only in spelling. |
        """;

    static string GlossaryPage() =>
        """
        # Glossary

        ## Product and Domain Terms

        These terms name the records a specification is written in.

        ### Expense

        One cost a person claims back.

        Home: docs/core/intent.md.

        Layer: Intent.

        Source: plain English.
        """;
}
