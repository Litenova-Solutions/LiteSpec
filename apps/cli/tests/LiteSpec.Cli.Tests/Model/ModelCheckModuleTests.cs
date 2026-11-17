using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>litespec model check --module</c> prints only the findings on that module, with the exit code over those findings.</summary>
public sealed class ModelCheckModuleTests
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
              "responsibilities": ["The catalog lists every copy for readers."],
              "excludes": ["The catalog tracks no loans."]
            }
            """);

        var broken = Path.Join(root, ".litespec", "behavior", "modules", "catalog", "jobs", "sweep", "sweep.yaml");
        Directory.CreateDirectory(Path.GetDirectoryName(broken)!);
        File.WriteAllText(broken, "---\ntype: job\nid: job/catalog.sweep\ntitle: Sweep\n---\n");
    }

    /// <summary>A module check prints only that module's findings in text, and exits 1 on its blocking ones.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void CheckModuleKeepsOnlyThatModulesFindings()
    {
        using var fixture = ModelFixture.Prepare("large", "paging-undeclared");
        WriteCatalogWithBrokenFile(fixture.Path);

        var lending = TestCli.Run("model", "check", "--root", fixture.Path, "--module", "lending");

        Assert.Equal(1, lending.Status);
        Assert.Contains("[paging-undeclared]", lending.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[record-invalid]", lending.Output, StringComparison.Ordinal);

        var catalog = TestCli.Run("model", "check", "--root", fixture.Path, "--module", "catalog");

        Assert.Equal(1, catalog.Status);
        Assert.Contains("[record-invalid]", catalog.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[paging-undeclared]", catalog.Output, StringComparison.Ordinal);
    }

    /// <summary>A module check in JSON holds only that module's findings.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void CheckModuleJsonKeepsOnlyThatModulesFindings()
    {
        using var fixture = ModelFixture.Prepare("large", "paging-undeclared");
        WriteCatalogWithBrokenFile(fixture.Path);

        var lending = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json", "--module", "lending");

        Assert.Equal(1, lending.Status);
        Assert.Contains("paging-undeclared", lending.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("record-invalid", lending.Stdout, StringComparison.Ordinal);

        var catalog = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json", "--module", "catalog");

        Assert.Equal(1, catalog.Status);
        Assert.Contains("record-invalid", catalog.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("paging-undeclared", catalog.Stdout, StringComparison.Ordinal);
    }

    /// <summary>A module check of an unknown module is rejected with the known modules.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void CheckModuleUnknownIsRejected()
    {
        using var fixture = ModelFixture.Prepare("large");
        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--module", "nope");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown module 'nope'.", result.Output, StringComparison.Ordinal);
    }

    /// <summary>A module check with no module is rejected as a missing value.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void CheckModuleEmptyIsRejected()
    {
        using var fixture = ModelFixture.Prepare("large");
        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--module", "");

        Assert.Equal(2, result.Status);
        Assert.Contains("Option --module needs a value.", result.Output, StringComparison.Ordinal);
    }
}
