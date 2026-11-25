using System.Text.Json;
using LiteSpec.Cli.Generate;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Generate;

/// <summary>Proves <c>litespec generate</c> writes knowledge sets from reference-data rows: one table class per area and variant with a row per record row.</summary>
public sealed class KnowledgeSetsTests
{
    /// <summary>Each Claimwick travel set renders byte for byte as its golden file, with rows in key order.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void GeneratedClaimwickTravelSetsMatchGoldens()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var model = ModelBuilder.Build(ConsumerConfig.Load(root));
        var plan = GeneratePlan.Build(model);
        var files = plan.Files
            .Where(file => file.AbsolutePath.Contains("Knowledge", StringComparison.Ordinal))
            .OrderBy(file => file.AbsolutePath, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(Goldens.Count, files.Count);

        foreach (var file in files)
        {
            Assert.True(
                Goldens.TryGetValue(Path.GetFileName(file.AbsolutePath), out var golden),
                $"No golden names '{Path.GetFileName(file.AbsolutePath)}'.");
            GenerateGoldens.AssertEqual(golden, file.Content);
        }
    }

    /// <summary>Each new column kind renders byte for byte as its golden file: a decimal rate, a nested closed-set case, and a nested object.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void GeneratedNewColumnKindsMatchGoldens()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteNewKindRecords(temp.Path);
        AppendGenerate(temp.Path, """
            generate:
            - kind: knowledge-sets
              stack: dotnet
              path: apps/api/src/Knowledge/Library.{Area}.{Variant}
              namespace: Library.{Area}.{Variant}
            """);

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var files = GeneratePlan.Build(model).Files
            .OrderBy(file => file.AbsolutePath, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(NewKindGoldens.Count, files.Count);

        foreach (var file in files)
        {
            Assert.True(
                NewKindGoldens.TryGetValue(Path.GetFileName(file.AbsolutePath), out var golden),
                $"No golden names '{Path.GetFileName(file.AbsolutePath)}'.");
            GenerateGoldens.AssertEqual(golden, file.Content);
        }
    }

    /// <summary>Each list and singleton kind renders byte for byte as its golden file: scalar, key and case lists, an object list, and a singleton value.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void GeneratedListAndSingletonKindsMatchGoldens()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteListRecords(temp.Path);
        AppendGenerate(temp.Path, """
            generate:
            - kind: knowledge-sets
              stack: dotnet
              path: apps/api/src/Knowledge/Library.{Area}.{Variant}
              namespace: Library.{Area}.{Variant}
            """);

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var files = GeneratePlan.Build(model).Files
            .OrderBy(file => file.AbsolutePath, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(ListGoldens.Count, files.Count);

        foreach (var file in files)
        {
            Assert.True(
                ListGoldens.TryGetValue(Path.GetFileName(file.AbsolutePath), out var golden),
                $"No golden names '{Path.GetFileName(file.AbsolutePath)}'.");
            GenerateGoldens.AssertEqual(golden, file.Content);
        }
    }

