using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior.Contracts;

/// <summary>The contract field shapes: every use-case input and integration-event payload field holds a known type with its required flag, an id field identifies a record it names, a choice field lists its choices, a list field holds its row shape, and a file field holds its content types and size.</summary>
/// <follows>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</follows>
/// <follows>litespec/core/use-cases#type-every-output-field</follows>
/// <follows>litespec/core/edges#describe-every-integration-event-the-product-sends-or-receives</follows>
internal static class ContractFieldShapes
{
    /// <summary>Reports one contract field holding no type, no required, or a dangling of.</summary>
    /// <param name="model">The loaded project model that resolves each id field target.</param>
    /// <param name="record">The use case or integration event record whose contract fields are judged.</param>
    /// <param name="array">The contract array field name, inputs or fields.</param>
    /// <param name="label">The record kind description that opens each finding.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckContractFields(ProjectModel model, Record record, string array, string label, List<Finding> findings)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(array, out var fields)
            || fields.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in fields.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = Str(entry, RecordFields.Name) ?? "(no name)";
            var type = Str(entry, RecordFields.Type);

            if (type is not { Length: > 0 })
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' with no type.", record.File, record.Line, SubjectOf(record)));
                continue;
            }

            if (!ContractChecks.ContractTypes.Contains(type))
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' with type '{type}' outside {string.Join(", ", ContractChecks.ContractTypes.Order(StringComparer.Ordinal))}.", record.File, record.Line, SubjectOf(record)));
                continue;
            }

            if (!entry.TryGetProperty(RecordFields.Required, out var required)
                || required.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' with no required.", record.File, record.Line, SubjectOf(record)));
            }

            CheckContractOf(model, record, label, array, name, type, entry, findings);
        }
    }

    /// <summary>Reports an identifies that names no record for an id, a choice with no choices, a list with no row shape, a file with no content types and size, and an identifies or rowShape on a type that carries neither.</summary>
    /// <param name="model">The loaded project model that resolves the id field target.</param>
    /// <param name="record">The record that holds the contract field.</param>
    /// <param name="label">The record kind description that opens each finding.</param>
    /// <param name="array">The contract array field name, input or payload.</param>
    /// <param name="name">The contract field name quoted in each finding.</param>
    /// <param name="type">The contract field type that decides the shape its other fields take.</param>
    /// <param name="entry">The contract field object whose shape value is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckContractOf(ProjectModel model, Record record, string label, string array, string name, string type, JsonElement entry, List<Finding> findings)
    {
        var hasId = entry.TryGetProperty(RecordFields.Identifies, out var of);
        var hasRow = entry.TryGetProperty(RecordFields.Items, out var rowShape);

        if (type == "id")
        {
            if (hasRow)
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type id with items that only list carries.", record.File, record.Line, SubjectOf(record)));
            }

            if (!hasId || of.ValueKind != JsonValueKind.String || of.GetString() is not { Length: > 0 } target)
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type id with no identifies.", record.File, record.Line, SubjectOf(record)));
                return;
            }

            if (!model.ById.ContainsKey(Identifiers.BaseId(target)))
            {
                findings.Add(new Finding("reference-unknown", $"{label} '{record.Id}' holds {array} field '{name}' of '{target}', which no record holds.", record.File, record.Line, SubjectOf(record)));
            }

            return;
        }

        if (type == "choice")
        {
            if (!entry.TryGetProperty(RecordFields.Choices, out var choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type choice with no choices.", record.File, record.Line, SubjectOf(record)));
            }

            RefuseShapes(record, label, array, name, type, hasId, hasRow, findings);
            return;
        }

        if (type == "list")
        {
            if (hasId)
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type list with an identifies that only id carries.", record.File, record.Line, SubjectOf(record)));
            }

            ContractChecks.CheckRowShape(record, label, array, name, hasRow ? rowShape : default, findings);
            return;
        }

        if (type == "file")
        {
            if (Str(entry, RecordFields.MaxSize) is not { Length: > 0 }
                || !entry.TryGetProperty(RecordFields.ContentTypes, out var contentTypes)
                || contentTypes.ValueKind != JsonValueKind.Array
                || contentTypes.GetArrayLength() == 0)
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type file with no contentTypes and maxSize.", record.File, record.Line, SubjectOf(record)));
            }

            RefuseShapes(record, label, array, name, type, hasId, hasRow, findings);
            return;
        }

        RefuseShapes(record, label, array, name, type, hasId, hasRow, findings);
    }

    /// <summary>Reports an identifies or items on a contract field type that carries neither.</summary>
    /// <param name="record">The record that holds the contract field.</param>
    /// <param name="label">The record kind description that opens each finding.</param>
    /// <param name="array">The contract array field name, inputs or fields.</param>
    /// <param name="name">The contract field name quoted in each finding.</param>
    /// <param name="type">The contract field type quoted in each finding.</param>
    /// <param name="hasId">Whether the field carries identifies.</param>
    /// <param name="hasRow">Whether the field carries items.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void RefuseShapes(Record record, string label, string array, string name, string type, bool hasId, bool hasRow, List<Finding> findings)
    {
        if (hasId)
        {
            findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type {type} with an identifies that only id carries.", record.File, record.Line, SubjectOf(record)));
        }

        if (hasRow)
        {
            findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type {type} with items that only list carries.", record.File, record.Line, SubjectOf(record)));
        }
    }
}
