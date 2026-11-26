using LiteSpec.Cli.Audit;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Audit;

/// <summary>Tests for audit route collection: the screen records and the routes file.</summary>
public sealed class AuditRoutesTests
{
    static readonly string Root = Path.Combine(TestCli.Repository, "examples", "claimwick");

    /// <summary>Collection reads the static screen routes and skips the route with a parameter.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact(DisplayName = "collection reads the static screen routes and skips the route with a parameter")]
    public void CollectsStaticScreenRoutes()
    {
        var config = ConsumerConfig.Load(Root);
        var records = RecordReader.Read(config).Records;

        var routes = AuditRoutes.Collect(records);

        Assert.Equal(
            [
                new AuditRoute("/expenses", "screen/claimwick-app.expense-list"),
                new AuditRoute("/expenses/new", "screen/claimwick-app.new-expense"),
            ],
            routes);
    }

    /// <summary>A route holding a parameter marker is skipped.</summary>
    /// <param name="path">The route path.</param>
    /// <param name="parameters">Whether the path holds a parameter.</param>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Theory(DisplayName = "a route holding a parameter marker is skipped")]
    [InlineData("/expenses/{expenseId}", true)]
    [InlineData("/expenses/:expenseId", true)]
    [InlineData("/expenses/[expenseId]", true)]
    [InlineData("/expenses/*", true)]
    [InlineData("/expenses", false)]
    [InlineData("/expenses/new", false)]
    public void DetectsRoutesWithParameters(string path, bool parameters)
    {
        Assert.Equal(parameters, AuditRoutes.HasParameters(path));
    }

    /// <summary>A YAML routes file reads in file order, naming each route by its name or its path.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact(DisplayName = "a YAML routes file reads in file order, naming each route by its name or its path")]
    public void ReadsYamlRoutesFile()
    {
        using var folder = TestCli.Temp("audit-routes");
        var file = Path.Combine(folder.Path, "routes.yaml");
        File.WriteAllText(file, "- path: /expenses\n  name: Expense list\n- path: /expenses/new\n");

        var (routes, error) = AuditRoutes.ReadFile(file);

        Assert.Null(error);
        Assert.Equal(
            [
                new AuditRoute("/expenses", "Expense list"),
                new AuditRoute("/expenses/new", "/expenses/new"),
            ],
            routes);
    }

    /// <summary>A JSON routes file reads like a YAML one.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact(DisplayName = "a JSON routes file reads like a YAML one")]
    public void ReadsJsonRoutesFile()
    {
        using var folder = TestCli.Temp("audit-routes");
        var file = Path.Combine(folder.Path, "routes.json");
        File.WriteAllText(file, "[{\"path\": \"/expenses\", \"name\": \"Expense list\"}]");

        var (routes, error) = AuditRoutes.ReadFile(file);

        Assert.Null(error);
        Assert.Equal([new AuditRoute("/expenses", "Expense list")], routes);
    }

    /// <summary>A routes file that does not parse reports why.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact(DisplayName = "a routes file that does not parse reports why")]
    public void ReportsUnparseableRoutesFile()
    {
        using var folder = TestCli.Temp("audit-routes");
        var file = Path.Combine(folder.Path, "routes.json");
        File.WriteAllText(file, "{oops");

        var (routes, error) = AuditRoutes.ReadFile(file);

        Assert.Null(routes);
        Assert.StartsWith($"Routes file '{file}' is not valid YAML or JSON", error);
    }

    /// <summary>A routes file holding no list reports why.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact(DisplayName = "a routes file holding no list reports why")]
    public void ReportsRoutesFileWithoutList()
    {
        using var folder = TestCli.Temp("audit-routes");
        var file = Path.Combine(folder.Path, "routes.yaml");
        File.WriteAllText(file, "path: /expenses\n");

        var (routes, error) = AuditRoutes.ReadFile(file);

        Assert.Null(routes);
        Assert.Equal($"Routes file '{file}' holds no list of routes: write a list of entries with a path.", error);
    }

    /// <summary>A routes entry without a path reports its position.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact(DisplayName = "a routes entry without a path reports its position")]
    public void ReportsRoutesEntryWithoutPath()
    {
        using var folder = TestCli.Temp("audit-routes");
        var file = Path.Combine(folder.Path, "routes.yaml");
        File.WriteAllText(file, "- path: /expenses\n- name: New expense\n");

        var (routes, error) = AuditRoutes.ReadFile(file);

        Assert.Null(routes);
        Assert.Equal($"Routes file '{file}' entry 2 needs a path.", error);
    }
}
