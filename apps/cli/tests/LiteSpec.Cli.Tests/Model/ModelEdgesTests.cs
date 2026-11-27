using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>litespec model edges</c> reports the record links between modules and every mutual pair.</summary>
public sealed class ModelEdgesTests
{
    /// <summary>Declares the catalog module with an aggregate and a use case that reads the lending loan aggregate.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void WriteCatalogLink(string root)
    {
        TestRecords.Write(root, ".litespec/behavior/modules/catalog/module.yaml", """
            {
              "type": "module",
              "id": "module/catalog",
              "title": "Catalog",
              "dependsOn": []
            }
            """);
        TestRecords.Write(root, ".litespec/behavior/modules/catalog/aggregates/shelves/aggregate.yaml", """
            {
              "type": "aggregate",
              "id": "aggregate/catalog.shelf",
              "title": "Shelf"
            }
            """);
        TestRecords.Write(root, ".litespec/behavior/modules/catalog/aggregates/shelves/use-cases/list-shelves/list-shelves.yaml", """
            {
              "type": "use-case",
              "id": "use-case/catalog.list-shelves",
              "title": "List shelves",
              "kind": "query",
              "reads": ["aggregate/lending.loan"]
            }
            """);
    }

    /// <summary>Writes a lending use case that reads the catalog shelf aggregate, closing the pair both ways.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void WriteLendingLink(string root)
    {
        TestRecords.Write(root, ".litespec/behavior/modules/lending/use-cases/probe-shelf/probe-shelf.yaml", """
            {
              "type": "use-case",
              "id": "use-case/lending.probe-shelf",
              "title": "Probe shelf",
              "kind": "query",
              "reads": ["aggregate/catalog.shelf"]
            }
            """);
    }

    /// <summary>Edges with one module reports no links and no mutual pair.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void EdgesWithOneModuleReportsNoLinks()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "edges", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("Module lending:", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Outgoing: 0 links to 0 modules", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Incoming: 0 links from 0 modules", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Mutual: 0 pairs link both ways", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Edges reports the cross-module links per kind and module, and the pair that links both ways.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void EdgesReportsCrossModuleLinksAndMutualPairs()
    {
        using var fixture = ModelFixture.Prepare("small");
        WriteCatalogLink(fixture.Path);
        WriteLendingLink(fixture.Path);
        var result = TestCli.Run("model", "edges", "--root", fixture.Path);

        Assert.Equal(0, result.Status);

        var catalog = result.Stdout.IndexOf("Module catalog:", StringComparison.Ordinal);
        var lending = result.Stdout.IndexOf("Module lending:", StringComparison.Ordinal);

        Assert.True(catalog >= 0 && lending >= 0);
        Assert.True(catalog < lending, "The modules print in alphabetical order.");
        Assert.Contains("reads: 1 (lending: 1)", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("reads: 1 (catalog: 1)", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Mutual: 1 pairs link both ways", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("catalog <-> lending", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Edges reports a queries edge under its verb.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void EdgesReportsQueriesEdgeUnderItsVerb()
    {
        using var fixture = ModelFixture.Prepare("small");
        TestRecords.Write(fixture.Path, ".litespec/behavior/modules/catalog/use-cases/read-shelf/read-shelf.yaml", """
            {
              "type": "use-case",
              "id": "use-case/catalog.read-shelf",
              "title": "Read shelf",
              "kind": "query",
              "effects": [{"queries": "use-case/lending.list-loans", "timing": "inline"}]
            }
            """);
        var result = TestCli.Run("model", "edges", "--root", fixture.Path, "--module", "catalog");

        Assert.Equal(0, result.Status);
        Assert.Contains("queries: 1 (lending: 1)", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Edges reports a runs edge under its verb.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void EdgesReportsRunsEdgeUnderItsVerb()
    {
        using var fixture = ModelFixture.Prepare("small");
        TestRecords.Write(fixture.Path, ".litespec/behavior/modules/catalog/use-cases/confirm-shelf/confirm-shelf.yaml", """
            {
              "type": "use-case",
              "id": "use-case/catalog.confirm-shelf",
              "title": "Confirm shelf",
              "kind": "command",
              "effects": [{"runs": "use-case/lending.borrow-copy", "timing": "inline"}]
            }
            """);
        var result = TestCli.Run("model", "edges", "--root", fixture.Path, "--module", "catalog");

        Assert.Equal(0, result.Status);
        Assert.Contains("runs: 1 (lending: 1)", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Edges with a module prints only that module with the mutual pairs it belongs to.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void EdgesModulePrintsOnlyThatModule()
    {
        using var fixture = ModelFixture.Prepare("small");
        WriteCatalogLink(fixture.Path);
        WriteLendingLink(fixture.Path);
        var result = TestCli.Run("model", "edges", "--root", fixture.Path, "--module", "catalog");

        Assert.Equal(0, result.Status);
        Assert.Contains("Module catalog:", result.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("Module lending:", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("catalog <-> lending", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Edges with a module folder that holds records but no module record prints that module.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void EdgesModuleWithRecordsButNoModuleRecordPrintsThatModule()
    {
        using var fixture = ModelFixture.Prepare("small");
        TestRecords.Write(fixture.Path, ".litespec/behavior/modules/catalog/use-cases/list-shelves/list-shelves.yaml", """
            {
              "type": "use-case",
              "id": "use-case/catalog.list-shelves",
              "title": "List shelves",
              "kind": "query"
            }
            """);
        var result = TestCli.Run("model", "edges", "--root", fixture.Path, "--module", "catalog");

        Assert.Equal(0, result.Status);
        Assert.Contains("Module catalog:", result.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("Module lending:", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Edges of an unknown module is rejected with the known modules.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void EdgesUnknownModuleIsRejected()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "edges", "--root", fixture.Path, "--module", "nope");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown module 'nope'.", result.Output, StringComparison.Ordinal);
    }

    /// <summary>Edges with no module is rejected as a missing value.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void EdgesEmptyModuleIsRejected()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "edges", "--root", fixture.Path, "--module", "");

        Assert.Equal(2, result.Status);
        Assert.Contains("Option --module needs a value.", result.Output, StringComparison.Ordinal);
    }
}