    /// <summary>A singleton renders one value from its row, through the constructor without a factory and through the factory when the table names one.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SingletonRendersValue()
    {
        using var document = JsonDocument.Parse("""
            {"value": 21, "variant": "central", "maxHolds": 5, "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"}
            """);
        var row = document.RootElement;
        var mappings = new List<KnowledgeSetsRenderer.ColumnMapping>
        {
            new("value", "defaultLoanDays", "number", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal), []),
            new("maxHolds", "maxHolds", "number", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal), []),
        };

        var built = KnowledgeSetsRenderer.RenderSingletonDotnet(
            "lending", "central", "lending-facts", "reference-data/lending.lending-facts",
            "Library.Lending.LendingFacts", null, mappings, row, "Library.Lending.Central");
        var factored = KnowledgeSetsRenderer.RenderSingletonDotnet(
            "lending", "central", "lending-facts", "reference-data/lending.lending-facts",
            "Library.Lending.LendingFacts", "Create", mappings, row, "Library.Lending.Central");

        Assert.Contains("public static partial class CentralLendingFacts", built);
        Assert.Contains("public static LendingFacts Value { get; } = new LendingFacts(", built);
        Assert.DoesNotContain("LendingFacts.Create(", built);
        Assert.Contains("defaultLoanDays: 21,", built);
        Assert.Contains("maxHolds: 5\n    );", built);
        Assert.Contains("public static LendingFacts Value { get; } = LendingFacts.Create(", factored);
    }

    /// <summary>A list renders its items in written order, an empty list renders empty, and a missing or non-list value renders null or a literal.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ListRendersItemsEmptyAndNull()
    {
        using var document = JsonDocument.Parse("""
            {"rows": [
            {"channels": ["email", "sms"], "key": "a-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"},
            {"channels": [], "key": "b-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"},
            {"key": "c-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"},
            {"channels": "email", "key": "d-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"}
            ]}
            """);
        var rows = document.RootElement.GetProperty("rows").EnumerateArray().ToList();

        var text = KnowledgeSetsRenderer.RenderDotnet(
            "lending",
            "central",
            "notice-rules",
            "reference-data/lending.notice-rules",
            "Library.Lending.HoldNotice",
            null,
            [
                new("channels", "channels", "list", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal), [], "string"),
            ],
            rows,
            "Library.Lending.Central");

        Assert.Contains("channels: [\"email\", \"sms\"]", text);
        Assert.Contains("channels: []", text);
        Assert.Contains("channels: null", text);
        Assert.Contains("channels: \"email\"", text);
    }

    /// <summary>Key and case items render through the model factory and as new cases, each in written order.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ListOfKeysAndCasesRenderThroughModels()
    {
        using var document = JsonDocument.Parse("""
            {"rows": [
            {"branchKeys": ["central", "harbor"], "noticeKinds": ["ready"], "key": "a-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"}
            ]}
            """);
        var rows = document.RootElement.GetProperty("rows").EnumerateArray().ToList();

        var text = KnowledgeSetsRenderer.RenderDotnet(
            "lending",
            "central",
            "notice-rules",
            "reference-data/lending.notice-rules",
            "Library.Lending.HoldNotice",
            null,
            [
                new("branchKeys", "branchKeys", "list", "Library.Lending.BranchKey", "Parse", null, new Dictionary<string, string>(StringComparer.Ordinal), [], "key"),
                new("noticeKinds", "noticeKinds", "list", "Library.Lending.NoticeKind", "From", null, new Dictionary<string, string>(StringComparer.Ordinal), [], "case"),
            ],
            rows,
            "Library.Lending.Central");

        Assert.Contains("branchKeys: [BranchKey.Parse(\"central\"), BranchKey.Parse(\"harbor\")],", text);
        Assert.Contains("noticeKinds: [new NoticeKind.Ready()]", text);
        Assert.Contains("using Library.Lending;", text);
    }

    /// <summary>Object items render one construction each; an item stating nothing renders null, and a partial item renders null for its missing fields.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ObjectItemsRenderNullWhenStatingNothing()
    {
        using var document = JsonDocument.Parse("""
            {"rows": [
            {"bands": [{}, {"minDays": 8}], "key": "a-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"}
            ]}
            """);
        var rows = document.RootElement.GetProperty("rows").EnumerateArray().ToList();

        var text = KnowledgeSetsRenderer.RenderDotnet(
            "lending",
            "central",
            "fine-bands",
            "reference-data/lending.fine-bands",
            "Library.Lending.FineBand",
            null,
            [
                new(
                    "bands",
                    "bands",
                    "list",
                    "Library.Lending.FineSlice",
                    "From",
                    null,
                    new Dictionary<string, string>(StringComparer.Ordinal),
                    [
                        new("minDays", "minDays", "number", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal), []),
                        new("maxDays", "maxDays", "number", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal), []),
                    ],
                    "object"),
            ],
            rows,
            "Library.Lending.Central");

        Assert.Contains("bands: [", text);
        Assert.Contains("null,", text);
        Assert.Contains("minDays: 8,", text);
        Assert.Contains("maxDays: null", text);
    }

    /// <summary>An item of the wrong kind renders as a literal: a scalar where an object item belongs.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ObjectItemOfWrongKindRendersLiteral()
    {
        using var document = JsonDocument.Parse("""
            {"rows": [
            {"bands": ["flat"], "key": "a-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"}
            ]}
            """);
        var rows = document.RootElement.GetProperty("rows").EnumerateArray().ToList();

        var text = KnowledgeSetsRenderer.RenderDotnet(
            "lending",
            "central",
            "fine-bands",
            "reference-data/lending.fine-bands",
            "Library.Lending.FineBand",
            null,
            [
                new(
                    "bands",
                    "bands",
                    "list",
                    "Library.Lending.FineSlice",
                    "From",
                    null,
                    new Dictionary<string, string>(StringComparer.Ordinal),
                    [new("minDays", "minDays", "number", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal), [])],
                    "object"),
            ],
            rows,
            "Library.Lending.Central");

        Assert.Contains("\"flat\"", text);
        Assert.DoesNotContain("new FineSlice(", text);
    }

    /// <summary>Mapped items render their literals, and an item the map skips renders as written.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MapItemsRenderMappedLiterals()
    {
        using var document = JsonDocument.Parse("""
            {"rows": [
            {"branchCodes": ["central", "harbor"], "key": "a-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"}
            ]}
            """);
        var rows = document.RootElement.GetProperty("rows").EnumerateArray().ToList();

        var text = KnowledgeSetsRenderer.RenderDotnet(
            "lending",
            "central",
            "notice-rules",
            "reference-data/lending.notice-rules",
            "Library.Lending.HoldNotice",
            null,
            [
                new("branchCodes", "branchCodes", "list", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal) { ["central"] = "CT" }, [], "map"),
            ],
            rows,
            "Library.Lending.Central");

        Assert.Contains("branchCodes: [\"CT\", \"harbor\"]", text);
    }

    /// <summary>A list nested in an object reads its row field: the reminder list inside the renewal window.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ListInsideObjectReadsRowField()
    {
        using var document = JsonDocument.Parse("""
            {"rows": [
            {"windowDays": 3, "reminders": ["email"], "key": "a-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"}
            ]}
            """);
        var rows = document.RootElement.GetProperty("rows").EnumerateArray().ToList();

        var text = KnowledgeSetsRenderer.RenderDotnet(
            "lending",
            "central",
            "renewal-rules",
            "reference-data/lending.renewal-rules",
            "Library.Lending.RenewalRule",
            null,
            [
                new(
                    "",
                    "window",
                    "object",
                    "Library.Lending.RenewalWindow",
                    "From",
                    null,
                    new Dictionary<string, string>(StringComparer.Ordinal),
                    [
                        new("windowDays", "days", "number", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal), []),
                        new("reminders", "reminders", "list", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal), [], "string"),
                    ]),
            ],
            rows,
            "Library.Lending.Central");

        Assert.Contains("window: new RenewalWindow(", text);
        Assert.Contains("reminders: [\"email\"]", text);
    }

    /// <summary>Rows build through the constructor when the table names no factory, and through the factory when it names one.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void RowsBuildThroughConstructorWithoutAFactory()
    {
        using var document = JsonDocument.Parse("""
            {"rows": [
            {"value": 21, "key": "a-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"}
            ]}
            """);
        var rows = document.RootElement.GetProperty("rows").EnumerateArray().ToList();
        var mappings = new List<KnowledgeSetsRenderer.ColumnMapping>
        {
            new("value", "maxDays", "number", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal), []),
        };

        var built = KnowledgeSetsRenderer.RenderDotnet(
            "lending", "central", "loan-limits", "reference-data/lending.loan-limits",
            "Library.Lending.LoanLimit", null, mappings, rows, "Library.Lending.Central");
        var factored = KnowledgeSetsRenderer.RenderDotnet(
            "lending", "central", "loan-limits", "reference-data/lending.loan-limits",
            "Library.Lending.LoanLimit", "Create", mappings, rows, "Library.Lending.Central");

        Assert.Contains("new LoanLimit(", built);
        Assert.DoesNotContain("LoanLimit.Create(", built);
        Assert.Contains("LoanLimit.Create(", factored);
    }

    /// <summary>A case column renders a new nested case, and null when the row states no value.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void CaseRendersNewCaseAndNull()
    {
        using var document = JsonDocument.Parse("""
            {"rows": [
            {"role": "standard-loan", "key": "a-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"},
            {"key": "b-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"}
            ]}
            """);
        var rows = document.RootElement.GetProperty("rows").EnumerateArray().ToList();

        var text = KnowledgeSetsRenderer.RenderDotnet(
            "lending",
            "central",
            "loan-classes",
            "reference-data/lending.loan-classes",
            "Library.Lending.LoanTerm",
            null,
            [
                new("role", "kind", "case", "Library.Lending.LoanKind", "From", null, new Dictionary<string, string>(StringComparer.Ordinal), []),
            ],
            rows,
            "Library.Lending.Central");

        Assert.Contains("kind: new LoanKind.StandardLoan()", text);
        Assert.Contains("kind: null", text);
        Assert.Contains("using Library.Lending;", text);
    }

    /// <summary>A decimal column renders its suffix, and a literal when the value states no number.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void DecimalRendersSuffixed()
    {
        using var document = JsonDocument.Parse("""
            {"rows": [
            {"ratePercent": 19, "key": "a-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"},
            {"ratePercent": "high", "key": "b-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"}
            ]}
            """);
        var rows = document.RootElement.GetProperty("rows").EnumerateArray().ToList();

        var text = KnowledgeSetsRenderer.RenderDotnet(
            "lending",
            "central",
            "fine-rates",
            "reference-data/lending.fine-rates",
            "Library.Lending.FineRate",
            null,
            [
                new("ratePercent", "rate", "decimal", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal), []),
            ],
            rows,
            "Library.Lending.Central");

        Assert.Contains("rate: 19m", text);
        Assert.Contains("rate: \"high\"", text);
    }

    /// <summary>An object column renders null without a model type, whatever fields the row states.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ObjectWithoutTypeRendersNull()
    {
        using var document = JsonDocument.Parse("""
            {"rows": [
            {"windowDays": 3, "key": "a-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"}
            ]}
            """);
        var rows = document.RootElement.GetProperty("rows").EnumerateArray().ToList();

        var text = KnowledgeSetsRenderer.RenderDotnet(
            "lending",
            "central",
            "renewal-rules",
            "reference-data/lending.renewal-rules",
            "Library.Lending.RenewalRule",
            null,
            [
                new(
                    "",
                    "window",
                    "object",
                    null,
                    "From",
                    null,
                    new Dictionary<string, string>(StringComparer.Ordinal),
                    [new("windowDays", "days", "number", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal), [])]),
            ],
            rows,
            "Library.Lending.Central");

        Assert.Contains("window: null", text);
    }

    /// <summary>Generate writes one table class per variant with a constructor call per row, rendering each column as its mapping states.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GenerateWritesKnowledgeSets()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteKnowledgeRecords(temp.Path);
        AppendGenerate(temp.Path, """
            generate:
            - kind: knowledge-sets
              stack: dotnet
              path: apps/api/src/Knowledge/Library.{Area}.{Variant}
              namespace: Library.{Area}.{Variant}
            """);

        var result = TestCli.Run("generate", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        var central = Path.Combine(temp.Path, "apps", "api", "src", "Knowledge", "Library.Lending.Central", "CentralLoanLimits.generated.cs");
        var harbor = Path.Combine(temp.Path, "apps", "api", "src", "Knowledge", "Library.Lending.Harbor", "HarborLoanLimits.generated.cs");
        Assert.True(File.Exists(central), "central table file");
        Assert.True(File.Exists(harbor), "harbor table file");

        var text = File.ReadAllText(central);
        Assert.Contains("namespace Library.Lending.Central;", text);
        Assert.Contains("public static partial class CentralLoanLimits", text);
        Assert.Contains("public static IReadOnlyList<LoanLimit> Rows", text);
        Assert.Contains("new LoanLimit(", text);
        Assert.Contains("branch: \"CT\",", text);
        Assert.Contains("key: LoanLimitKey.From(\"central-standard-2026\"),", text);
        Assert.Contains("maxDays: 21,", text);
        Assert.Contains("validFrom: new DateOnly(2026, 1, 1),", text);
        Assert.Contains("source: \"decision/0001-loan-period\"\n        ),", text);

        var harborText = File.ReadAllText(harbor);
        Assert.Contains("branch: \"HB\",", harborText);
        Assert.Contains("maxDays: 14,", harborText);
        Assert.DoesNotContain("central-standard-2026", harborText);
    }

    /// <summary>Generate refuses a knowledge-sets target on a TypeScript stack, which writes no C#.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GenerateRefusesKnowledgeSetsOnATypeScriptStack()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: knowledge-sets
              stack: svelte
              path: apps/web/src/generated
            """);

        var result = TestCli.Run("generate", "--root", temp.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("knowledge-sets", result.Stderr, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(temp.Path, "apps", "web", "src", "generated")));
    }

    /// <summary>Generate refuses a knowledge-sets target holding a module placeholder, which knowledge sets do not read.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GenerateRefusesKnowledgeSetsWithAModulePlaceholder()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: knowledge-sets
              stack: dotnet
              path: apps/api/src/Knowledge/Library.{Module}
              namespace: Library.Knowledge
            """);

        var result = TestCli.Run("generate", "--root", temp.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("{area}", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Rows render in key order, then by start date, whatever order the record writes them in.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void RowsOrderByKeyThenStartDate()
    {
        using var document = JsonDocument.Parse("""
            {"rows": [
            {"value": 14, "key": "b-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"},
            {"value": 21, "key": "a-2027", "effectiveFrom": "2027-01-01", "source": "decision/0001-loan-period"},
            {"value": 20, "key": "a-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"}
            ]}
            """);
        var rows = document.RootElement.GetProperty("rows").EnumerateArray().ToList();

        var text = KnowledgeSetsRenderer.RenderDotnet(
            "lending",
            "central",
            "loan-limits",
            "reference-data/lending.loan-limits",
            "Library.Lending.LoanLimit",
            "Create",
            [
                new("value", "maxDays", "number", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal), []),
                new("key", "key", "string", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal), []),
            ],
            rows,
            "Library.Lending.Central");

        Assert.True(
            text.IndexOf("a-2026", StringComparison.Ordinal) < text.IndexOf("a-2027", StringComparison.Ordinal),
            "Rows with one key order by start date.");
        Assert.True(
            text.IndexOf("a-2027", StringComparison.Ordinal) < text.IndexOf("b-2026", StringComparison.Ordinal),
            "Rows order by key.");
    }

    /// <summary>A column the row does not state renders as null: an open end date stays open.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MissingColumnRendersNull()
    {
        using var document = JsonDocument.Parse("""
            {"rows": [
            {"value": 21, "key": "a-2026", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"}
            ]}
            """);
        var rows = document.RootElement.GetProperty("rows").EnumerateArray().ToList();

        var text = KnowledgeSetsRenderer.RenderDotnet(
            "lending",
            "central",
            "loan-limits",
            "reference-data/lending.loan-limits",
            "Library.Lending.LoanLimit",
            "Create",
            [
                new("effectiveTo", "validTo", "date", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal), []),
            ],
            rows,
            "Library.Lending.Central");

        Assert.Contains("validTo: null", text);
        Assert.DoesNotContain("using System;", text);
    }

    /// <summary>A value the mapping cannot build renders as a literal: an unknown rendering, a money value of text, and a map miss.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UnbuildableValuesRenderLiterals()
    {
        using var document = JsonDocument.Parse("""
            {"rows": [
            {"value": "high", "variant": "rural", "effectiveFrom": "2026-01-01", "source": "decision/0001-loan-period"}
            ]}
            """);
        var rows = document.RootElement.GetProperty("rows").EnumerateArray().ToList();

        var text = KnowledgeSetsRenderer.RenderDotnet(
            "lending",
            "central",
            "loan-limits",
            "reference-data/lending.loan-limits",
            "Library.Lending.LoanLimit",
            "Create",
            [
                new("value", "maxDays", "furlong", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal), []),
                new("value", "amount", "money", "Library.Kernel.Money", "From", "EUR", new Dictionary<string, string>(StringComparer.Ordinal), []),
                new("variant", "branch", "map", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal) { ["central"] = "CT" }, []),
            ],
            rows,
            "Library.Lending.Central");

        Assert.Contains("maxDays: \"high\",", text);
        Assert.Contains("amount: \"high\",", text);
        Assert.Contains("branch: \"rural\"", text);
        Assert.DoesNotContain("using Library.Kernel;", text);
    }

    /// <summary>Knowledge placeholders resolve both spellings, and only knowledge placeholders count as knowledge ones.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void KnowledgePlaceholdersResolve()
    {
        Assert.Equal("Library.Lending.Central", GenerateNaming.ResolveKnowledge("Library.{Area}.{Variant}", "lending", "central"));
        Assert.Equal("apps/lending/central", GenerateNaming.ResolveKnowledge("apps/{area}/{variant}", "lending", "central"));
        Assert.True(GenerateNaming.HasKnowledgePlaceholder("Library.{Area}.{Variant}"));
        Assert.False(GenerateNaming.HasKnowledgePlaceholder("Library.{Module}"));
        Assert.False(GenerateNaming.HasKnowledgePlaceholder("Library.Knowledge"));
    }

    /// <summary>The golden file by generated file name, one per Claimwick travel set.</summary>
    static readonly IReadOnlyDictionary<string, string> Goldens = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["GermanyMileageRates.generated.cs"] = "knowledge-sets-dotnet-germany-mileage-rates.txt",
        ["GermanyPerDiemRates.generated.cs"] = "knowledge-sets-dotnet-germany-per-diem-rates.txt",
        ["NetherlandsMileageRates.generated.cs"] = "knowledge-sets-dotnet-netherlands-mileage-rates.txt",
        ["NetherlandsPerDiemRates.generated.cs"] = "knowledge-sets-dotnet-netherlands-per-diem-rates.txt",
    };

    /// <summary>The golden file by generated file name, one per new column kind.</summary>
    static readonly IReadOnlyDictionary<string, string> NewKindGoldens = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["CentralFineRates.generated.cs"] = "knowledge-sets-dotnet-central-fine-rates.txt",
        ["CentralLoanClasses.generated.cs"] = "knowledge-sets-dotnet-central-loan-classes.txt",
        ["CentralRenewalRules.generated.cs"] = "knowledge-sets-dotnet-central-renewal-rules.txt",
    };

    /// <summary>The golden file by generated file name, one per list and singleton kind.</summary>
    static readonly IReadOnlyDictionary<string, string> ListGoldens = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["CentralNoticeRules.generated.cs"] = "knowledge-sets-dotnet-central-notice-rules.txt",
        ["CentralFineBands.generated.cs"] = "knowledge-sets-dotnet-central-fine-bands.txt",
        ["CentralLendingFacts.generated.cs"] = "knowledge-sets-dotnet-central-lending-facts.txt",
    };

    /// <summary>Writes a knowledge area with one loan limit table into the fixture consumer.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteKnowledgeRecords(string root)
    {
        var area = Path.Combine(root, ".litespec", "behavior", "knowledge", "lending");
        Directory.CreateDirectory(area);
        File.WriteAllText(
            Path.Combine(area, "knowledge-area.yaml"),
            """
            type: knowledge-area
            id: knowledge-area/lending
            title: Lending
            meaning: The loan rules the library looks up by branch.
            variants:
            - central
            - harbor
            """);
        var table = Path.Combine(area, "reference-data", "loan-limits");
        Directory.CreateDirectory(table);
        File.WriteAllText(
            Path.Combine(table, "loan-limits.yaml"),
            """
            type: reference-data
            id: reference-data/lending.loan-limits
            title: Loan Limits
            summary: The longest loan per branch.
            unit: days
            requiredRoles:
            - standard
            variation: branch
            row:
              type: Library.Lending.LoanLimit
            columns:
            - column: variant
              parameter: branch
              as: map
              literals:
                central: CT
                harbor: HB
            - column: key
              parameter: key
              as: key
              type: Library.Lending.LoanLimitKey
            - column: value
              parameter: maxDays
              as: number
            - column: effectiveFrom
              parameter: validFrom
              as: date
            - column: source
              parameter: source
              as: string
            rows:
            - value: 21
              key: central-standard-2026
              role: standard
              variant: central
              effectiveFrom: 2026-01-01
              source: decision/0001-loan-period
            - value: 14
              key: harbor-standard-2026
              role: standard
              variant: harbor
              effectiveFrom: 2026-01-01
              source: decision/0001-loan-period
            """);
    }

    /// <summary>Writes a knowledge area with one table per new column kind into the fixture consumer.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteNewKindRecords(string root)
    {
        var area = Path.Combine(root, ".litespec", "behavior", "knowledge", "lending");
        Directory.CreateDirectory(area);
        File.WriteAllText(
            Path.Combine(area, "knowledge-area.yaml"),
            """
            type: knowledge-area
            id: knowledge-area/lending
            title: Lending
            meaning: The loan rules the library looks up by branch.
            variants:
            - central
            """);
        WriteTable(
            area,
            "fine-rates",
            """
            type: reference-data
            id: reference-data/lending.fine-rates
            title: Fine Rates
            summary: The daily fine per overdue day.
            unit: EUR per day
            row:
              type: Library.Lending.FineRate
            columns:
            - column: key
              parameter: key
              as: key
              type: Library.Lending.FineRateKey
              factory: Parse
            - column: value
              parameter: dailyRate
              as: decimal
            - column: source
              parameter: source
              as: string
            rows:
            - value: 0.50
              key: central-standard-2026
              variant: central
              effectiveFrom: 2026-01-01
              source: decision/0001-loan-period
            """);
        WriteTable(
            area,
            "loan-classes",
            """
            type: reference-data
            id: reference-data/lending.loan-classes
            title: Loan Classes
            summary: The longest loan per class.
            row:
              type: Library.Lending.LoanTerm
            columns:
            - column: key
              parameter: key
              as: key
              type: Library.Lending.LoanTermKey
            - column: role
              parameter: kind
              as: case
              type: Library.Lending.LoanKind
            - column: value
              parameter: maxDays
              as: number
            - column: source
              parameter: source
              as: string
            rows:
            - value: 21
              key: central-standard-2026
              role: standard-loan
              variant: central
              effectiveFrom: 2026-01-01
              source: decision/0001-loan-period
            - value: 7
              key: central-short-2026
              variant: central
              effectiveFrom: 2026-01-01
              source: decision/0001-loan-period
            """);
        WriteTable(
            area,
            "renewal-rules",
            """
            type: reference-data
            id: reference-data/lending.renewal-rules
            title: Renewal Rules
            summary: When each loan renews.
            row:
              type: Library.Lending.RenewalRule
            columns:
            - column: key
              parameter: key
              as: key
              type: Library.Lending.RenewalRuleKey
            - parameter: window
              as: object
              type: Library.Lending.RenewalWindow
              columns:
              - column: windowDays
                parameter: days
                as: number
              - column: windowEndsOn
                parameter: endsOn
                as: date
              - parameter: grace
                as: object
                type: Library.Lending.RenewalGrace
                columns:
                - column: graceDays
                  parameter: days
                  as: number
            - column: source
              parameter: source
              as: string
            rows:
            - value: 1
              key: central-open-2026
              variant: central
              windowDays: 3
              windowEndsOn: 2026-12-31
              graceDays: 1
              effectiveFrom: 2026-01-01
              source: decision/0001-loan-period
            - value: 1
              key: central-plain-2026
              variant: central
              windowDays: 5
              effectiveFrom: 2026-01-01
              source: decision/0001-loan-period
            - value: 1
              key: central-closed-2026
              variant: central
              effectiveFrom: 2026-01-01
              source: decision/0001-loan-period
            """);
    }

    /// <summary>Writes a knowledge area with one table per list and singleton kind into the fixture consumer.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteListRecords(string root)
    {
        var area = Path.Combine(root, ".litespec", "behavior", "knowledge", "lending");
        Directory.CreateDirectory(area);
        File.WriteAllText(
            Path.Combine(area, "knowledge-area.yaml"),
            """
            type: knowledge-area
            id: knowledge-area/lending
            title: Lending
            meaning: The loan rules the library looks up by branch.
            variants:
            - central
            """);
        WriteTable(
            area,
            "notice-rules",
            """
            type: reference-data
            id: reference-data/lending.notice-rules
            title: Notice Rules
            summary: When each hold notice goes out.
            row:
              type: Library.Lending.HoldNotice
            columns:
            - column: key
              parameter: key
              as: key
              type: Library.Lending.HoldNoticeKey
            - column: channels
              parameter: channels
              as: list
              items: string
            - column: branchKeys
              parameter: branchKeys
              as: list
              items: key
              type: Library.Lending.BranchKey
              factory: Parse
            - column: noticeKinds
              parameter: noticeKinds
              as: list
              items: case
              type: Library.Lending.NoticeKind
            - column: source
              parameter: source
              as: string
            rows:
            - value: 1
              key: central-ready-2026
              variant: central
              channels: [email, sms]
              branchKeys: [central, harbor]
              noticeKinds: [ready, lapsed]
              effectiveFrom: 2026-01-01
              source: decision/0001-loan-period
            - value: 1
              key: central-collected-2026
              variant: central
              channels: []
              effectiveFrom: 2026-01-01
              source: decision/0001-loan-period
            """);
        WriteTable(
            area,
            "fine-bands",
            """
            type: reference-data
            id: reference-data/lending.fine-bands
            title: Fine Bands
            summary: The daily fine per overdue band.
            row:
              type: Library.Lending.FineBand
            columns:
            - column: key
              parameter: key
              as: key
              type: Library.Lending.FineBandKey
            - column: bands
              parameter: bands
              as: list
              items: object
              type: Library.Lending.FineSlice
              columns:
              - column: minDays
                parameter: minDays
                as: number
              - column: maxDays
                parameter: maxDays
                as: number
              - column: dailyRate
                parameter: dailyRate
                as: decimal
            - column: source
              parameter: source
              as: string
            rows:
            - value: 1
              key: central-standard-2026
              variant: central
              bands:
              - minDays: 1
                maxDays: 7
                dailyRate: 0.50
              - minDays: 8
                dailyRate: 1.00
              - {}
              effectiveFrom: 2026-01-01
              source: decision/0001-loan-period
            - value: 1
              key: central-short-2026
              variant: central
              bands: []
              effectiveFrom: 2026-01-01
              source: decision/0001-loan-period
            """);
        WriteTable(
            area,
            "lending-facts",
            """
            type: reference-data
            id: reference-data/lending.lending-facts
            title: Lending Facts
            summary: The facts every lending lookup shares.
            singleton: true
            row:
              type: Library.Lending.LendingFacts
            columns:
            - column: value
              parameter: defaultLoanDays
              as: number
            - column: maxHolds
              parameter: maxHolds
              as: number
            - column: homeBranch
              parameter: homeBranch
              as: string
            - column: openWeekdays
              parameter: openWeekdays
              as: list
              items: string
            rows:
            - value: 21
              variant: central
              maxHolds: 5
              homeBranch: central
              openWeekdays: [monday, tuesday, wednesday, thursday, friday]
              effectiveFrom: 2026-01-01
              source: decision/0001-loan-period
            """);
    }

    /// <summary>Writes one reference-data table into the fixture knowledge area.</summary>
    /// <param name="area">The fixture knowledge area folder.</param>
    /// <param name="table">The table name.</param>
    /// <param name="yaml">The table record YAML.</param>
    static void WriteTable(string area, string table, string yaml)
    {
        var folder = Path.Combine(area, "reference-data", table);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, table + ".yaml"), yaml);
    }

    /// <summary>Appends a generate block to the fixture project file.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="yaml">The generate block YAML.</param>
    static void AppendGenerate(string root, string yaml) =>
        File.AppendAllText(Path.Combine(root, ".litespec", "project.yaml"), "\n" + yaml + "\n");
}
