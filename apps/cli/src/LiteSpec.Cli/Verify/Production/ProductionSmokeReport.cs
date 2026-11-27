using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.CommandLine;

namespace LiteSpec.Cli.Verify.Production;

/// <summary>The production smoke JSON report written under .litespec/verify.</summary>
/// <follows>litespec/core/release#boot-the-production-configuration-before-release</follows>
static class ProductionSmokeReport
{
    /// <summary>The report file name inside the verify folder.</summary>
    internal const string ReportFileName = "production-smoke.json";

    /// <summary>The JSON report object: the probed address, the compose inputs, and the findings.</summary>
    /// <param name="baseUrl">The production address the smoke probed.</param>
    /// <param name="production">The production settings the smoke read from the verify block.</param>
    /// <param name="findings">The findings the smoke raised, in run order.</param>
    /// <returns>The JSON report object.</returns>
    internal static JsonObject AsJson(string baseUrl, VerifyProduction production, IReadOnlyList<ProductionSmokeFinding> findings) => new()
    {
        ["baseUrl"] = baseUrl,
        ["compose"] = production.Compose,
        ["envFile"] = production.EnvFile,
        ["health"] = production.Health,
        ["paths"] = CliJson.Strings(production.Paths),
        ["findings"] = new JsonArray([.. findings.Select(finding => (JsonNode?)finding.AsJson())]),
    };
}
