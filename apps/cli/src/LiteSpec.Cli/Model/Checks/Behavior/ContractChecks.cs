using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The contract checks: each use-case input and integration-event payload field holds a known type and its required flag, a claim-shaped input is not taken from a caller of an authenticated entry point, an integration event payload field is carried by its translated domain event, and each entry point states a known auth kind.</summary>
/// <follows>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</follows>
/// <follows>litespec/core/use-cases#mark-every-provider-verified-input</follows>
/// <follows>litespec/core/use-cases#type-every-output-field</follows>
/// <follows>litespec/core/edges#describe-every-integration-event-for-its-direction</follows>
/// <follows>litespec/core/access#authenticate-every-caller-through-a-declared-auth-kind</follows>
internal static class ContractChecks
{
    /// <summary>The contract field types a use-case input or payload field may carry.</summary>
    private static readonly HashSet<string> ContractTypes = new(StringComparer.Ordinal)
    {
        "text", "integer", "decimal", "money", "boolean", "date", "date-time", "duration", "email", "choice", "id", "file", "list",
    };

    /// <summary>The input names that carry a claim a provider verifies, never a caller.</summary>
    private static readonly string[] ClaimNames = ["subject", "tenant", "verified"];

    /// <summary>Reports a use-case input or integration-event payload field outside its typed shape, a claim-shaped input a caller supplies, and a payload field its translated domain event does not carry.</summary>
    /// <param name="model">The loaded project model that holds every use case and integration event record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunTypedInputs(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            CheckContractFields(model, useCase, "input", "Use case", findings);
            CheckClaimOrigins(model, useCase, findings);
        }

        foreach (var system in model.Records.Where(record => record.Type == RecordTypes.ExternalSystem))
        {
            CheckProcesses(model, system, findings);
        }

        foreach (var integration in model.Records.Where(record => record.Type == RecordTypes.IntegrationEvent))
        {
            CheckContractFields(model, integration, "payload", "Integration event", findings);
            CheckPayloadCarried(model, integration, findings);

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

    /// <summary>Reports an external system processes entry that names no aggregate, or a data class that aggregate does not declare.</summary>
    /// <param name="model">The loaded project model that resolves each aggregate.</param>
    /// <param name="system">The external system record whose processes list is read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckProcesses(ProjectModel model, Record system, List<Finding> findings)
    {
        if (system.Raw.ValueKind != JsonValueKind.Object
            || !system.Raw.TryGetProperty(RecordFields.Processes, out var list)
            || list.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in list.EnumerateArray())
        {
            var aggregateId = Str(entry, RecordFields.Aggregate);
            var dataClass = Str(entry, RecordFields.DataClass);

            if (aggregateId is not { Length: > 0 }
                || !model.ById.TryGetValue(aggregateId, out var aggregate)
                || aggregate.Type != RecordTypes.Aggregate)
            {
                findings.Add(new Finding("reference-unknown", $"External system '{system.Id}' processes '{aggregateId ?? "none"}', which is no aggregate.", system.File, system.Line, SubjectOf(system)));
                continue;
            }

            if (dataClass is not { Length: > 0 } || !DataClassNamesOf(aggregate).Contains(dataClass))
            {
                findings.Add(new Finding("field-invalid", $"External system '{system.Id}' processes data class '{dataClass ?? "none"}' of '{aggregate.Id}', which the aggregate does not declare.", system.File, system.Line, SubjectOf(system)));
            }
        }
    }

    /// <summary>The data class names an aggregate declares, in array or map form.</summary>
    /// <param name="aggregate">The aggregate record whose dataClasses field is read.</param>
    /// <returns>Each declared data class name.</returns>
    private static HashSet<string> DataClassNamesOf(Record aggregate)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        if (aggregate.Raw.ValueKind != JsonValueKind.Object || !aggregate.Raw.TryGetProperty(RecordFields.DataClasses, out var classes))
        {
            return names;
        }

        if (classes.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in classes.EnumerateArray())
            {
                if (Str(entry, RecordFields.Name) is { Length: > 0 } name)
                {
                    names.Add(name);
                }
            }
        }
        else if (classes.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in classes.EnumerateObject())
            {
                names.Add(property.Name);
            }
        }

        return names;
    }

    /// <summary>Reports a claim-shaped input that a caller supplies on a use case an authenticated entry point reaches.</summary>
    /// <param name="model">The loaded project model that resolves each entry point the use case is reached through.</param>
    /// <param name="useCase">The use case record whose inputs are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckClaimOrigins(ProjectModel model, Record useCase, List<Finding> findings)
    {
        var authenticated = useCase.Ids(RecordFields.ReachedThrough)
            .Where(id => model.ById.TryGetValue(Identifiers.BaseId(id), out var point)
                && point.Type == RecordTypes.EntryPoint
                && point.Field(RecordFields.Auth) is { Length: > 0 } auth
                && auth != "none")
            .ToList();

        if (authenticated.Count == 0 || useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Input, out var inputs)
            || inputs.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var input in inputs.EnumerateArray())
        {
            var name = Str(input, RecordFields.Name) ?? "";

            if (Str(input, RecordFields.Origin) == "caller"
                && ClaimNames.Any(claim => name.Contains(claim, StringComparison.OrdinalIgnoreCase)))
            {
                findings.Add(new Finding("input-claim-unverified", $"Use case '{useCase.Id}' takes claim '{name}' from the caller, reached through {string.Join(", ", authenticated.Order(StringComparer.Ordinal))}; a provider verifies it, so mark it verified-claim.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Reports an integration event payload field that its translated domain event does not carry.</summary>
    /// <param name="model">The loaded project model that resolves each translated domain event.</param>
    /// <param name="integration">The integration event record whose payload is compared with the domain events it translates.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPayloadCarried(ProjectModel model, Record integration, List<Finding> findings)
    {
        if (integration.Raw.ValueKind != JsonValueKind.Object
            || !integration.Raw.TryGetProperty(RecordFields.Payload, out var payload)
            || payload.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var domainId in integration.Ids(RecordFields.Translates))
        {
            if (!model.ById.TryGetValue(Identifiers.BaseId(domainId), out var domain)
                || domain.Type != RecordTypes.DomainEvent
                || domain.Raw.ValueKind != JsonValueKind.Object
                || !domain.Raw.TryGetProperty(RecordFields.Carries, out var carries)
                || carries.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var carried = carries.EnumerateArray()
                .Select(entry => Str(entry, RecordFields.Name))
                .Where(name => name is { Length: > 0 })
                .ToHashSet(StringComparer.Ordinal);

            foreach (var entry in payload.EnumerateArray())
            {
                if (Str(entry, RecordFields.Name) is { Length: > 0 } field && !carried.Contains(field))
                {
                    findings.Add(new Finding("integration-event-field-uncarried", $"Integration event '{integration.Id}' publishes field '{field}' that its translated domain event '{domain.Id}' does not carry.", integration.File, integration.Line, SubjectOf(integration)));
                }
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

    /// <summary>Reports an of that names no record for an id, a choice with no choices, a list with no row shape, or a file with no content types and size.</summary>
    /// <param name="model">The loaded project model that resolves the id field target.</param>
    /// <param name="record">The record that holds the contract field.</param>
    /// <param name="label">The record kind description that opens each finding.</param>
    /// <param name="array">The contract array field name, input or payload.</param>
    /// <param name="name">The contract field name quoted in each finding.</param>
    /// <param name="type">The contract field type that decides the shape its other fields take.</param>
    /// <param name="entry">The contract field object whose shape value is judged.</param>
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
            if (!entry.TryGetProperty(RecordFields.Choices, out var choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
            {
                findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type choice with no choices.", record.File, record.Line, SubjectOf(record)));
            }

            return;
        }

        if (type == "list")
        {
            CheckRowShape(record, label, array, name, has ? of : default, findings);
            return;
        }

        if (type == "file")
        {
            if (Str(entry, RecordFields.MaxSize) is not { Length: > 0 }
                || !entry.TryGetProperty(RecordFields.ContentTypes, out var contentTypes)
                || contentTypes.ValueKind != JsonValueKind.Array
                || contentTypes.GetArrayLength() == 0)
            {
                findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type file with no contentTypes and maxSize.", record.File, record.Line, SubjectOf(record)));
            }

            return;
        }

        if (has)
        {
            findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type {type} with an of that only id and list carry.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>
    ///     Reports a list-of-rows shape that is no object with a non-empty fields array, a row field with no name, a type outside
    ///     the contract types, or no required flag, a minItems that is no non-negative whole number, and a unique key that names no row field.
    /// </summary>
    /// <param name="record">The record whose list field is judged.</param>
    /// <param name="label">The record kind description that opens each finding.</param>
    /// <param name="array">The contract array field name, input, payload or output.</param>
    /// <param name="name">The contract field name quoted in each finding.</param>
    /// <param name="of">The of object of the list field, or a default element when the field states none.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckRowShape(Record record, string label, string array, string name, JsonElement of, List<Finding> findings)
    {
        if (of.ValueKind != JsonValueKind.Object
            || !of.TryGetProperty(RecordFields.Fields, out var rowFields)
            || rowFields.ValueKind != JsonValueKind.Array
            || rowFields.GetArrayLength() == 0)
        {
            findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type list with no of row fields.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        var rowNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in rowFields.EnumerateArray())
        {
            var rowName = Str(row, RecordFields.Name);
            var rowType = Str(row, RecordFields.Type);

            if (rowName is not { Length: > 0 })
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds a row field of {array} field '{name}' with no name.", record.File, record.Line, SubjectOf(record)));
                continue;
            }

            rowNames.Add(rowName);

            if (rowType is not { Length: > 0 } || !ContractTypes.Contains(rowType))
            {
                findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds row field '{rowName}' of {array} field '{name}' with type '{rowType ?? "none"}' outside the contract types.", record.File, record.Line, SubjectOf(record)));
            }

            if (row.ValueKind != JsonValueKind.Object
                || !row.TryGetProperty(RecordFields.Required, out var required)
                || required.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds row field '{rowName}' of {array} field '{name}' with no required.", record.File, record.Line, SubjectOf(record)));
            }
        }

        if (of.TryGetProperty(RecordFields.MinItems, out var minItems)
            && (minItems.ValueKind != JsonValueKind.Number || !minItems.TryGetInt32(out var count) || count < 0))
        {
            findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds minItems on {array} field '{name}' that is no non-negative whole number.", record.File, record.Line, SubjectOf(record)));
        }

        if (!of.TryGetProperty(RecordFields.Unique, out var unique))
        {
            return;
        }

        if (unique.ValueKind != JsonValueKind.Array)
        {
            findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds unique on {array} field '{name}' as no list.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        foreach (var key in unique.EnumerateArray())
        {
            if (key.ValueKind != JsonValueKind.String || !rowNames.Contains(key.GetString()!))
            {
                findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds unique key '{key.ToString()}' on {array} field '{name}', which names no row field.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>The entry point auth values an entry point may state.</summary>
    private static readonly HashSet<string> EntryPointAuth = new(StringComparer.Ordinal)
    {
        "session", "token", "device", "link", "app", "none",
    };

    /// <summary>Reports an entry point stating an auth outside its six values.</summary>
    /// <param name="model">The loaded project model that holds every entry point record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunDoors(ProjectModel model, List<Finding> findings)
    {
        foreach (var point in model.Records.Where(record => record.Type == RecordTypes.EntryPoint))
        {
            foreach (var auth in point.Ids(RecordFields.Auth).Order(StringComparer.Ordinal))
            {
                if (!EntryPointAuth.Contains(auth))
                {
                    findings.Add(new Finding("field-invalid", $"Entry point '{point.Id}' holds auth '{auth}' outside {string.Join(", ", EntryPointAuth)}.", point.File, point.Line, SubjectOf(point)));
                }
            }
        }
    }
}
