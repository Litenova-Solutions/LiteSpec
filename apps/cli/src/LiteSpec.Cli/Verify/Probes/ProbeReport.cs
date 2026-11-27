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
            ["probes"] = new JsonArray([.. probes.Select(probe => (JsonNode?)new JsonObject
            {
                ["scenario"] = probe.ScenarioId,
                ["useCase"] = probe.OwnerId,
                ["as"] = probe.As,
                ["method"] = probe.Method,
                ["path"] = probe.Path,
                ["status"] = probe.Status,
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
}
