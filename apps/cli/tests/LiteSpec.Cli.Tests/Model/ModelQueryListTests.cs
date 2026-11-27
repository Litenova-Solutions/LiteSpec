using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>litespec model query --list</c> prints every record of a type, optionally of one module.</summary>
public sealed class ModelQueryListTests
{
    /// <summary>Declares the catalog module with one use case, so a module filter has a second module to keep.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void WriteCatalogModule(string root)
    {
        TestRecords.Write(root, ".litespec/behavior/modules/catalog/module.yaml", """
            {
              "type": "module",
              "id": "module/catalog",
              "title": "Catalog",
              "dependsOn": []
            }
            """);
        TestRecords.Write(root, ".litespec/behavior/modules/catalog/use-cases/list-shelves/list-shelves.yaml", """
            {
              "type": "use-case",
              "id": "use-case/catalog.list-shelves",
              "title": "List shelves",
              "kind": "query"
            }
            """);
    }

    /// <summary>A list prints every record of the type, one line each with identifier, title and path.</summary>
    /// <proves>litespec/core/agent#select-task-context</proves>
    [Fact]
    public void ListPrintsEveryRecordOfTheType()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "query", "--root", fixture.Path, "--list", "use-case");

        Assert.Equal(0, result.Status);
        Assert.Contains(
            "use-case/lending.borrow-copy - Borrow copy (.litespec/behavior/modules/lending/aggregates/loans/use-cases/borrow-copy/borrow-copy.yaml)",
            result.Stdout,
            StringComparison.Ordinal);
        Assert.Contains("use-case/lending.list-loans - List loans (", result.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("rule/", result.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("result/", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>A list with a module keeps only the records of that module.</summary>
    /// <proves>litespec/core/agent#select-task-context</proves>
    [Fact]
    public void ListWithModuleKeepsOnlyThatModule()
    {
        using var fixture = ModelFixture.Prepare("small");
        WriteCatalogModule(fixture.Path);
        var result = TestCli.Run("model", "query", "--root", fixture.Path, "--list", "use-case", "--module", "catalog");

        Assert.Equal(0, result.Status);
        Assert.Contains("use-case/catalog.list-shelves - List shelves (", result.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("lending.", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>A list of the lending module keeps the lending use cases.</summary>
    /// <proves>litespec/core/agent#select-task-context</proves>
    [Fact]
    public void ListWithLendingModuleKeepsLendingUseCases()
    {
        using var fixture = ModelFixture.Prepare("small");
        WriteCatalogModule(fixture.Path);
        var result = TestCli.Run("model", "query", "--root", fixture.Path, "--list", "use-case", "--module", "lending");

        Assert.Equal(0, result.Status);
        Assert.Contains("use-case/lending.borrow-copy", result.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("catalog.", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>A list of an unknown type is rejected with the known types.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void ListUnknownTypeIsRejected()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "query", "--root", fixture.Path, "--list", "nope");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown record type 'nope'.", result.Output, StringComparison.Ordinal);
    }

    /// <summary>A list of an unknown module is rejected with the known modules.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void ListUnknownModuleIsRejected()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "query", "--root", fixture.Path, "--list", "use-case", "--module", "nope");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown module 'nope'.", result.Output, StringComparison.Ordinal);
    }

    /// <summary>A list with no type is rejected as a missing value.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void ListWithEmptyTypeIsRejected()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "query", "--root", fixture.Path, "--list", "");

        Assert.Equal(2, result.Status);
        Assert.Contains("Option --list needs a value.", result.Output, StringComparison.Ordinal);
    }
}
