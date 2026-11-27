using System.Text;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.CommandLine;

namespace LiteSpec.Cli.Audit;

/// <summary>The route-suite check result: the file the rendered-page audit writes under the check results folder.</summary>
/// <follows>litespec/core/ui-verification#run-geometry-sensors-on-every-route</follows>
static class AuditCheckResult
{
    /// <summary>The check name the result file carries and the measured requirements name.</summary>
    internal const string CheckName = "route-suite";

    /// <summary>The result file name under the check results folder.</summary>
    internal const string FileName = "route-suite.json";

    /// <summary>Writes the route-suite check result for one audit run, so the model check reads it like any other check result.</summary>
    /// <param name="config">The consumer configuration holding the check results folder.</param>
    /// <param name="routes">The audited routes the audit visited.</param>
    /// <param name="findings">The merged findings the audit reported.</param>
    internal static void Write(ConsumerConfig config, IReadOnlyList<AuditRoute> routes, IReadOnlyList<AuditFinding> findings)
    {
        var passed = findings.Count == 0;
        var value = passed
            ? $"No findings across {Count(routes.Count, "route")}."
            : $"{Count(findings.Count, "finding")} across {Count(routes.Count, "route")}.";

        var json = new JsonObject
        {
            ["check"] = CheckName,
            ["passed"] = passed,
            ["value"] = value,
        };

        try
        {
            Directory.CreateDirectory(config.CheckResultsDir);
            File.WriteAllText(
                Path.Combine(config.CheckResultsDir, FileName),
                CliJson.Serialize(json),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw new InvalidOperationException($"Cannot write the route-suite check result: {exception.Message}", exception);
        }
    }

    /// <summary>The count with its noun, singular for one.</summary>
    /// <param name="count">The count.</param>
    /// <param name="noun">The singular noun.</param>
    /// <returns>The count followed by the noun, plural past one.</returns>
    static string Count(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";
}
