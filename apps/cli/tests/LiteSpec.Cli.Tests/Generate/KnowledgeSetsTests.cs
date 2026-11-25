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

    /// <summary>Generate writes one table class per variant with a factory call per row, rendering each column as its mapping states.</summary>
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
        Assert.Contains("LoanLimit.Create(", text);
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
                new("value", "maxDays", "number", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal)),
                new("key", "key", "string", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal)),
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
                new("effectiveTo", "validTo", "date", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal)),
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
                new("value", "maxDays", "furlong", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal)),
                new("value", "amount", "money", "Library.Kernel.Money", "From", "EUR", new Dictionary<string, string>(StringComparer.Ordinal)),
                new("variant", "branch", "map", null, "From", null, new Dictionary<string, string>(StringComparer.Ordinal) { ["central"] = "CT" }),
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

    /// <summary>Appends a generate block to the fixture project file.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="yaml">The generate block YAML.</param>
    static void AppendGenerate(string root, string yaml) =>
        File.AppendAllText(Path.Combine(root, ".litespec", "project.yaml"), "\n" + yaml + "\n");
}
