using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Intent;

/// <summary>The sourced-statement checks: a constraint obligation or a research finding names source keys the record's sources hold.</summary>
/// <follows>litespec/core/intent#cite-a-source-for-each-constraint-rule</follows>
/// <follows>litespec/core/intent#state-each-research-finding-with-its-source</follows>
internal static class SourcedStatementChecks
{
    /// <summary>Runs the sourced-statement checks on every constraint and research record.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record => !record.IsEntry && record.Type is RecordTypes.Constraint or RecordTypes.Research))
        {
            var keys = SourceKeysOf(record);
            var field = record.Type == RecordTypes.Constraint ? RecordFields.Obligations : RecordFields.Findings;
            var code = record.Type == RecordTypes.Constraint ? "constraint-obligation-source-unknown" : "research-finding-source-unknown";
            var label = record.Type == RecordTypes.Constraint ? "Constraint" : "Research";

            if (record.Raw.ValueKind != JsonValueKind.Object
                || !record.Raw.TryGetProperty(field, out var statements)
                || statements.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var statement in statements.EnumerateArray())
            {
                if (statement.ValueKind != JsonValueKind.Object
                    || !statement.TryGetProperty(RecordFields.Sources, out var sources)
                    || sources.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var source in sources.EnumerateArray())
                {
                    if (source.ValueKind != JsonValueKind.String
                        || source.GetString() is not { Length: > 0 } key)
                    {
                        continue;
                    }

                    if (!keys.Contains(key, StringComparer.Ordinal))
                    {
                        findings.Add(new Finding(code, $"{label} '{record.Id}' names source key '{key}' in {field} that its sources do not hold.", record.File, record.Line, SubjectOf(record)));
                    }
                }
            }
        }
    }

    /// <summary>The source keys a constraint or research record declares.</summary>
    /// <param name="record">The record whose sources are read.</param>
    /// <returns>Each declared source key in written order.</returns>
    private static List<string> SourceKeysOf(Record record)
    {
        var keys = new List<string>();

        if (record.Raw.ValueKind == JsonValueKind.Object
            && record.Raw.TryGetProperty(RecordFields.Sources, out var sources)
            && sources.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in sources.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.Object
                    && entry.TryGetProperty(RecordFields.Key, out var key)
                    && key.ValueKind == JsonValueKind.String
                    && key.GetString() is { Length: > 0 } name)
                {
                    keys.Add(name);
                }
            }
        }

        return keys;
    }
}
