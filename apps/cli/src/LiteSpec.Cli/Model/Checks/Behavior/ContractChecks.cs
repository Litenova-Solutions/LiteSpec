using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;
using static LiteSpec.Cli.Model.Checks.Behavior.Contracts.ContractEventShapes;
using static LiteSpec.Cli.Model.Checks.Behavior.Contracts.ContractFieldShapes;
using static LiteSpec.Cli.Model.Checks.Behavior.Contracts.ContractPartResolution;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The contract checks: each use-case input and each integration-event payload field holds a known type and its required flag, a claim-shaped input is not taken from a caller of an authenticated entry point, a module event selects only fields its translated domain event carries, each entry point states a known auth kind, and each identifies naming an aggregate part resolves to a declared identified part.</summary>
/// <follows>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</follows>
/// <follows>litespec/core/use-cases#mark-every-provider-verified-input</follows>
/// <follows>litespec/core/use-cases#type-every-output-field</follows>
/// <follows>litespec/core/edges#describe-every-integration-event-the-product-sends-or-receives</follows>
/// <follows>litespec/core/access#authenticate-every-caller-through-a-declared-authentication-kind</follows>
internal static class ContractChecks
{
    /// <summary>The contract field types a use-case input or an integration-event payload field may carry.</summary>
    internal static readonly HashSet<string> ContractTypes = new(StringComparer.Ordinal)
    {
        "text", "integer", "decimal", "money", "boolean", "date", "date-time", "duration", "email", "choice", "id", "file", "list",
    };

    /// <summary>The input names that carry a claim a provider verifies, never a caller.</summary>
    private static readonly string[] ClaimNames = ["subject", "tenant", "verified"];

