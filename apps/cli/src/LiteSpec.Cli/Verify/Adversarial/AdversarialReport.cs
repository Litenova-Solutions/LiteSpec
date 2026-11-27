using System.Text;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Verify.Probes;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Verify.Adversarial;

/// <summary>
///     The JSON report one adversarial run writes: the base address, the probes replayed and
///     every finding, under .litespec/verify/adversarial.json.
/// </summary>
/// <follows>litespec/core/backend-application#enforce-target-authorization-in-the-pipeline</follows>
/// <follows>litespec/core/concurrency#store-idempotency-state-atomically</follows>
/// <follows>litespec/core/backend-application#separate-input-validation-from-invariants</follows>
internal static class AdversarialReport
{
    /// <summary>The report file path under the consumer root.</summary>
    public const string RelativePath = ".litespec/verify/adversarial.json";

    /// <summary>Writes the adversarial report and returns its absolute path.</summary>
    /// <param name="config">The consumer configuration holding the root.</param>
    /// <param name="baseUrl">The API host address the replays ran against.</param>
    /// <param name="probes">The probes the run replayed, in run order.</param>
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
            ["command"] = "adversarial",
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

        var absolute = Path.Combine(config.Root, ".litespec", "verify", "adversarial.json");

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
