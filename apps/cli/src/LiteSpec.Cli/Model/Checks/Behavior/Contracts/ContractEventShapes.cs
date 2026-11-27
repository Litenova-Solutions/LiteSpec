using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior.Contracts;

/// <summary>The contract event shapes: every external-system processes entry names an aggregate with a data class it declares, and every module-event and integration-event field is carried by a domain event it translates.</summary>
/// <follows>litespec/core/edges#describe-every-integration-event-the-product-sends-or-receives</follows>
internal static class ContractEventShapes
{
    /// <summary>Reports an external system processes entry that names no aggregate, or a data class that aggregate does not declare.</summary>
    /// <param name="model">The loaded project model that resolves each aggregate.</param>
    /// <param name="system">The external system record whose processes list is read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckProcesses(ProjectModel model, Record system, List<Finding> findings)
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
                findings.Add(new Finding("record-invalid", $"External system '{system.Id}' processes data class '{dataClass ?? "none"}' of '{aggregate.Id}', which the aggregate does not declare.", system.File, system.Line, SubjectOf(system)));
            }
        }
    }

    /// <summary>The data class names an aggregate declares, in array or map form.</summary>
    /// <param name="aggregate">The aggregate record whose dataClasses field is read.</param>
    /// <returns>Each declared data class name.</returns>
    internal static HashSet<string> DataClassNamesOf(Record aggregate)
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

    /// <summary>Reports a module-event field that its translated domain event does not carry.</summary>
    /// <param name="model">The loaded project model that resolves the translated domain event.</param>
    /// <param name="published">The module event record whose fields are compared with the domain event it translates.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckFieldsCarried(ProjectModel model, Record published, List<Finding> findings)
    {
        if (published.Raw.ValueKind != JsonValueKind.Object
            || !published.Raw.TryGetProperty(RecordFields.Fields, out var fields)
            || fields.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var domainId in published.Ids(RecordFields.Translates))
        {
            if (!model.ById.TryGetValue(Identifiers.BaseId(domainId), out var domain)
                || domain.Type != RecordTypes.DomainEvent
                || domain.Raw.ValueKind != JsonValueKind.Object
                || !domain.Raw.TryGetProperty(RecordFields.Fields, out var carries)
                || carries.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var carried = carries.EnumerateArray()
                .Select(entry => Str(entry, RecordFields.Name))
                .Where(name => name is { Length: > 0 })
                .ToHashSet(StringComparer.Ordinal);

            foreach (var entry in fields.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.String
                    && entry.GetString() is { Length: > 0 } field
                    && !carried.Contains(field))
                {
                    findings.Add(new Finding("event-field-uncarried", $"Module event '{published.Id}' publishes field '{field}' that its translated domain event '{domain.Id}' does not hold.", published.File, published.Line, SubjectOf(published)));
                }
            }
        }
    }

    /// <summary>Reports an integration-event field that its translated domain event does not carry.</summary>
    /// <param name="model">The loaded project model that resolves each translated domain event.</param>
    /// <param name="published">The integration event record whose fields are compared with the domain events it translates.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckPayloadCarried(ProjectModel model, Record published, List<Finding> findings)
    {
        if (published.Raw.ValueKind != JsonValueKind.Object
            || !published.Raw.TryGetProperty(RecordFields.Fields, out var payload)
            || payload.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var domainId in published.Ids(RecordFields.Translates))
        {
            if (!model.ById.TryGetValue(Identifiers.BaseId(domainId), out var domain)
                || domain.Type != RecordTypes.DomainEvent
                || domain.Raw.ValueKind != JsonValueKind.Object
                || !domain.Raw.TryGetProperty(RecordFields.Fields, out var carries)
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
                    findings.Add(new Finding("event-field-uncarried", $"Integration event '{published.Id}' publishes field '{field}' that its translated domain event '{domain.Id}' does not hold.", published.File, published.Line, SubjectOf(published)));
                }
            }
        }
    }
}
