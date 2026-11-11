using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The contract checks: each use-case input and integration-event payload field holds a known type and its required flag, and each entry point states a known auth kind.</summary>
/// <follows>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</follows>
/// <follows>litespec/core/edges#describe-every-integration-event-for-its-direction</follows>
/// <follows>litespec/core/access#authenticate-every-caller-through-a-declared-auth-kind</follows>
internal static class ContractChecks
{
    /// <summary>The contract field types a use-case input or payload field may carry.</summary>
    private static readonly HashSet<string> ContractTypes = new(StringComparer.Ordinal)
    {
        "text", "integer", "decimal", "money", "boolean", "date", "date-time", "duration", "email", "choice", "id",
    };

    /// <summary>Reports a use-case input or integration-event payload field outside its typed shape.</summary>
    /// <param name="model">The loaded project model that holds every use case and integration event record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunTypedInputs(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            CheckContractFields(model, useCase, "input", "Use case", findings);
        }

        foreach (var integration in model.Records.Where(record => record.Type == RecordTypes.IntegrationEvent))
        {
            CheckContractFields(model, integration, "payload", "Integration event", findings);

            if (integration.Raw.ValueKind == JsonValueKind.Object
                && integration.Raw.TryGetProperty(RecordFields.Dataschema, out var schema)
                && schema.ValueKind == JsonValueKind.String
                && schema.GetString() is { Length: > 0 } url
                && !Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                findings.Add(new Finding("field-invalid", $"Integration event '{integration.Id}' holds dataschema '{url}', which is no URL.", integration.File, integration.Line, SubjectOf(integration)));
            }
        }
    }

    /// <summary>Reports one contract field holding no type, no required, or a dangling of.</summary>
    /// <param name="model">The loaded project model that resolves each id field target.</param>
    /// <param name="record">The use case or integration event record whose contract fields are judged.</param>
    /// <param name="array">The contract array field name, input or payload.</param>
    /// <param name="label">The record kind description that opens each finding.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckContractFields(ProjectModel model, Record record, string array, string label, List<Finding> findings)
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
                findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds {array} field '{name}' with no type.", record.File, record.Line, SubjectOf(record)));
                continue;
            }

            if (!ContractTypes.Contains(type))
            {
                findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds {array} field '{name}' with type '{type}' outside {string.Join(", ", ContractTypes.Order(StringComparer.Ordinal))}.", record.File, record.Line, SubjectOf(record)));
                continue;
            }

            if (!entry.TryGetProperty(RecordFields.Required, out var required)
                || required.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds {array} field '{name}' with no required.", record.File, record.Line, SubjectOf(record)));
            }

            CheckContractOf(model, record, label, array, name, type, entry, findings);
        }
    }

    /// <summary>Reports an of that names no record for an id, or no choices for a choice.</summary>
    /// <param name="model">The loaded project model that resolves the id field target.</param>
    /// <param name="record">The record that holds the contract field.</param>
    /// <param name="label">The record kind description that opens each finding.</param>
    /// <param name="array">The contract array field name, input or payload.</param>
    /// <param name="name">The contract field name quoted in each finding.</param>
    /// <param name="type">The contract field type that decides the of shape.</param>
    /// <param name="entry">The contract field object whose of value is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckContractOf(ProjectModel model, Record record, string label, string array, string name, string type, JsonElement entry, List<Finding> findings)
    {
        var has = entry.TryGetProperty(RecordFields.Of, out var of);

        if (type == "id")
        {
            if (!has || of.ValueKind != JsonValueKind.String || of.GetString() is not { Length: > 0 } target)
            {
                findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type id with no of record.", record.File, record.Line, SubjectOf(record)));
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
            if (!has || of.ValueKind != JsonValueKind.Array || of.GetArrayLength() == 0)
            {
                findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type choice with no of choices.", record.File, record.Line, SubjectOf(record)));
            }

            return;
        }

        if (has)
        {
            findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type {type} with an of that only id and choice carry.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>The door auth values an entry point may state.</summary>
    private static readonly HashSet<string> DoorAuth = new(StringComparer.Ordinal)
    {
        "session", "token", "device", "link", "app", "none",
    };

    /// <summary>Reports a door stating an auth outside its six values.</summary>
    /// <param name="model">The loaded project model that holds every entry point record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunDoors(ProjectModel model, List<Finding> findings)
    {
        foreach (var point in model.Records.Where(record => record.Type == RecordTypes.EntryPoint))
        {
            foreach (var auth in point.Ids(RecordFields.Auth).Order(StringComparer.Ordinal))
            {
                if (!DoorAuth.Contains(auth))
                {
                    findings.Add(new Finding("field-invalid", $"Entry point '{point.Id}' holds auth '{auth}' outside {string.Join(", ", DoorAuth)}.", point.File, point.Line, SubjectOf(point)));
                }
            }
        }
    }
}
