using System.Text.Json;
using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Operations;
using LiteSpec.Cli.Model.Tests;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Readers.Measures;

/// <summary>
///     Reads measured-check results the way the test-run reader reads test runs:
///     every result file under the test results folder, dispatched by shape to the
///     k6, Lighthouse, or ZAP reader, and every check result the drills and
///     harnesses write under the check results folder. A measured requirement
///     shows verified when its check names one of these results and that result passes.
/// </summary>
/// <follows>litespec/core/intent#state-the-measure-on-a-counted-requirement</follows>
static class MeasureReader
{
    /// <summary>Reads every measure result file under the test results and check results folders.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <param name="problems">The findings an invalid check result file is reported into.</param>
    /// <returns>Every measure result under the test results and check results folders.</returns>
    public static List<MeasureResult> Read(ConsumerConfig config, List<Finding> problems)
    {
        var measures = new List<MeasureResult>();

        foreach (var file in SourceTree.Files(config.TestResultsDir, ".json"))
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
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var root = document.RootElement;

                if (root.TryGetProperty("site", out var site) && site.ValueKind == JsonValueKind.Array)
                {
                    measures.AddRange(ZapResultReader.Parse(root, file));
                }
                else if (root.TryGetProperty("audits", out var audits) && audits.ValueKind == JsonValueKind.Object
                    && root.TryGetProperty("categories", out var categories) && categories.ValueKind == JsonValueKind.Object)
                {
                    measures.AddRange(LighthouseResultReader.Parse(root, file));
                }
                else if (root.TryGetProperty("metrics", out var metrics) && metrics.ValueKind == JsonValueKind.Object)
                {
                    measures.AddRange(K6ResultReader.Parse(root, file));
                }
            }
        }

        foreach (var result in CheckResultReader.Read(config, problems))
        {
            measures.Add(new MeasureResult("check", result.Check, result.Passed, result.Value, result.File));
        }

        measures.Sort((left, right) =>
        {
            var tool = string.Compare(left.Tool, right.Tool, StringComparison.Ordinal);

            if (tool != 0)
            {
                return tool;
            }

            var check = string.Compare(left.Check, right.Check, StringComparison.Ordinal);

            return check != 0
                ? check
                : string.Compare(left.Source, right.Source, StringComparison.Ordinal);
        });

        return measures;
    }
}
