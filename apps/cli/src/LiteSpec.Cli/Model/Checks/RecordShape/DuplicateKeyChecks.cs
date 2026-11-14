using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.RecordShape;

/// <summary>
///     The duplicate key checks: no two entries of one record share a key. A list of objects
///     cannot use uniqueItems, which compares whole objects, so the model check reads each key.
/// </summary>
/// <follows>litespec/core/model#keep-shape-rules-in-the-schema</follows>
internal static class DuplicateKeyChecks
{
    /// <summary>Reports record-key-duplicate for each key that two entries of one record share.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records)
        {
            if (record.Derived || record.Raw.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            switch (record.Type)
            {
                case RecordTypes.Constraint:
                case RecordTypes.Research:
                    CheckKeys(record, ArrayProperty(record.Raw, RecordFields.Sources), RecordFields.Key, "source key", null, findings);
                    break;

                case RecordTypes.Journey:
                    CheckKeys(record, ArrayProperty(record.Raw, RecordFields.Steps), RecordFields.Id, "step id", null, findings);
                    CheckBranchKeys(record, findings);
                    break;

                case RecordTypes.ExternalSystem:
                    CheckKeys(record, ArrayProperty(record.Raw, RecordFields.Operations), RecordFields.Name, "operation name", null, findings);
                    break;

                case RecordTypes.IntegrationEvent:
                    CheckKeys(record, ArrayProperty(record.Raw, RecordFields.Payload), RecordFields.Name, "field name", null, findings);
                    break;

                case RecordTypes.UseCase:
                    CheckKeys(record, ArrayProperty(record.Raw, RecordFields.Input), RecordFields.Name, "field name", null, findings);
                    CheckOutputKeys(record, findings);
                    break;
            }
        }
    }

    /// <summary>
    ///     Reports the field names of a use case output: a command output is one list of fields,
    ///     a query output names one list of fields for each group in its include map.
    /// </summary>
    /// <param name="record">The use case record whose output is read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckOutputKeys(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.Output, out var output))
        {
            return;
        }

        if (output.ValueKind == JsonValueKind.Array)
        {
            CheckKeys(record, output, RecordFields.Name, "field name", null, findings);
            return;
        }

        if (output.ValueKind == JsonValueKind.Object
            && output.TryGetProperty(RecordFields.Include, out var include)
            && include.ValueKind == JsonValueKind.Object)
        {
            foreach (var group in include.EnumerateObject())
            {
                CheckKeys(record, ArrayProperty(group.Value, RecordFields.Fields), RecordFields.Name, "field name", $"output group '{group.Name}'", findings);
            }
        }
    }

    /// <summary>The array held by one property, or null when the property is absent or holds no array.</summary>
    /// <param name="element">The JSON value expected to hold the property.</param>
    /// <param name="name">The property name whose array is wanted.</param>
    /// <returns>The array element, or null.</returns>
    private static JsonElement? ArrayProperty(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Array
            ? value
            : null;

    /// <summary>
    ///     Reports each path that two branches of one journey name twice. A branch is keyed by the
    ///     step it leaves and the step it continues at, so one step may hold several branches with
    ///     different targets, and two branches to the same target are one path named twice.
    /// </summary>
    /// <param name="record">The journey record whose branches are read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckBranchKeys(Record record, List<Finding> findings)
    {
        if (ArrayProperty(record.Raw, RecordFields.Branches) is not { } list)
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in list.EnumerateArray())
        {
            if (Str(entry, RecordFields.Step) is not { Length: > 0 } step
                || Str(entry, RecordFields.NextStep) is not { Length: > 0 } next)
            {
                continue;
            }

            var path = $"{step} to {next}";

            if (!seen.Add(path) && reported.Add(path))
            {
                findings.Add(new Finding(
                    "record-key-duplicate",
                    $"Record '{record.Id}' names the branch '{path}' twice.",
                    record.File,
                    record.Line,
                    SubjectOf(record)));
            }
        }
    }

    /// <summary>Reports each key that two or more entries of one list share, once per key.</summary>
    /// <param name="record">The record that holds the list.</param>
    /// <param name="entries">The list of entries, or null when the record holds no such list.</param>
    /// <param name="keyName">The property that holds each entry's key.</param>
    /// <param name="noun">The kind of key, for the message.</param>
    /// <param name="scope">The part of the record the list sits in, for the message, or null for the whole record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckKeys(Record record, JsonElement? entries, string keyName, string noun, string? scope, List<Finding> findings)
    {
        if (entries is not { } list)
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in list.EnumerateArray())
        {
            if (Str(entry, keyName) is not { Length: > 0 } key)
            {
                continue;
            }

            if (!seen.Add(key) && reported.Add(key))
            {
                var where = scope is null ? string.Empty : $" in {scope}";

                findings.Add(new Finding(
                    "record-key-duplicate",
                    $"Record '{record.Id}' names {noun} '{key}' twice{where}.",
                    record.File,
                    record.Line,
                    SubjectOf(record)));
            }
        }
    }
}
