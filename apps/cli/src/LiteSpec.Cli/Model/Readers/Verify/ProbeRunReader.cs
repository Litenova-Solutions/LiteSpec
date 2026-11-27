using System.Globalization;
using System.Text.Json;
using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Tests;
using LiteSpec.Cli.Verify.Probes;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Readers.Verify;

/// <summary>
///     Reads test outcomes from the verify report. Absent when the report holds no probe outcome.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
sealed class ProbeRunReader : ITestRunReader
{
    /// <summary>Reads the run, or null when no verify report exists or no probe in it ran.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <returns>The test run, or null when no result file exists or no probe in it ran.</returns>
    public TestRun? Read(ConsumerConfig config)
    {
        var file = Path.Combine(config.Root, ProbeReport.RelativePath);

        if (!File.Exists(file))
        {
            return null;
        }

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(File.ReadAllBytes(file));
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("probes", out var probes)
                || probes.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var run = new TestRun();
            run.Files.Add(PathText.Relative(config.Root, file));
            run.Date = DateOf(document.RootElement) ?? FileDate(file);

            var failed = SubjectsOf(document.RootElement, "probe-failed");
            var unreachable = SubjectsOf(document.RootElement, "probe-unreachable");

            foreach (var probe in probes.EnumerateArray())
            {
                if (probe.ValueKind != JsonValueKind.Object
                    || !probe.TryGetProperty("scenario", out var scenarioValue)
                    || scenarioValue.ValueKind != JsonValueKind.String
                    || scenarioValue.GetString() is not { Length: > 0 } scenario)
                {
                    continue;
                }

                if (OutcomeOf(probe, scenario, failed, unreachable) is not { } outcome)
                {
                    continue;
                }

                run.OutcomeByTest[scenario] = outcome;
                run.MergeOutcome(run.OutcomeByScenario, scenario, outcome);
            }

            if (run.OutcomeByTest.Count == 0)
            {
                return null;
            }

            run.Total = run.OutcomeByTest.Count;
            run.Passed = run.OutcomeByTest.Values.Count(outcome => outcome == "Passed");
            run.Failed = run.OutcomeByTest.Values.Count(outcome => outcome == "Failed");

            return run;
        }
    }

    /// <summary>
    ///     The outcome one probe answers to, or null when the probe never ran: the stated result when the
    ///     report carries one, else failed when a probe-failed finding names its scenario, passed when no
    ///     finding names it, and null when the probe could not reach the host.
    /// </summary>
    /// <param name="probe">The probe entry of the verify report.</param>
    /// <param name="scenario">The scenario identifier the probe proves.</param>
    /// <param name="failed">The scenarios a probe-failed finding names.</param>
    /// <param name="unreachable">The scenarios a probe-unreachable finding names.</param>
    /// <returns>The outcome as the run writes it, or null when the probe never ran.</returns>
    private static string? OutcomeOf(JsonElement probe, string scenario, HashSet<string> failed, HashSet<string> unreachable)
    {
        if (probe.TryGetProperty("result", out var stated)
            && stated.ValueKind == JsonValueKind.String
            && stated.GetString() is { Length: > 0 } text)
        {
            if (string.Equals(text, "unreachable", StringComparison.Ordinal))
            {
                return null;
            }

            if (string.Equals(text, "failed", StringComparison.Ordinal))
            {
                return "Failed";
            }

            if (string.Equals(text, "passed", StringComparison.Ordinal))
            {
                return "Passed";
            }
        }

        if (unreachable.Contains(scenario))
        {
            return null;
        }

        return failed.Contains(scenario) ? "Failed" : "Passed";
    }

    /// <summary>The scenarios the findings of one code name: every finding subject the code marks.</summary>
    /// <param name="root">The parsed report document.</param>
    /// <param name="code">The finding code to keep, such as probe-failed.</param>
    /// <returns>The scenario identifiers the code marks.</returns>
    private static HashSet<string> SubjectsOf(JsonElement root, string code)
    {
        var subjects = new HashSet<string>(StringComparer.Ordinal);

        if (!root.TryGetProperty("findings", out var findings) || findings.ValueKind != JsonValueKind.Array)
        {
            return subjects;
        }

        foreach (var finding in findings.EnumerateArray())
        {
            if (finding.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!finding.TryGetProperty("code", out var found)
                || found.ValueKind != JsonValueKind.String
                || found.GetString() != code)
            {
                continue;
            }

            if (finding.TryGetProperty("subject", out var subject)
                && subject.ValueKind == JsonValueKind.String
                && subject.GetString() is { Length: > 0 } scenario)
            {
                subjects.Add(scenario);
            }
        }

        return subjects;
    }

    /// <summary>The report run date, yyyy-MM-dd, or null when the report states none.</summary>
    /// <param name="root">The parsed report document.</param>
    /// <returns>The yyyy-MM-dd run date, or null when the report states no valid date.</returns>
    private static string? DateOf(JsonElement root)
    {
        if (!root.TryGetProperty("date", out var stated)
            || stated.ValueKind != JsonValueKind.String
            || stated.GetString() is not { } stamp
            || stamp.Length < 10)
        {
            return null;
        }

        var day = stamp[..10];

        return DateOnly.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            ? day
            : null;
    }

    /// <summary>The file last write day in UTC, as yyyy-MM-dd.</summary>
    /// <param name="file">The run file path whose last write day is wanted.</param>
    /// <returns>A yyyy-MM-dd text built from the file last write time in UTC.</returns>
    private static string FileDate(string file) =>
        File.GetLastWriteTimeUtc(file).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
