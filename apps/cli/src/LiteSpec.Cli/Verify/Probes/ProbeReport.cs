using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Verify.Probes;

/// <summary>
///     The JSON report one verify run writes: the base address, the probes run and
///     every finding, under .litespec/verify/report.json.
/// </summary>
/// <follows>litespec/quality/testing#prove-each-scenario-through-the-public-boundary</follows>
internal static class ProbeReport
{
    /// <summary>The report file path under the consumer root.</summary>
    public const string RelativePath = ".litespec/verify/report.json";

    /// <summary>Writes the verify report and returns its absolute path.</summary>
    /// <param name="config">The consumer configuration holding the root.</param>
    /// <param name="baseUrl">The API host address the probes ran against.</param>
    /// <param name="probes">The probes the run attempted, in run order.</param>
    /// <param name="findings">The findings the run raised, in probe order.</param>
    /// <returns>The absolute report path.</returns>
    public static string Write(
        ConsumerConfig config,
        string baseUrl,
        IReadOnlyList<ProbeCase> probes,
        IReadOnlyList<Finding> findings)
    {
        var report = new JsonObject
        {
            ["baseUrl"] = baseUrl,
            ["date"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["probes"] = new JsonArray([.. probes.Select(probe => (JsonNode?)new JsonObject
            {
                ["scenario"] = probe.ScenarioId,
                ["useCase"] = probe.OwnerId,
                ["as"] = probe.As,
                ["method"] = probe.Method,
                ["path"] = probe.Path,
                ["status"] = probe.Status,
                ["result"] = ResultOf(probe.ScenarioId, findings),
            })]),
            ["findings"] = new JsonArray([.. findings.Select(finding => (JsonNode?)new JsonObject
            {
                ["code"] = finding.Code,
                ["message"] = finding.Message,
                ["path"] = PathText.Relative(config.Root, finding.Path),
                ["line"] = finding.Line,
                ["subject"] = finding.Subject,
            })]),
        };

        var absolute = Path.Combine(config.Root, ".litespec", "verify", "report.json");

        try
        {
            if (Path.GetDirectoryName(absolute) is { Length: > 0 } folder)
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(absolute, CliJson.Serialize(report), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            CliHost.Error.WriteLine($"Cannot write '{PathText.Relative(config.Root, absolute)}': {exception.Message}");
        }

        return absolute;
    }

    /// <summary>The run result one probe carries in the report: unreachable when the host could not be reached, failed on any finding, else passed.</summary>
    /// <param name="scenario">The scenario identifier the probe proves.</param>
    /// <param name="findings">The findings the run raised, in probe order.</param>
    /// <returns>The probe result the model reader proves the scenario with.</returns>
    private static string ResultOf(string scenario, IReadOnlyList<Finding> findings)
    {
        var seen = false;

        foreach (var finding in findings)
        {
            if (!string.Equals(finding.Subject, scenario, StringComparison.Ordinal))
            {
                continue;
            }

            seen = true;

            if (string.Equals(finding.Code, "probe-unreachable", StringComparison.Ordinal))
            {
                return "unreachable";
            }
        }

        return seen ? "failed" : "passed";
    }
}
