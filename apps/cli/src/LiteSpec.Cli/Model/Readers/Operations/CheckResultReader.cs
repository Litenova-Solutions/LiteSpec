using System.Text.Json;

namespace LiteSpec.Model.Readers.Operations;

/// <summary>One drill or harness run: the check it names and what it saw.</summary>
/// <param name="Check">The check name the requirement carries.</param>
/// <param name="Passed">Whether the run passed.</param>
/// <param name="Value">The measured value, when the run states one.</param>
/// <param name="RanOn">The run date, when the run states one.</param>
/// <param name="File">The absolute file path.</param>
sealed record CheckResult(string Check, bool Passed, string? Value, string? RanOn, string File);

/// <summary>
///     Reads the check results the drills and measuring harnesses write under
///     paths.checkResults, beside the measured-check results of the test run.
/// </summary>
/// <follows>litespec/quality/operations#record-each-drill-run-as-a-check-result</follows>
static class CheckResultReader
{
    /// <summary>Reads every check result under the check results folder.</summary>
    public static List<CheckResult> Read(ConsumerConfig config)
    {
        var results = new List<CheckResult>();

        foreach (var file in SourceTree.Files(config.CheckResultsDir, ".json"))
        {
            JsonDocument document;

            try
            {
                document = JsonDocument.Parse(File.ReadAllBytes(file));
            }
            catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
            {
                continue;
            }

            using (document)
            {
                if (Parse(document.RootElement, file) is { } result)
                {
                    results.Add(result);
                }
            }
        }

        return results;
    }

    /// <summary>Reads one check result object, or null when it holds no check run.</summary>
    public static CheckResult? Parse(JsonElement root, string file)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("check", out var check)
            || check.ValueKind != JsonValueKind.String
            || check.GetString() is not { Length: > 0 } name
            || !root.TryGetProperty("passed", out var passed)
            || passed.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return null;
        }

        return new CheckResult(
            name,
            passed.ValueKind == JsonValueKind.True,
            root.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null,
            root.TryGetProperty("ranOn", out var ranOn) && ranOn.ValueKind == JsonValueKind.String ? ranOn.GetString() : null,
            file);
    }
}
