using System.Text.Json.Nodes;
using LiteSpec.Cli.Audit;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Audit;

/// <summary>Tests that <c>litespec ui audit</c> writes the route-suite check result the model check reads.</summary>
public sealed class UiAuditCheckResultTests
{
    /// <summary>An audit with no findings writes a passing route-suite check result and exits zero.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact(DisplayName = "an audit with no findings writes a passing route-suite check result and exits zero")]
    public void AuditWithNoFindingsWritesPassingResult()
    {
        using var consumer = TestCli.Temp("audit-result");
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);
        var routes = WriteRoutes(consumer.Path);
        AuditBrowser.Runner = (_, _, _, _) => Task.FromResult<AuditBrowser.Run?>(new AuditBrowser.Run([], ["expenses--light--390.png"]));

        try
        {
            var result = TestCli.RunInProcess(
                "ui", "audit", "--base-url", "http://localhost:4173", "--routes", routes,
                "--root", consumer.Path, "--out", Path.Combine(consumer.Path, "ui-audit"));

            Assert.Equal(0, result.Status);
            var json = ReadResult(consumer.Path);
            Assert.Equal("route-suite", json["check"]!.GetValue<string>());
            Assert.True(json["passed"]!.GetValue<bool>());
            Assert.True(json["value"]!.GetValue<string>().Length > 0);
        }
        finally
        {
            AuditBrowser.Runner = null;
        }
    }

    /// <summary>An audit with findings writes a failing route-suite check result and exits one.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact(DisplayName = "an audit with findings writes a failing route-suite check result and exits one")]
    public void AuditWithFindingsWritesFailingResult()
    {
        using var consumer = TestCli.Temp("audit-result");
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);
        var routes = WriteRoutes(consumer.Path);
        AuditBrowser.Runner = (_, _, _, _) => Task.FromResult<AuditBrowser.Run?>(new AuditBrowser.Run(
            [new AuditRawFinding("text-code-invalid", "main", "The page shows the code 'null'.", "/expenses", "light", 390)],
            ["expenses--light--390.png"]));

        try
        {
            var result = TestCli.RunInProcess(
                "ui", "audit", "--base-url", "http://localhost:4173", "--routes", routes,
                "--root", consumer.Path, "--out", Path.Combine(consumer.Path, "ui-audit"));

            Assert.Equal(1, result.Status);
            var json = ReadResult(consumer.Path);
            Assert.Equal("route-suite", json["check"]!.GetValue<string>());
            Assert.False(json["passed"]!.GetValue<bool>());
            Assert.True(json["value"]!.GetValue<string>().Length > 0);
        }
        finally
        {
            AuditBrowser.Runner = null;
        }
    }

    /// <summary>An audit with findings and fail-on none still writes a failing route-suite check result and exits zero.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact(DisplayName = "an audit with findings and fail-on none still writes a failing route-suite check result and exits zero")]
    public void AuditWithFindingsAndFailOnNoneWritesFailingResult()
    {
        using var consumer = TestCli.Temp("audit-result");
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);
        var routes = WriteRoutes(consumer.Path);
        AuditBrowser.Runner = (_, _, _, _) => Task.FromResult<AuditBrowser.Run?>(new AuditBrowser.Run(
            [new AuditRawFinding("text-code-invalid", "main", "The page shows the code 'null'.", "/expenses", "light", 390)],
            ["expenses--light--390.png"]));

        try
        {
            var result = TestCli.RunInProcess(
                "ui", "audit", "--base-url", "http://localhost:4173", "--routes", routes,
                "--fail-on", "none", "--root", consumer.Path, "--out", Path.Combine(consumer.Path, "ui-audit"));

            Assert.Equal(0, result.Status);
            var json = ReadResult(consumer.Path);
            Assert.Equal("route-suite", json["check"]!.GetValue<string>());
            Assert.False(json["passed"]!.GetValue<bool>());
            Assert.True(json["value"]!.GetValue<string>().Length > 0);
        }
        finally
        {
            AuditBrowser.Runner = null;
        }
    }

    /// <summary>Writes a routes file with one route.</summary>
    /// <param name="root">The consumer root.</param>
    /// <returns>The routes file path.</returns>
    static string WriteRoutes(string root)
    {
        var routes = Path.Combine(root, "routes.yaml");
        File.WriteAllText(routes, "- path: /expenses\n  name: Expense list\n");
        return routes;
    }

    /// <summary>Reads the route-suite check result the audit wrote.</summary>
    /// <param name="root">The consumer root.</param>
    /// <returns>The parsed check result.</returns>
    static JsonObject ReadResult(string root)
    {
        var file = Path.Combine(ConsumerConfig.Load(root).CheckResultsDir, "route-suite.json");
        return (JsonObject)JsonNode.Parse(File.ReadAllText(file))!;
    }
}
