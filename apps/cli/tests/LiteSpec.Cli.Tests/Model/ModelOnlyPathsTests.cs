using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet.Backend;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>litespec model check --only-paths</c> reports only the findings on the named paths and their records, and that <c>--module</c> limits the backend read to that module's projects.</summary>
public sealed class ModelOnlyPathsTests
{
    /// <summary>Declares the catalog module with one record file the reader refuses, so it reports a finding of its own.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void WriteCatalogWithBrokenFile(string root)
    {
        TestRecords.Write(root, ".litespec/behavior/modules/catalog/module.yaml", """
            {
              "type": "module",
              "id": "module/catalog",
              "title": "Catalog",
              "dependsOn": [],
              "covers": ["The catalog lists every copy for readers."],
              "excludes": ["The catalog tracks no loans."]
            }
            """);

        var broken = Path.Join(root, ".litespec", "behavior", "modules", "catalog", "jobs", "sweep", "sweep.yaml");
        Directory.CreateDirectory(Path.GetDirectoryName(broken)!);
        File.WriteAllText(broken, "---\ntype: job\ntitle: Sweep\n---\n");
    }

    /// <summary>Writes an unmarked helper class into the lending module, so the check reports a mark finding on a code file.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void WriteUnmarkedHelper(string root)
    {
        var file = Path.Join(root, "apps", "api", "src", "Modules", "Library.Lending", "Application", "Loans", "BorrowCopy", "BorrowCopyHelper.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "namespace Library.Lending.Application.Loans.BorrowCopy;\n\ninternal sealed class BorrowCopyHelper\n{\n    public string Note() => \"note\";\n}\n");
    }

    /// <summary>A scoped check prints only the findings on the named paths and their records.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void CheckOnlyPathsKeepsOnlyNamedPathFindings()
    {
        using var fixture = ModelFixture.Prepare("large", "paging-undeclared");
        WriteCatalogWithBrokenFile(fixture.Path);

        var catalog = TestCli.Run("model", "check", "--root", fixture.Path, "--only-paths", ".litespec/behavior/modules/catalog");

        Assert.Equal(1, catalog.Status);
        Assert.Contains("[record-invalid]", catalog.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[paging-undeclared]", catalog.Output, StringComparison.Ordinal);

        var lending = TestCli.Run("model", "check", "--root", fixture.Path, "--only-paths", ".litespec/behavior/modules/lending/aggregates/loans/use-cases/list-loans/list-loans.yaml");

        Assert.Equal(1, lending.Status);
        Assert.Contains("[paging-undeclared]", lending.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[record-invalid]", lending.Output, StringComparison.Ordinal);
    }

    /// <summary>A scoped check in JSON holds only the findings on the named paths and their records.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void CheckOnlyPathsJsonKeepsOnlyNamedPathFindings()
    {
        using var fixture = ModelFixture.Prepare("large", "paging-undeclared");
        WriteCatalogWithBrokenFile(fixture.Path);

        var catalog = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json", "--only-paths", ".litespec/behavior/modules/catalog");

        Assert.Equal(1, catalog.Status);
        Assert.Contains("record-invalid", catalog.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("paging-undeclared", catalog.Stdout, StringComparison.Ordinal);
    }

    /// <summary>A scoped check keeps the code findings on a named file, with no record scope.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void CheckOnlyPathsKeepsCodeFileFindings()
    {
        using var fixture = ModelFixture.Prepare("large");
        WriteCatalogWithBrokenFile(fixture.Path);
        WriteUnmarkedHelper(fixture.Path);

        var helper = TestCli.Run("model", "check", "--root", fixture.Path, "--only-paths", "apps/api/src/Modules/Library.Lending/Application/Loans/BorrowCopy/BorrowCopyHelper.cs");

        Assert.Equal(1, helper.Status);
        Assert.Contains("[mark-missing]", helper.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[record-invalid]", helper.Output, StringComparison.Ordinal);
    }

    /// <summary>A record scope omits code findings on files outside it, where a module check keeps them.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void CheckOnlyPathsRecordScopeOmitsOtherScopeCodeFindings()
    {
        using var fixture = ModelFixture.Prepare("large", "paging-undeclared");
        WriteUnmarkedHelper(fixture.Path);

        var scoped = TestCli.Run("model", "check", "--root", fixture.Path, "--only-paths", ".litespec/behavior/modules/lending/aggregates/loans/use-cases/list-loans");

        Assert.Contains("[paging-undeclared]", scoped.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[mark-missing]", scoped.Output, StringComparison.Ordinal);

        var module = TestCli.Run("model", "check", "--root", fixture.Path, "--module", "lending");

        Assert.Contains("[paging-undeclared]", module.Output, StringComparison.Ordinal);
        Assert.Contains("[mark-missing]", module.Output, StringComparison.Ordinal);
    }

    /// <summary>Module and path scopes combine: only the findings in both remain.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void CheckOnlyPathsWithModuleKeepsIntersection()
    {
        using var fixture = ModelFixture.Prepare("large", "paging-undeclared");
        WriteCatalogWithBrokenFile(fixture.Path);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--module", "lending", "--only-paths", ".litespec/behavior/modules/catalog");

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("[paging-undeclared]", result.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[record-invalid]", result.Output, StringComparison.Ordinal);
    }

    /// <summary>A scoped check with no path is rejected as a missing value.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void CheckOnlyPathsEmptyIsRejected()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--only-paths");

        Assert.Equal(2, result.Status);
        Assert.Contains("Option --only-paths needs a value.", result.Output, StringComparison.Ordinal);
    }

    /// <summary>A module read keeps only that module's projects.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ModuleReadKeepsOnlyModuleProjects()
    {
        using var temp = TestCli.Temp("only-paths");
        WriteTwoModuleProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("Modules", "Library.Lending", "Application", "Borrow"), "BorrowCommand.cs", """
            namespace Library.Lending.Application.Borrow;

            // Lending marker.
            public sealed class BorrowCommand
            {
            }
            """);
        WriteSource(temp.Path, Path.Combine("Modules", "Library.Catalog", "Application", "Add"), "AddCommand.cs", """
            namespace Library.Catalog.Application.Add;

            // Catalog marker.
            public sealed class AddCommand
            {
            }
            """);

        var config = ConsumerConfig.Load(temp.Path);
        var scoped = DotnetBackendReader.ReadForModule(config, "lending");

        Assert.Contains(scoped.Code.Comments, comment => comment.File.EndsWith("BorrowCommand.cs", StringComparison.Ordinal));
        Assert.DoesNotContain(scoped.Code.Comments, comment => comment.File.EndsWith("AddCommand.cs", StringComparison.Ordinal));
    }

    /// <summary>A module read skips a sibling file no parse survives, so a concurrent edit elsewhere does not fail the run.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ModuleReadSkipsUnparsableSiblingFile()
    {
        using var temp = TestCli.Temp("only-paths");
        WriteTwoModuleProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("Modules", "Library.Lending", "Application", "Borrow"), "BorrowCommand.cs", """
            namespace Library.Lending.Application.Borrow;

            // Lending marker.
            public sealed class BorrowCommand
            {
            }
            """);
        WriteSource(temp.Path, Path.Combine("Modules", "Library.Catalog", "Application", "Add"), "Broken.cs", "class { broken");

        var config = ConsumerConfig.Load(temp.Path);

        Assert.Contains(new DotnetBackendReader().ReadWithProblems(config).Problems, problem => problem.Path.EndsWith("Broken.cs", StringComparison.Ordinal));

        var scoped = DotnetBackendReader.ReadForModule(config, "lending");

        Assert.DoesNotContain(scoped.Problems, problem => problem.Path.EndsWith("Broken.cs", StringComparison.Ordinal));
        Assert.Empty(scoped.Problems);
    }

    /// <summary>Writes a consumer with two module projects, so a module read can skip one.</summary>
    /// <param name="root">The temporary consumer root folder.</param>
    static void WriteTwoModuleProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {"modules": "src/Modules/*"}}}
            """);

    /// <summary>Writes one source file under the temporary consumer.</summary>
    /// <param name="root">The temporary consumer root folder.</param>
    /// <param name="project">The project folder under <c>src</c>.</param>
    /// <param name="file">The file name.</param>
    /// <param name="text">The file text.</param>
    static void WriteSource(string root, string project, string file, string text)
    {
        var folder = Path.Combine(root, "src", project);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }
}
