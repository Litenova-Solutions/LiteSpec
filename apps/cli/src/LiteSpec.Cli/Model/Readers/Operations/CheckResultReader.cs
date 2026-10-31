using System.Text.Json;

namespace LiteSpec.Model.Readers.Operations;

/// <summary>
///     Reads the check results the drills and measuring harnesses write under
///     paths.checkResults, beside the measured-check results of the test run.
/// </summary>
/// <follows>litespec/quality/operations#record-each-drill-run-as-a-check-result</follows>
static class CheckResultReader
{
    /// <summary>Reads every check result under the check results folder.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <returns>Every check result under the check results folder.</returns>
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
    /// <param name="root">The root element of the parsed document.</param>
    /// <param name="file">The path of the file to read.</param>
    /// <returns>The check result the object states, or null when it holds no check run.</returns>
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
