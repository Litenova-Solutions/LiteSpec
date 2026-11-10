using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Schemas;

namespace LiteSpec.Model.Readers.Operations;

/// <summary>
///     Reads the check results the drills and measuring harnesses write under
///     paths.checkResults, beside the measured-check results of the test run. A file
///     that is not JSON or does not match schemas/check-result.schema.yaml is a
///     proof-invalid finding, never a silent skip.
/// </summary>
/// <follows>litespec/ext/delivery#record-each-drill-run-as-a-check-result</follows>
static class CheckResultReader
{
    /// <summary>The check result schema file, in the pinned LiteSpec release.</summary>
    const string SchemaFile = "schemas/check-result.schema.yaml";

    /// <summary>Reads every check result under the check results folder.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <param name="problems">The findings a file that is not a valid check result is reported into.</param>
    /// <returns>Every valid check result under the check results folder.</returns>
    public static List<CheckResult> Read(ConsumerConfig config, List<Finding> problems)
    {
        var results = new List<CheckResult>();
        var schema = LiteSpecHome.PathTo(SchemaFile);

        foreach (var file in SourceTree.Files(config.CheckResultsDir, ".json"))
        {
            JsonNode? node;

            try
            {
                node = JsonNode.Parse(File.ReadAllText(file));
            }
            catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
            {
                problems.Add(Invalid(file, $"is not JSON ({exception.Message})"));
                continue;
            }

            var found = JsonSchema.Problems(schema, node, PathText.Relative(config.Root, file), DataFile.Read);

            if (found.Count > 0)
            {
                foreach (var problem in found)
                {
                    problems.Add(Invalid(file, problem));
                }

                continue;
            }

            if (Parse(node, file) is { } result)
            {
                results.Add(result);
            }
        }

        return results;
    }

    /// <summary>Reads one check result object, or null when it holds no check run.</summary>
    /// <param name="root">The parsed document.</param>
    /// <param name="file">The path of the file to read.</param>
    /// <returns>The check result the object states, or null when it holds no check run.</returns>
    public static CheckResult? Parse(JsonNode? root, string file)
    {
        if (root is not JsonObject check
            || JsonValues.PropString(check, "check") is not { Length: > 0 } name
            || check["passed"] is not JsonValue passedValue
            || !passedValue.TryGetValue<bool>(out var passed))
        {
            return null;
        }

        return new CheckResult(
            name,
            passed,
            JsonValues.PropString(check, "value"),
            JsonValues.PropString(check, "ranOn"),
            file);
    }

    /// <summary>One invalid check result file as a proof-invalid finding.</summary>
    /// <param name="file">The file that is not a valid check result.</param>
    /// <param name="detail">What is wrong with it.</param>
    /// <returns>The finding.</returns>
    static Finding Invalid(string file, string detail) =>
        new(
            "proof-invalid",
            $"check result {detail} [litespec/ext/delivery#record-each-drill-run-as-a-check-result]",
            file,
            1,
            null);
}
