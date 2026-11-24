using LiteSpec.Cli.Generate;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Refusals;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves each generated file exists and matches what the records render.</summary>
public sealed class GeneratedFileCheckTests
{
    /// <summary>A configured file that is missing reports generated drift.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MissingFileReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path, "refusal-codes", "dotnet", "apps/gen", "Library.{Module}");
        var model = ModelWithRefusal(temp.Path);

        var drift = Checker.Check(model).Findings.Single(finding => finding.Code == "generated-drift");

        Assert.Contains("LendingRefusalCodes.generated.cs", drift.Message, StringComparison.Ordinal);
        Assert.Contains("missing", drift.Message, StringComparison.Ordinal);
    }

    /// <summary>A configured file that differs from the rendered text reports generated drift.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void DifferingFileReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path, "refusal-codes", "dotnet", "apps/gen", "Library.{Module}");
        var model = ModelWithRefusal(temp.Path);
        var file = ExpectedFile(temp.Path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "// edited by hand\n");

        var drift = Checker.Check(model).Findings.Single(finding => finding.Code == "generated-drift");

        Assert.Contains("differs", drift.Message, StringComparison.Ordinal);
    }

    /// <summary>A configured file matching the rendered text reports no generated drift.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void CurrentFilePasses()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path, "refusal-codes", "dotnet", "apps/gen", "Library.{Module}");
        var model = ModelWithRefusal(temp.Path);
        var planned = GeneratePlan.Build(model);

        Assert.Empty(planned.Errors);
        var file = planned.Files.Single();
        Directory.CreateDirectory(Path.GetDirectoryName(file.AbsolutePath)!);
        File.WriteAllText(file.AbsolutePath, file.Content);

        Assert.DoesNotContain("generated-drift", TestModel.Codes(model));
    }

    /// <summary>A project with no generate targets reports no generated drift.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UnconfiguredProjectPasses()
    {
        using var temp = TestCli.Temp("checks");
        Directory.CreateDirectory(Path.Combine(temp.Path, ".litespec"));
        File.WriteAllText(Path.Combine(temp.Path, ".litespec", "project.yaml"), "selectedExtensions: []\n");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        Assert.DoesNotContain("generated-drift", TestModel.Codes(model));
    }

    /// <summary>An invalid target renders nothing and reports no generated drift.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void InvalidTargetIsSkipped()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path, "event-decisions", "svelte", "apps/gen", "");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        Assert.DoesNotContain("generated-drift", TestModel.Codes(model));
    }

    /// <summary>A model holding one declared refusal.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <returns>The project model holding the refusal.</returns>
    static ProjectModel ModelWithRefusal(string root)
    {
        var model = TestModel.Model(TestModel.Config(root));
        model.RefusalSets.Add(new RefusalEntry(
            "lending", "lending.copy-not-available", "The copy is on loan.",
            Path.Combine(root, "refusals.yaml"), null));
        return model;
    }

    /// <summary>The file the plan renders for the written target.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <returns>The absolute expected file path.</returns>
    static string ExpectedFile(string root) =>
        Path.Combine(root, "apps", "gen", "Lending", "LendingRefusalCodes.generated.cs");

    /// <summary>Writes a project file with one generate target.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <param name="kind">The target kind.</param>
    /// <param name="stack">The target stack.</param>
    /// <param name="path">The target path.</param>
    /// <param name="namespace">The target namespace, or empty for none.</param>
    static void WriteProject(string root, string kind, string stack, string path, string @namespace)
    {
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(
            Path.Combine(root, ".litespec", "project.yaml"),
            "selectedExtensions: []\n"
            + "generate:\n"
            + $"- kind: {kind}\n  stack: {stack}\n  path: {path}\n"
            + (@namespace.Length > 0 ? $"  namespace: {@namespace}\n" : ""));
    }
}