    /// <summary>Reports a use-case input or an integration-event payload field outside its typed shape, a claim-shaped input a caller supplies, and a module-event field or integration-event payload field its translated domain event does not carry.</summary>
    /// <param name="model">The loaded project model that holds every use case, module event and integration event record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunTypedInputs(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            CheckContractFields(model, useCase, "inputs", "Use case", findings);
            CheckClaimOrigins(model, useCase, findings);
        }

        foreach (var system in model.Records.Where(record => record.Type == RecordTypes.ExternalSystem))
        {
            CheckProcesses(model, system, findings);
        }

        foreach (var moduleEvent in model.Records.Where(record => record.Type == RecordTypes.ModuleEvent))
        {
            CheckFieldsCarried(model, moduleEvent, findings);
        }

        foreach (var integration in model.Records.Where(record => record.Type == RecordTypes.IntegrationEvent))
        {
            CheckContractFields(model, integration, "fields", "Integration event", findings);
            CheckPayloadCarried(model, integration, findings);

            if (integration.Raw.ValueKind == JsonValueKind.Object
                && integration.Raw.TryGetProperty(RecordFields.Dataschema, out var schema)
                && schema.ValueKind == JsonValueKind.String
                && schema.GetString() is { Length: > 0 } url
                && !Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                findings.Add(new Finding("record-invalid", $"Integration event '{integration.Id}' holds dataschema '{url}', which is no URL.", integration.File, integration.Line, SubjectOf(integration)));
            }
        }
    }

    /// <summary>Reports an identifies that names an aggregate part the aggregate does not declare, or declares without identity, and an aggregate that declares one part name twice.</summary>
    /// <param name="model">The loaded project model that resolves each identified aggregate part.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunIdentifiedParts(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records)
        {
            if (record.Raw.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            CheckDuplicateParts(record, findings);

            foreach (var (array, label, path, target) in IdentifiesFields(record))
            {
                if (!TrySplitPart(target, out var baseId, out var part))
                {
                    continue;
                }

                // A dangling base is reference-unknown through the contract and citation checks, so one defect draws one finding.
                if (!model.ById.TryGetValue(baseId, out var aggregate))
                {
                    continue;
                }

                if (aggregate.Type != RecordTypes.Aggregate)
                {
                    findings.Add(new Finding("reference-unknown", $"{label} '{record.Id}' holds {array} field '{path}' of '{target}', which names a part of '{baseId}', which is no aggregate.", record.File, record.Line, SubjectOf(record)));
                    continue;
                }

                if (!DeclaresPart(aggregate, part))
                {
                    findings.Add(new Finding("reference-unknown", $"{label} '{record.Id}' holds {array} field '{path}' of '{target}', which names part '{part}' the aggregate does not declare.", record.File, record.Line, SubjectOf(record)));
                    continue;
                }

                if (!IsIdentifiedPart(aggregate, part))
                {
                    findings.Add(new Finding("reference-unknown", $"{label} '{record.Id}' holds {array} field '{path}' of '{target}', which names part '{part}' the aggregate declares without identity.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }
    }

    /// <summary>Whether an identifies value resolves: a bare value to a held record, and a part value to an aggregate declaring the part with identity.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="identifies">The identifies value under test.</param>
    /// <returns>True when the value resolves to a record or an identified part; otherwise false.</returns>
    internal static bool IdentifiesResolves(ProjectModel model, string identifies)
    {
        if (!TrySplitPart(identifies, out var baseId, out var part))
        {
            return model.ById.ContainsKey(Identifiers.BaseId(identifies));
        }

        return model.ById.TryGetValue(baseId, out var aggregate)
            && aggregate.Type == RecordTypes.Aggregate
            && IsIdentifiedPart(aggregate, part);
    }

    /// <summary>Reports a claim-shaped input that a caller supplies on a use case an authenticated entry point reaches.</summary>
    /// <param name="model">The loaded project model that resolves each entry point the use case is reached through.</param>
    /// <param name="useCase">The use case record whose inputs are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckClaimOrigins(ProjectModel model, Record useCase, List<Finding> findings)
    {
        var authenticated = useCase.Ids(RecordFields.Via)
            .Where(id => model.ById.TryGetValue(Identifiers.BaseId(id), out var point)
                && point.Type == RecordTypes.EntryPoint
                && point.Field(RecordFields.Authentication) is { Length: > 0 } auth
                && auth != "none")
            .ToList();

        if (authenticated.Count == 0 || useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Inputs, out var inputs)
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

    /// <summary>
    ///     Reports a list-of-rows shape that is no object with a non-empty fields array, a row field with no name, a type outside
    ///     the contract types, or no required flag, a minItems that is no non-negative whole number, and a unique key that names no row field.
    /// </summary>
    /// <param name="record">The record whose list field is judged.</param>
    /// <param name="label">The record kind description that opens each finding.</param>
    /// <param name="array">The contract array field name, inputs, fields or outputs.</param>
    /// <param name="name">The contract field name quoted in each finding.</param>
    /// <param name="rowShape">The items object of the list field, or a default element when the field states none.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckRowShape(Record record, string label, string array, string name, JsonElement rowShape, List<Finding> findings)
    {
        if (rowShape.ValueKind != JsonValueKind.Object
            || !rowShape.TryGetProperty(RecordFields.Fields, out var rowFields)
            || rowFields.ValueKind != JsonValueKind.Array
            || rowFields.GetArrayLength() == 0)
        {
            findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type list with no items.", record.File, record.Line, SubjectOf(record)));
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
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds row field '{rowName}' of {array} field '{name}' with type '{rowType ?? "none"}' outside the contract types.", record.File, record.Line, SubjectOf(record)));
            }

            if (row.ValueKind != JsonValueKind.Object
                || !row.TryGetProperty(RecordFields.Required, out var required)
                || required.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds row field '{rowName}' of {array} field '{name}' with no required.", record.File, record.Line, SubjectOf(record)));
            }
        }

        if (rowShape.TryGetProperty(RecordFields.MinItems, out var minItems)
            && (minItems.ValueKind != JsonValueKind.Number || !minItems.TryGetInt32(out var count) || count < 0))
        {
            findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds minItems on {array} field '{name}' that is no non-negative whole number.", record.File, record.Line, SubjectOf(record)));
        }

        if (!rowShape.TryGetProperty(RecordFields.Unique, out var unique))
        {
            return;
        }

        if (unique.ValueKind != JsonValueKind.Array)
        {
            findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds unique on {array} field '{name}' as no list.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        foreach (var key in unique.EnumerateArray())
        {
            if (key.ValueKind != JsonValueKind.String || !rowNames.Contains(key.GetString()!))
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds unique key '{key.ToString()}' on {array} field '{name}', which names no row field.", record.File, record.Line, SubjectOf(record)));
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
            foreach (var auth in point.Ids(RecordFields.Authentication).Order(StringComparer.Ordinal))
            {
                if (!EntryPointAuth.Contains(auth))
                {
                    findings.Add(new Finding("record-invalid", $"Entry point '{point.Id}' holds auth '{auth}' outside {string.Join(", ", EntryPointAuth)}.", point.File, point.Line, SubjectOf(point)));
                }
            }
        }
    }
}
