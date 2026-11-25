using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The contract checks: each use-case input and each integration-event payload field holds a known type and its required flag, every contract field states the exact type facts and constraints its type needs, a claim-shaped input is not taken from a caller of an authenticated entry point, a module event selects only fields its translated domain event carries, each entry point states a known auth kind, and each identifies naming an aggregate part resolves to a declared identified part.</summary>
/// <follows>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</follows>
/// <follows>litespec/core/use-cases#mark-every-provider-verified-input</follows>
/// <follows>litespec/core/use-cases#type-every-output-field</follows>
/// <follows>litespec/core/edges#describe-every-integration-event-the-product-sends-or-receives</follows>
/// <follows>litespec/core/access#authenticate-every-caller-through-a-declared-authentication-kind</follows>
internal static class ContractChecks
{
    /// <summary>The contract field types a use-case input or an integration-event payload field may carry.</summary>
    private static readonly HashSet<string> ContractTypes = new(StringComparer.Ordinal)
    {
        "text", "integer", "decimal", "money", "boolean", "date", "instant", "duration", "email", "choice", "id", "file", "list",
    };

    /// <summary>The input names that carry a claim a provider verifies, never a caller.</summary>
    private static readonly string[] ClaimNames = ["subject", "tenant", "verified"];

    /// <summary>Reports a use-case input or an integration-event payload field outside its typed shape, a contract field whose exact type facts name nothing, a claim-shaped input a caller supplies, a module-event field or integration-event payload field its translated domain event does not carry, and a carried field whose type or rowType names an aggregate part no aggregate declares.</summary>
    /// <param name="model">The loaded project model that holds every use case, module event and integration event record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunTypedInputs(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            CheckContractFields(model, useCase, "input", "Use case", findings);
            CheckClaimOrigins(model, useCase, findings);
            CheckOutputFields(model, useCase, findings);
        }

        foreach (var readModel in model.Records.Where(record => record.Type == RecordTypes.ReadModel))
        {
            CheckFieldArray(model, readModel, RecordFields.Fields, "Read model", "read-model", findings);
        }

        foreach (var domainEvent in model.Records.Where(record => record.Type == RecordTypes.DomainEvent))
        {
            CheckFieldArray(model, domainEvent, RecordFields.Carries, "Domain event", "carries", findings);
            CheckCarryTypeRefs(model, domainEvent, findings);
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
            CheckContractFields(model, integration, "payload", "Integration event", findings);
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

    /// <summary>Splits an identifies value naming an aggregate part into its aggregate and its part.</summary>
    /// <param name="identifies">The identifies value to split.</param>
    /// <param name="aggregateId">The identifier before the number sign.</param>
    /// <param name="part">The part name after the number sign.</param>
    /// <returns>True when the value names a part after a number sign; otherwise false.</returns>
    internal static bool TrySplitPart(string identifies, out string aggregateId, out string part)
    {
        aggregateId = "";
        part = "";

        var hash = identifies.IndexOf('#');

        if (hash <= 0 || hash == identifies.Length - 1)
        {
            return false;
        }

        aggregateId = identifies[..hash];
        part = identifies[(hash + 1)..];

        return true;
    }

    /// <summary>Whether the aggregate declares the part in its parts, identified or not.</summary>
    /// <param name="aggregate">The aggregate record whose parts are read.</param>
    /// <param name="part">The part name to find.</param>
    /// <returns>True when a parts entry carries the name; otherwise false.</returns>
    internal static bool DeclaresPart(Record aggregate, string part) =>
        PartsOf(aggregate).Any(entry => entry.Name.Equals(part, StringComparison.Ordinal));

    /// <summary>Whether the aggregate declares the part with its own identity.</summary>
    /// <param name="aggregate">The aggregate record whose parts are read.</param>
    /// <param name="part">The part name to find.</param>
    /// <returns>True when a parts entry carries the name with identity true; otherwise false.</returns>
    internal static bool IsIdentifiedPart(Record aggregate, string part) =>
        PartsOf(aggregate).Any(entry => entry.Name.Equals(part, StringComparison.Ordinal) && entry.Identity);

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

    /// <summary>The parts an aggregate declares: each parts entry name with whether it carries its own identity.</summary>
    /// <param name="aggregate">The aggregate record whose parts are read.</param>
    /// <returns>Each well-formed parts entry once, in written order.</returns>
    private static List<(string Name, bool Identity)> PartsOf(Record aggregate)
    {
        var parts = new List<(string Name, bool Identity)>();

        if (aggregate.Raw.ValueKind != JsonValueKind.Object
            || !aggregate.Raw.TryGetProperty(RecordFields.Parts, out var entries)
            || entries.ValueKind != JsonValueKind.Array)
        {
            return parts;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object && Str(entry, RecordFields.Name) is { Length: > 0 } name)
            {
                var identity = entry.TryGetProperty(RecordFields.Identity, out var flag) && flag.ValueKind == JsonValueKind.True;
                parts.Add((name, identity));
            }
        }

        return parts;
    }

    /// <summary>Reports an aggregate that declares one part name twice.</summary>
    /// <param name="record">The record whose parts are judged, when it is an aggregate.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckDuplicateParts(Record record, List<Finding> findings)
    {
        if (record.Type != RecordTypes.Aggregate)
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (name, _) in PartsOf(record))
        {
            if (!seen.Add(name) && reported.Add(name))
            {
                findings.Add(new Finding("record-invalid", $"Aggregate '{record.Id}' declares part '{name}' more than once.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>The identifies values a record holds: each contract field and list row of its input, payload, carries, fields and output, with its field path.</summary>
    /// <param name="record">The record whose contract fields are read.</param>
    /// <returns>Each identifies value with its array, its record label and its dotted field path.</returns>
    private static IEnumerable<(string Array, string Label, string Path, string Target)> IdentifiesFields(Record record)
    {
        var label = record.Type switch
        {
            RecordTypes.UseCase => "Use case",
            RecordTypes.IntegrationEvent => "Integration event",
            RecordTypes.DomainEvent => "Domain event",
            RecordTypes.ReadModel => "Read model",
            _ => "Record",
        };

        foreach (var array in new[] { RecordFields.Input, RecordFields.Payload, RecordFields.Carries, RecordFields.Fields })
        {
            if (record.Raw.TryGetProperty(array, out var entries) && entries.ValueKind == JsonValueKind.Array)
            {
                foreach (var found in FieldIdentifies(entries, ""))
                {
                    yield return (array, label, found.Path, found.Target);
                }
            }
        }

        if (!record.Raw.TryGetProperty(RecordFields.Output, out var output))
        {
            yield break;
        }

        if (output.ValueKind == JsonValueKind.Array)
        {
            foreach (var found in FieldIdentifies(output, ""))
            {
                yield return ("output", label, found.Path, found.Target);
            }

            yield break;
        }

        if (output.ValueKind != JsonValueKind.Object
            || !output.TryGetProperty(RecordFields.Include, out var include)
            || include.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (var group in include.EnumerateObject())
        {
            if (group.Value.ValueKind == JsonValueKind.Object
                && group.Value.TryGetProperty(RecordFields.Fields, out var fields)
                && fields.ValueKind == JsonValueKind.Array)
            {
                foreach (var found in FieldIdentifies(fields, ""))
                {
                    yield return ("output", label, found.Path, found.Target);
                }
            }
        }
    }

    /// <summary>The identifies values of contract fields: each field's identifies with its dotted path, descending into list rows.</summary>
    /// <param name="entries">The contract field array whose identifies values are read.</param>
    /// <param name="prefix">The dotted path of the enclosing field, or empty at the top.</param>
    /// <returns>Each identifies value with its dotted field path.</returns>
    private static IEnumerable<(string Path, string Target)> FieldIdentifies(JsonElement entries, string prefix)
    {
        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = Str(entry, RecordFields.Name) ?? "(no name)";
            var path = prefix.Length > 0 ? $"{prefix}.{name}" : name;

            if (Str(entry, RecordFields.Identifies) is { Length: > 0 } target)
            {
                yield return (path, target);
            }

            if (entry.TryGetProperty(RecordFields.RowShape, out var shape)
                && shape.ValueKind == JsonValueKind.Object
                && shape.TryGetProperty(RecordFields.Fields, out var rows)
                && rows.ValueKind == JsonValueKind.Array)
            {
                foreach (var found in FieldIdentifies(rows, path))
                {
                    yield return found;
                }
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
                findings.Add(new Finding("record-invalid", $"External system '{system.Id}' processes data class '{dataClass ?? "none"}' of '{aggregate.Id}', which the aggregate does not declare.", system.File, system.Line, SubjectOf(system)));
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
                && point.Field(RecordFields.Authentication) is { Length: > 0 } auth
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

    /// <summary>Reports a module-event field that its translated domain event does not carry.</summary>
    /// <param name="model">The loaded project model that resolves the translated domain event.</param>
    /// <param name="published">The module event record whose fields are compared with the domain event it translates.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFieldsCarried(ProjectModel model, Record published, List<Finding> findings)
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
                || !domain.Raw.TryGetProperty(RecordFields.Carries, out var carries)
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
                    findings.Add(new Finding("event-field-uncarried", $"Module event '{published.Id}' publishes field '{field}' that its translated domain event '{domain.Id}' does not carry.", published.File, published.Line, SubjectOf(published)));
                }
            }
        }
    }

    /// <summary>Reports an integration-event payload field that its translated domain event does not carry.</summary>
    /// <param name="model">The loaded project model that resolves each translated domain event.</param>
    /// <param name="published">The integration event record whose payload is compared with the domain events it translates.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPayloadCarried(ProjectModel model, Record published, List<Finding> findings)
    {
        if (published.Raw.ValueKind != JsonValueKind.Object
            || !published.Raw.TryGetProperty(RecordFields.Payload, out var payload)
            || payload.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var domainId in published.Ids(RecordFields.Translates))
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
                    findings.Add(new Finding("event-field-uncarried", $"Integration event '{published.Id}' publishes field '{field}' that its translated domain event '{domain.Id}' does not carry.", published.File, published.Line, SubjectOf(published)));
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

        var siblings = SiblingsOf(fields);

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

            if (!ContractTypes.Contains(type))
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' with type '{type}' outside {string.Join(", ", ContractTypes.Order(StringComparer.Ordinal))}.", record.File, record.Line, SubjectOf(record)));
                continue;
            }

            if (!entry.TryGetProperty(RecordFields.Required, out var required)
                || required.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' with no required.", record.File, record.Line, SubjectOf(record)));
            }

            CheckContractOf(model, record, label, array, name, type, entry, findings);
            CheckFieldFacts(model, record, label, array + " field", name, type, entry, siblings, findings);
        }
    }

    /// <summary>Reports a use-case output field whose exact type facts name nothing, in both the field list and the include-group forms.</summary>
    /// <param name="model">The loaded project model that resolves each closed set and currency home.</param>
    /// <param name="useCase">The use case record whose output is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckOutputFields(ProjectModel model, Record useCase, List<Finding> findings)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Output, out var output))
        {
            return;
        }

        if (output.ValueKind == JsonValueKind.Array)
        {
            CheckFieldEntries(model, useCase, "Use case", "output", "output field", output, findings);
            return;
        }

        if (output.ValueKind != JsonValueKind.Object
            || !output.TryGetProperty(RecordFields.Include, out var include)
            || include.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var group in include.EnumerateObject())
        {
            if (group.Value.ValueKind != JsonValueKind.Object
                || !group.Value.TryGetProperty(RecordFields.Fields, out var fields)
                || fields.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            CheckFieldEntries(model, useCase, "Use case", "output group '" + group.Name + "'", "output group '" + group.Name + "' field", fields, findings);
        }
    }

    /// <summary>Reports a read-model field or a carried domain-event field whose exact type facts name nothing.</summary>
    /// <param name="model">The loaded project model that resolves each closed set and currency home.</param>
    /// <param name="record">The read model or domain event record whose fields are judged.</param>
    /// <param name="array">The field array name, fields or carries.</param>
    /// <param name="label">The record kind description that opens each finding.</param>
    /// <param name="noun">The array noun each finding names, read-model or carries.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFieldArray(ProjectModel model, Record record, string array, string label, string noun, List<Finding> findings)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(array, out var fields)
            || fields.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        CheckFieldEntries(model, record, label, noun, noun + " field", fields, findings);
    }

    /// <summary>Reports a carried field whose type or rowType names an aggregate part no aggregate declares.</summary>
    /// <param name="model">The loaded project model that resolves each named aggregate part.</param>
    /// <param name="domainEvent">The domain event record whose carries are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckCarryTypeRefs(ProjectModel model, Record domainEvent, List<Finding> findings)
    {
        if (domainEvent.Raw.ValueKind != JsonValueKind.Object
            || !domainEvent.Raw.TryGetProperty(RecordFields.Carries, out var fields)
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

            if (Str(entry, RecordFields.Type) is { Length: > 0 } type)
            {
                CheckPartTypeRef(model, domainEvent, name, "type", type, findings);
            }

            if (Str(entry, RecordFields.RowType) is { Length: > 0 } rowType)
            {
                CheckPartTypeRef(model, domainEvent, name, RecordFields.RowType, rowType, findings);
            }
        }
    }

    /// <summary>Reports an existing type reference naming a part of no record, of no aggregate, or of a part the aggregate does not declare. A value object part needs no identity, so any declared part resolves.</summary>
    /// <param name="model">The loaded project model that resolves the named aggregate.</param>
    /// <param name="domainEvent">The domain event record holding the carries field.</param>
    /// <param name="name">The carries field name quoted in each finding.</param>
    /// <param name="holder">The field holding the reference: type or rowType.</param>
    /// <param name="target">The reference value under test.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPartTypeRef(ProjectModel model, Record domainEvent, string name, string holder, string target, List<Finding> findings)
    {
        if (!TrySplitPart(target, out var baseId, out var part))
        {
            return;
        }

        if (!model.ById.TryGetValue(baseId, out var aggregate))
        {
            findings.Add(new Finding("reference-unknown", $"Domain event '{domainEvent.Id}' holds carries field '{name}' with {holder} '{target}', which names a part of '{baseId}', which no record holds.", domainEvent.File, domainEvent.Line, SubjectOf(domainEvent)));
            return;
        }

        if (aggregate.Type != RecordTypes.Aggregate)
        {
            findings.Add(new Finding("reference-unknown", $"Domain event '{domainEvent.Id}' holds carries field '{name}' with {holder} '{target}', which names a part of '{baseId}', which is no aggregate.", domainEvent.File, domainEvent.Line, SubjectOf(domainEvent)));
            return;
        }

        if (!DeclaresPart(aggregate, part))
        {
            findings.Add(new Finding("reference-unknown", $"Domain event '{domainEvent.Id}' holds carries field '{name}' with {holder} '{target}', which names part '{part}' the aggregate does not declare.", domainEvent.File, domainEvent.Line, SubjectOf(domainEvent)));
        }
    }

    /// <summary>Reports each field of one field array whose exact type facts name nothing.</summary>
    /// <param name="model">The loaded project model that resolves each closed set and currency home.</param>
    /// <param name="record">The record that holds the field array.</param>
    /// <param name="label">The record kind description that opens each finding.</param>
    /// <param name="array">The array noun the row-shape findings name.</param>
    /// <param name="holder">The field noun each finding names.</param>
    /// <param name="fields">The field array whose entries are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFieldEntries(ProjectModel model, Record record, string label, string array, string holder, JsonElement fields, List<Finding> findings)
    {
        var siblings = SiblingsOf(fields);

        foreach (var entry in fields.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = Str(entry, RecordFields.Name) ?? "(no name)";
            var type = Str(entry, RecordFields.Type);

            if (type is not { Length: > 0 } || !ContractTypes.Contains(type))
            {
                continue;
            }

            CheckFieldFacts(model, record, label, holder, name, type, entry, siblings, findings);

            if (type == "list" && entry.TryGetProperty(RecordFields.RowShape, out var rowShape))
            {
                CheckRowShape(model, record, label, array, name, rowShape, findings);
            }
        }
    }

    /// <summary>The field names of one field array with the type each states, for sibling checks.</summary>
    /// <param name="fields">The field array whose entries are read.</param>
    /// <returns>Each named entry with its stated type, or null when it states none.</returns>
    private static Dictionary<string, string?> SiblingsOf(JsonElement fields)
    {
        var siblings = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var entry in fields.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object && Str(entry, RecordFields.Name) is { Length: > 0 } name)
            {
                siblings.TryAdd(name, Str(entry, RecordFields.Type));
            }
        }

        return siblings;
    }

    /// <summary>Reports an identifies that names no record for an id, a choice with inline choices, a list with no row shape, a file with no content types and size, and an identifies or rowShape on a type that carries neither. An id field with no identifies reads as an untyped identifier and reports nothing.</summary>
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
        var hasId = entry.TryGetProperty(RecordFields.Identifies, out var of);
        var hasRow = entry.TryGetProperty(RecordFields.RowShape, out var rowShape);

        if (type == "id")
        {
            if (hasRow)
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type id with a rowShape that only list carries.", record.File, record.Line, SubjectOf(record)));
            }

            if (!hasId || of.ValueKind != JsonValueKind.String || of.GetString() is not { Length: > 0 } target)
            {
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
            if (entry.TryGetProperty(RecordFields.Choices, out _))
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type choice with inline choices; name the closed set in set.", record.File, record.Line, SubjectOf(record)));
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

            CheckRowShape(model, record, label, array, name, hasRow ? rowShape : default, findings);
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

    /// <summary>Reports an identifies or rowShape on a contract field type that carries neither.</summary>
    /// <param name="record">The record that holds the contract field.</param>
    /// <param name="label">The record kind description that opens each finding.</param>
    /// <param name="array">The contract array field name, input or payload.</param>
    /// <param name="name">The contract field name quoted in each finding.</param>
    /// <param name="type">The contract field type quoted in each finding.</param>
    /// <param name="hasId">Whether the field carries identifies.</param>
    /// <param name="hasRow">Whether the field carries rowShape.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void RefuseShapes(Record record, string label, string array, string name, string type, bool hasId, bool hasRow, List<Finding> findings)
    {
        if (hasId)
        {
            findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type {type} with an identifies that only id carries.", record.File, record.Line, SubjectOf(record)));
        }

        if (hasRow)
        {
            findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type {type} with a rowShape that only list carries.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a contract field whose closed set, currency home, currency field, length span, or numeric span names nothing or contradicts itself.</summary>
    /// <param name="model">The loaded project model that resolves each closed set and currency home.</param>
    /// <param name="record">The record that holds the contract field.</param>
    /// <param name="label">The record kind description that opens each finding.</param>
    /// <param name="holder">The field noun each finding names.</param>
    /// <param name="name">The contract field name quoted in each finding.</param>
    /// <param name="type">The contract field type that decides which facts are judged.</param>
    /// <param name="entry">The contract field object whose facts are judged.</param>
    /// <param name="siblings">The field names beside the entry with the type each states.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFieldFacts(ProjectModel model, Record record, string label, string holder, string name, string type, JsonElement entry, Dictionary<string, string?> siblings, List<Finding> findings)
    {
        if (type == "choice")
        {
            if (Str(entry, RecordFields.Set) is not { Length: > 0 })
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {holder} '{name}' of type choice with no set.", record.File, record.Line, SubjectOf(record)));
                return;
            }

            var set = Str(entry, RecordFields.Set)!;
            var baseId = Identifiers.BaseId(set);

            if (!model.ById.TryGetValue(baseId, out var target))
            {
                findings.Add(new Finding("reference-unknown", $"{label} '{record.Id}' holds {holder} '{name}' with set '{set}', which no record holds.", record.File, record.Line, SubjectOf(record)));
            }
            else if (target.Type != RecordTypes.ReferenceData && target.Type != RecordTypes.Setting)
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {holder} '{name}' with set '{set}', which is a {target.Type}.", record.File, record.Line, SubjectOf(record)));
            }
            else if (!HoldsChoices(target))
            {
                findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {holder} '{name}' with set '{set}', which holds no choices.", record.File, record.Line, SubjectOf(record)));
            }
        }

        if (type == "money")
        {
            if (Str(entry, RecordFields.CurrencyHome) is { Length: > 0 } home
                && !model.ById.ContainsKey(Identifiers.BaseId(home)))
            {
                findings.Add(new Finding("reference-unknown", $"{label} '{record.Id}' holds {holder} '{name}' with currencyHome '{home}', which no record holds.", record.File, record.Line, SubjectOf(record)));
            }

            if (Str(entry, RecordFields.CurrencyField) is { Length: > 0 } currencyField)
            {
                if (currencyField == name)
                {
                    findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {holder} '{name}' with currencyField '{currencyField}', which names the field itself.", record.File, record.Line, SubjectOf(record)));
                }
                else if (!siblings.TryGetValue(currencyField, out var siblingType))
                {
                    findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {holder} '{name}' with currencyField '{currencyField}', which names no sibling field.", record.File, record.Line, SubjectOf(record)));
                }
                else if (siblingType is { Length: > 0 } && siblingType != "text")
                {
                    findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {holder} '{name}' with currencyField '{currencyField}' naming '{currencyField}' of type '{siblingType}', which is no text.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }

        if (entry.TryGetProperty(RecordFields.MinLength, out var minLength)
            && entry.TryGetProperty(RecordFields.MaxLength, out var maxLength)
            && minLength.TryGetInt32(out var min)
            && maxLength.TryGetInt32(out var max)
            && min > max)
        {
            findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {holder} '{name}' with minLength {min} above maxLength {max}.", record.File, record.Line, SubjectOf(record)));
        }

        if (entry.TryGetProperty(RecordFields.Bounds, out var bounds)
            && bounds.ValueKind == JsonValueKind.Object
            && bounds.TryGetProperty(RecordFields.Min, out var lower)
            && bounds.TryGetProperty(RecordFields.Max, out var upper)
            && lower.ValueKind == JsonValueKind.Number
            && upper.ValueKind == JsonValueKind.Number
            && lower.TryGetDecimal(out var floor)
            && upper.TryGetDecimal(out var ceiling)
            && floor > ceiling)
        {
            findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {holder} '{name}' with bounds min {lower.GetRawText()} above max {upper.GetRawText()}.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Whether a closed-set record holds the choices a choice field takes.</summary>
    /// <param name="target">The reference-data or setting record the set names.</param>
    /// <returns>True when reference data lists choices, or a choice setting lists them.</returns>
    private static bool HoldsChoices(Record target)
    {
        if (target.Raw.ValueKind != JsonValueKind.Object
            || !target.Raw.TryGetProperty(RecordFields.Choices, out var choices)
            || choices.ValueKind != JsonValueKind.Array
            || choices.GetArrayLength() == 0)
        {
            return false;
        }

        return target.Type != RecordTypes.Setting || Str(target.Raw, RecordFields.ValueType) == "choice";
    }

    /// <summary>
    ///     Reports a list-of-rows shape that is no object with a non-empty fields array, a row field with no name, a type outside
    ///     the contract types, or no required flag, a row field whose exact type facts name nothing, a minItems that is no non-negative whole number, and a unique key that names no row field.
    /// </summary>
    /// <param name="model">The loaded project model that resolves each closed set and currency home.</param>
    /// <param name="record">The record whose list field is judged.</param>
    /// <param name="label">The record kind description that opens each finding.</param>
    /// <param name="array">The contract array field name, input, payload or output.</param>
    /// <param name="name">The contract field name quoted in each finding.</param>
    /// <param name="rowShape">The rowShape object of the list field, or a default element when the field states none.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckRowShape(ProjectModel model, Record record, string label, string array, string name, JsonElement rowShape, List<Finding> findings)
    {
        if (rowShape.ValueKind != JsonValueKind.Object
            || !rowShape.TryGetProperty(RecordFields.Fields, out var rowFields)
            || rowFields.ValueKind != JsonValueKind.Array
            || rowFields.GetArrayLength() == 0)
        {
            findings.Add(new Finding("record-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type list with no rowShape.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        var rowNames = new HashSet<string>(StringComparer.Ordinal);
        var siblings = SiblingsOf(rowFields);

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
            else
            {
                CheckFieldFacts(model, record, label, "row field of " + array + " field '" + name + "'", rowName, rowType, row, siblings, findings);
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
