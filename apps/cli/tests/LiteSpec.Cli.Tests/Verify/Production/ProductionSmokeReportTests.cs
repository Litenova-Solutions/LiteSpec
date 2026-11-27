using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Verify.Production;

namespace LiteSpec.Cli.Tests.Verify.Production;

/// <summary>Proves the production smoke report holds the probed address and the findings.</summary>
public sealed class ProductionSmokeReportTests
{
    /// <summary>The report names the probed address, the compose inputs, and each finding.</summary>
    [Fact]
    public void ReportHoldsTheProbedAddressAndFindings()
    {
        var production = new VerifyProduction("deploy/compose.yaml", ".env.example", "http://localhost:8080", null, "/health/ready", ["/api"]);
        List<ProductionSmokeFinding> findings = [new("path-failed", "/api", "The production host answered /api with 500.")];

        var report = ProductionSmokeReport.AsJson("http://localhost:8080", production, findings);

        Assert.Equal("http://localhost:8080", report["baseUrl"]!.GetValue<string>());
        Assert.Equal("/health/ready", report["health"]!.GetValue<string>());
        Assert.Equal(["/api"], report["paths"]!.AsArray().Select(path => path!.GetValue<string>()));
        var stored = report["findings"]!.AsArray();
        Assert.Single(stored);
        Assert.Equal("path-failed", stored[0]!["code"]!.GetValue<string>());
        Assert.Equal("/api", stored[0]!["subject"]!.GetValue<string>());
    }

    /// <summary>A smoke with no findings writes an empty findings array.</summary>
    [Fact]
    public void ReportWithNoFindingsHoldsAnEmptyArray()
    {
        var production = new VerifyProduction("deploy/compose.yaml", ".env.example", "http://localhost:8080", null, "/health/ready", []);

        var report = ProductionSmokeReport.AsJson("http://localhost:8080", production, []);

        Assert.Empty(report["findings"]!.AsArray());
    }

    /// <summary>A finding line states the project file, the level, the code, and the message.</summary>
    [Fact]
    public void FindingLineStatesTheCodeAndMessage()
    {
        var finding = new ProductionSmokeFinding("stub-adapter-shown", "mail", "The production readiness lists stub adapter 'mail', so a stand-in serves production traffic.");

        var line = finding.Line(".litespec/project.yaml");

        Assert.StartsWith(".litespec/project.yaml:1 [S] [stub-adapter-shown]", line, StringComparison.Ordinal);
        Assert.Contains("mail", line, StringComparison.Ordinal);
    }
}
