using LiteSpec.Cli.Generate;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Generate;

/// <summary>Proves each generator renderer byte for byte against golden files, and the target configuration guard.</summary>
public sealed class GenerateGoldenTests
{
    /// <summary>Refusal codes render the golden C# class for the small fixture's lending refusals.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void RefusalCodesDotnetMatchesGolden()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: refusal-codes
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        GenerateGoldens.AssertEqual("refusal-codes-dotnet.txt", file.Content);
    }

    /// <summary>Refusal codes render the golden TypeScript file for the small fixture's lending refusals.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void RefusalCodesTypeScriptMatchesGolden()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: refusal-codes
              stack: svelte
              path: apps/web/src/generated
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        GenerateGoldens.AssertEqual("refusal-codes-typescript.txt", file.Content);
    }

    /// <summary>Permissions render the golden C# class for the small fixture's lending permissions.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void PermissionsDotnetMatchesGolden()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: permissions
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        GenerateGoldens.AssertEqual("permissions-dotnet.txt", file.Content);
    }

    /// <summary>Permissions render the golden TypeScript file for the small fixture's lending permissions.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void PermissionsTypeScriptMatchesGolden()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: permissions
              stack: svelte
              path: apps/web/src/generated
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        GenerateGoldens.AssertEqual("permissions-typescript.txt", file.Content);
    }

    /// <summary>Event decisions render the golden C# table for the small fixture's lending events.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void EventDecisionsDotnetMatchesGolden()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: event-decisions
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        GenerateGoldens.AssertEqual("event-decisions-dotnet.txt", file.Content);
    }

    /// <summary>Closed sets render the golden C# classes for a reference-data table with choices.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void ClosedSetsDotnetMatchesGolden()
    {
        using var temp = ModelFixture.Prepare("small");
        var folder = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "reference-data", "loan-kind");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "loan-kind.yaml"),
            """
            type: reference-data
            id: reference-data/lending.loan-kind
            title: Loan Kind
            summary: The kinds of loan the library lends.
            choices:
            - standard
            - reference-only
            rows:
            - value: standard
              key: standard-2026
              effectiveFrom: 2026-01-01
              source: decision/0001-loan-period
            """);
        AppendGenerate(temp.Path, """
            generate:
            - kind: closed-sets
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        GenerateGoldens.AssertEqual("closed-sets-dotnet.txt", file.Content);
    }

    /// <summary>Generate exits 2 naming the target and the folder when a target writes inside a generated client folder.</summary>
    /// <proves>litespec/core/model#keep-generate-targets-outside-other-tools-output-folders</proves>
    [Fact]
    public void GenerateRefusesATargetInsideAGeneratedClientFolder()
    {
        using var temp = ModelFixture.Prepare("small");
        ReplaceFrontends(temp.Path, """
            - name: web
              path: apps/web
              platform: svelte
              client:
                contract: packages/api/openapi.json
                generated: packages/api/src/generated
            """);
        AppendGenerate(temp.Path, """
            generate:
            - kind: refusal-codes
              stack: svelte
              path: packages/api/src/generated
            """);

        var result = TestCli.Run("generate", "--root", temp.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("refusal-codes", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("packages/api/src/generated", result.Stderr, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(temp.Path, "packages", "api", "src", "generated")));
    }

    /// <summary>Generate exits 2 naming both targets when two targets write the same file.</summary>
    /// <proves>litespec/core/model#keep-generate-targets-outside-other-tools-output-folders</proves>
    [Fact]
    public void GenerateRefusesTwoTargetsWritingTheSameFile()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: refusal-codes
              stack: react
              path: apps/web/src/generated
            - kind: refusal-codes
              stack: svelte
              path: apps/web/src/generated
            """);

        var result = TestCli.Run("generate", "--root", temp.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("refusal-codes", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("refusal-codes.ts", result.Stderr, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(temp.Path, "apps", "web", "src", "generated")));
    }

    /// <summary>Model check reports the target inside a generated client folder.</summary>
    /// <proves>litespec/core/model#keep-generate-targets-outside-other-tools-output-folders</proves>
    [Fact]
    public void ModelCheckReportsATargetInsideAGeneratedClientFolder()
    {
        using var temp = ModelFixture.Prepare("small");
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "web"));
        ReplaceFrontends(temp.Path, """
            - name: web
              path: apps/web
              platform: svelte
              client:
                contract: packages/api/openapi.json
                generated: packages/api/src/generated
            """);
        AppendGenerate(temp.Path, """
            generate:
            - kind: refusal-codes
              stack: svelte
              path: packages/api/src/generated
            """);

        var result = TestCli.Run("model", "check", "--root", temp.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("generate-target-invalid", result.Output, StringComparison.Ordinal);
    }

    /// <summary>Model check reports two targets writing the same file.</summary>
    /// <proves>litespec/core/model#keep-generate-targets-outside-other-tools-output-folders</proves>
    [Fact]
    public void ModelCheckReportsTwoTargetsWritingTheSameFile()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: refusal-codes
              stack: react
              path: apps/web/src/generated
            - kind: refusal-codes
              stack: svelte
              path: apps/web/src/generated
            """);

        var result = TestCli.Run("model", "check", "--root", temp.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("generate-target-invalid", result.Output, StringComparison.Ordinal);
    }

    /// <summary>Appends a generate block to the fixture project file.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="yaml">The generate block YAML.</param>
    static void AppendGenerate(string root, string yaml) =>
        File.AppendAllText(Path.Combine(root, ".litespec", "project.yaml"), "\n" + yaml + "\n");

    /// <summary>Replaces the fixture's empty frontends list with the given entries.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="yaml">The frontend entries YAML.</param>
    static void ReplaceFrontends(string root, string yaml)
    {
        var file = Path.Combine(root, ".litespec", "project.yaml");
        File.WriteAllText(file, File.ReadAllText(file).Replace("frontends: []", "frontends:\n" + yaml, StringComparison.Ordinal));
    }
}
