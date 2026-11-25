using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>
///     The idempotency key check: every placeholder an idempotency or deduplication key names is
///     carried by the record that uses the key, as an input or a field of a triggering event.
///     A reaction placeholder may reach one member deeper with a dotted path when the carry
///     names an aggregate part the records describe. A placeholder may state a .NET format
///     after a colon, judged by its field name, on a field of a formattable type.
/// </summary>
/// <follows>litespec/core/use-cases#carry-every-key-placeholder</follows>
internal static class IdempotencyKeyChecks
{
    /// <summary>A placeholder in a key template, written between braces.</summary>
    private static readonly Regex Placeholder = new(@"\{(?<name>[^{}]+)\}", RegexOptions.CultureInvariant);

    /// <summary>The contract value types, which hold no member a dotted placeholder could reach.</summary>
    private static readonly HashSet<string> ValueTypes = new(StringComparer.Ordinal)
    {
        "text", "integer", "decimal", "money", "boolean", "date", "instant", "duration", "email", "choice", "id", "file", "list", "map",
    };

    /// <summary>The contract types a key placeholder may format after a colon: the date, time, span, and number types the generated key renders through FormattableString.Invariant.</summary>
    private static readonly HashSet<string> FormattableTypes = new(StringComparer.Ordinal)
    {
        "date", "instant", "duration", "integer", "decimal",
    };

    /// <summary>Reports each key placeholder that no input or triggering event carries.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record =>
            record.Type is RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job))
        {
            var key = IdempotencyKey(record);

            if (key is null)
            {
                continue;
            }

            var placeholders = Placeholder.Matches(key)
                .Select(match => match.Groups["name"].Value.Trim())
                .Where(text => text.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(text => text, StringComparer.Ordinal)
                .ToList();

            if (placeholders.Count == 0)
            {
                continue;
            }

            var carried = CarriedFields(model, record);

            foreach (var placeholder in placeholders)
            {
                var (path, format) = SplitFormat(placeholder);

                if (path.Length == 0)
                {
                    findings.Add(new Finding("record-invalid",
                        $"{Label(record.Type)} '{record.Id}' holds idempotency key '{key}' where placeholder '{placeholder}' names no field; name a carried field, with an optional format as field:format.",
                        record.File, record.Line, SubjectOf(record)));
                    continue;
                }

                if (carried.Contains(path, StringComparer.Ordinal))
                {
                    CheckFormat(model, record, key, placeholder, path, format, findings);
                    continue;
                }

                if (record.Type == RecordTypes.Reaction
                    && path.Contains('.', StringComparison.Ordinal)
                    && ResolveDotted(model, record, path, out var dotted)
                    && dotted)
                {
                    CheckFormat(model, record, key, placeholder, path, format, findings);
                    continue;
                }

                if (record.Type == RecordTypes.Reaction
                    && path.Contains('.', StringComparison.Ordinal)
                    && DottedFinding(model, record, key, path) is { } finding)
                {
                    findings.Add(finding);
                    continue;
                }

                findings.Add(new Finding("idempotency-key-uncarried",
                    $"{Label(record.Type)} '{record.Id}' holds idempotency key '{key}' where placeholder '{placeholder}' is carried by no input or triggering event; carried: {string.Join(", ", carried.Order(StringComparer.Ordinal))}.",
                    record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Splits a placeholder into its field path and its optional .NET format: the text before the first colon names the carried field, and the text after it formats the value.</summary>
    /// <param name="placeholder">The placeholder text between the braces, trimmed.</param>
    /// <returns>The field path with the format, or null when the placeholder states no colon.</returns>
    private static (string Path, string? Format) SplitFormat(string placeholder)
    {
        var colon = placeholder.IndexOf(':');

        return colon < 0
            ? (placeholder, null)
            : (placeholder[..colon].Trim(), placeholder[(colon + 1)..].Trim());
    }

    /// <summary>Reports a format a carried placeholder states that its field cannot render: an empty format, a format with a quote or a newline, or a format on a field of no date, instant, duration, integer, or decimal type. A format on a dotted member path renders as stated, since the member type is not modeled.</summary>
    /// <param name="model">The loaded project model that resolves each carried field type.</param>
    /// <param name="record">The use case, reaction or job holding the key.</param>
    /// <param name="key">The key template quoted in each finding.</param>
    /// <param name="placeholder">The placeholder text between the braces, as written.</param>
    /// <param name="path">The carried field path the placeholder names.</param>
    /// <param name="format">The stated format, or null when the placeholder states none.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFormat(ProjectModel model, Record record, string key, string placeholder, string path, string? format, List<Finding> findings)
    {
        if (format is null)
        {
            return;
        }

        if (format.Length == 0)
        {
            findings.Add(new Finding("record-invalid",
                $"{Label(record.Type)} '{record.Id}' holds idempotency key '{key}' where placeholder '{placeholder}' states an empty format; state the .NET format after the colon, or drop the colon.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (format.IndexOfAny(['"', '\r', '\n']) >= 0)
        {
            findings.Add(new Finding("record-invalid",
                $"{Label(record.Type)} '{record.Id}' holds idempotency key '{key}' where placeholder '{placeholder}' states a format with a quote or a newline, which a key cannot render; state a format of letters, digits, and punctuation.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (path.Contains('.', StringComparison.Ordinal))
        {
            return;
        }

        if (FieldTypeOf(model, record, path) is not { Length: > 0 } type || FormattableTypes.Contains(type))
        {
            return;
        }

        findings.Add(new Finding("record-invalid",
            $"{Label(record.Type)} '{record.Id}' holds idempotency key '{key}' where placeholder '{placeholder}' formats '{path}' of type '{type}', which takes no format; a format needs a date, instant, duration, integer, or decimal field.",
            record.File, record.Line, SubjectOf(record)));
    }

    /// <summary>The contract type of one carried field: the input type of a use case, the triggering event field type of a reaction, or the run input type of a job.</summary>
    /// <param name="model">The loaded project model that resolves reaction triggers and job runs.</param>
    /// <param name="record">The use case, reaction or job holding the key.</param>
    /// <param name="field">The carried field name.</param>
    /// <returns>The contract type, or null when the field states none.</returns>
    private static string? FieldTypeOf(ProjectModel model, Record record, string field) =>
        record.Type switch
        {
            RecordTypes.UseCase => InputTypes(record).TryGetValue(field, out var type) ? type : null,
            RecordTypes.Reaction => CarryTypeOf(model, record, field),
            _ => JobInputTypes(model, record).TryGetValue(field, out var type) ? type : null,
        };

    /// <summary>The input field types of a record, by field name.</summary>
    /// <param name="record">The record whose input array is read.</param>
    /// <returns>Each input name with the type it states, when it states one.</returns>
    private static Dictionary<string, string> InputTypes(Record record)
    {
        var types = new Dictionary<string, string>(StringComparer.Ordinal);

        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Input, out var input)
            || input.ValueKind != JsonValueKind.Array)
        {
            return types;
        }

        foreach (var entry in input.EnumerateArray())
        {
            if (Str(entry, RecordFields.Name) is { Length: > 0 } name
                && Str(entry, RecordFields.Type) is { Length: > 0 } type)
            {
                types.TryAdd(name, type);
            }
        }

        return types;
    }

    /// <summary>The input field types of the use cases a job runs or commands, by field name.</summary>
    /// <param name="model">The loaded project model that resolves the run use cases.</param>
    /// <param name="job">The job whose run effects are read.</param>
    /// <returns>Each run input name with the type it states, when it states one.</returns>
    private static Dictionary<string, string> JobInputTypes(ProjectModel model, Record job)
    {
        var types = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var effect in EffectEntries(job))
        {
            foreach (var id in LinkStrings(effect, RecordFields.Runs)
                .Concat(LinkStrings(effect, RecordFields.Commands)))
            {
                if (model.ById.TryGetValue(Identifiers.BaseId(id), out var useCase)
                    && useCase.Type == RecordTypes.UseCase)
                {
                    foreach (var (name, type) in InputTypes(useCase))
                    {
                        types.TryAdd(name, type);
                    }
                }
            }
        }

        return types;
    }

    /// <summary>Whether a dotted reaction placeholder resolves: its head names a triggering event field whose type is an aggregate part the records describe.</summary>
    /// <param name="model">The loaded project model that resolves the triggering events and the named part.</param>
    /// <param name="reaction">The reaction whose triggering events are read.</param>
    /// <param name="name">The dotted placeholder.</param>
    /// <param name="resolved">True when the path resolves through a declared part; otherwise false.</param>
    /// <returns>True when the placeholder states a two-part path the check judges; otherwise false.</returns>
    private static bool ResolveDotted(ProjectModel model, Record reaction, string name, out bool resolved)
    {
        resolved = false;

        if (name.Split('.') is not [var head, var member] || head.Length == 0 || member.Length == 0)
        {
            return false;
        }

        if (CarryTypeOf(model, reaction, head) is not { Length: > 0 } type)
        {
            return false;
        }

        if (!ContractChecks.TrySplitPart(type, out var baseId, out var part))
        {
            return true;
        }

        resolved = model.ById.TryGetValue(baseId, out var aggregate)
            && aggregate.Type == RecordTypes.Aggregate
            && ContractChecks.DeclaresPart(aggregate, part);

        return true;
    }

    /// <summary>The finding for a dotted reaction placeholder that resolves to no declared part: a C# type name the records do not describe, or a value type holding no member.</summary>
    /// <param name="model">The loaded project model that resolves the triggering events.</param>
    /// <param name="reaction">The reaction whose triggering events are read.</param>
    /// <param name="key">The key template quoted in the finding.</param>
    /// <param name="name">The dotted placeholder.</param>
    /// <returns>The finding, or null when the placeholder states no two-part path over a carried field, or names a part form the part check judges.</returns>
    private static Finding? DottedFinding(ProjectModel model, Record reaction, string key, string name)
    {
        if (name.Split('.') is not [var head, _] || head.Length == 0)
        {
            return null;
        }

        if (CarryTypeOf(model, reaction, head) is not { Length: > 0 } type)
        {
            return null;
        }

        if (ContractChecks.TrySplitPart(type, out _, out _))
        {
            return null;
        }

        if (ValueTypes.Contains(type))
        {
            return new Finding("idempotency-key-uncarried",
                $"Reaction '{reaction.Id}' holds idempotency key '{key}' where placeholder '{name}' reaches into carry '{head}' of type '{type}', which holds no member; key on the carried field itself.",
                reaction.File, reaction.Line, SubjectOf(reaction));
        }

        return new Finding("idempotency-key-uncarried",
            $"Reaction '{reaction.Id}' holds idempotency key '{key}' where placeholder '{name}' reaches into carry '{head}' of type '{type}', which the records do not describe; name an aggregate part in type, as aggregate/<module>.<aggregate>#<part>, so the path resolves.",
            reaction.File, reaction.Line, SubjectOf(reaction));
    }

    /// <summary>The carried type of one triggering event field: the domain carry type, or the module event field's override when it states its own.</summary>
    /// <param name="model">The loaded project model that resolves each trigger.</param>
    /// <param name="reaction">The reaction whose triggers are read.</param>
    /// <param name="field">The triggering event field name.</param>
    /// <returns>The carried type, or null when no triggering event carries the field.</returns>
    private static string? CarryTypeOf(ProjectModel model, Record reaction, string field)
    {
        foreach (var trigger in ReactionEventTriggers(reaction))
        {
            if (!model.ById.TryGetValue(Identifiers.BaseId(trigger), out var @event))
            {
                continue;
            }

            if (@event.Type == RecordTypes.ModuleEvent)
            {
                if (ModuleFieldTypeOf(model, @event, field) is { } selected)
                {
                    return selected;
                }

                continue;
            }

            var listName = @event.Type == RecordTypes.DomainEvent ? RecordFields.Carries : RecordFields.Payload;

            if (@event.Raw.ValueKind == JsonValueKind.Object
                && @event.Raw.TryGetProperty(listName, out var list)
                && list.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in list.EnumerateArray())
                {
                    if (Str(entry, RecordFields.Name) == field
                        && Str(entry, RecordFields.Type) is { Length: > 0 } type)
                    {
                        return type;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>The carried type of one module event field: its own override when it states one, else the translated domain carry type.</summary>
    /// <param name="model">The loaded project model that resolves the translated domain event.</param>
    /// <param name="published">The module event record whose fields are read.</param>
    /// <param name="field">The published field name.</param>
    /// <returns>The carried type, or null when the module event publishes no such field.</returns>
    private static string? ModuleFieldTypeOf(ProjectModel model, Record published, string field)
    {
        if (published.Raw.ValueKind != JsonValueKind.Object
            || !published.Raw.TryGetProperty(RecordFields.Fields, out var selected)
            || selected.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var found = false;
        string? overrideType = null;

        foreach (var entry in selected.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.String && entry.GetString() == field)
            {
                found = true;
                break;
            }

            if (entry.ValueKind == JsonValueKind.Object && Str(entry, RecordFields.Name) == field)
            {
                found = true;
                overrideType = Str(entry, RecordFields.Type);
                break;
            }
        }

        if (!found)
        {
            return null;
        }

        if (overrideType is { Length: > 0 })
        {
            return overrideType;
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

            foreach (var carry in carries.EnumerateArray())
            {
                if (Str(carry, RecordFields.Name) == field
                    && Str(carry, RecordFields.Type) is { Length: > 0 } type)
                {
                    return type;
                }
            }
        }

        return null;
    }

    /// <summary>The idempotency key template of a record, when it holds one.</summary>
    /// <param name="record">The record whose idempotency key is read.</param>
    /// <returns>The key template, or null when the record holds no key.</returns>
    private static string? IdempotencyKey(Record record) =>
        record.Raw.ValueKind == JsonValueKind.Object
        && record.Raw.TryGetProperty(RecordFields.Idempotency, out var idempotency)
        && idempotency.ValueKind == JsonValueKind.Object
        && Str(idempotency, RecordFields.Key) is { Length: > 0 } key
            ? key
            : null;

    /// <summary>The field names the record carries for its key.</summary>
    /// <param name="model">The loaded project model that resolves reaction triggers and job runs.</param>
    /// <param name="record">The use case, reaction or job whose carried fields are read.</param>
    /// <returns>The carried field names.</returns>
    private static List<string> CarriedFields(ProjectModel model, Record record) =>
        record.Type switch
        {
            RecordTypes.UseCase => InputNames(record).ToList(),
            RecordTypes.Reaction => TriggerFields(model, record).ToList(),
            _ => RunInputNames(model, record).ToList(),
        };

    /// <summary>The input field names of a record.</summary>
    /// <param name="record">The record whose input array is read.</param>
    /// <returns>The input field names in written order.</returns>
    private static List<string> InputNames(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Input, out var input)
            || input.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return input.EnumerateArray()
            .Select(entry => Str(entry, RecordFields.Name))
            .OfType<string>()
            .Where(name => name.Length > 0)
            .ToList();
    }

    /// <summary>The published field names of the events a reaction answers.</summary>
    /// <param name="model">The loaded project model that resolves each trigger.</param>
    /// <param name="reaction">The reaction whose triggers are read.</param>
    /// <returns>The field names the triggering events carry.</returns>
    private static List<string> TriggerFields(ProjectModel model, Record reaction)
    {
        var fields = new List<string>();

        foreach (var trigger in ReactionEventTriggers(reaction))
        {
            if (!model.ById.TryGetValue(Identifiers.BaseId(trigger), out var @event))
            {
                continue;
            }

            if (@event.Type == RecordTypes.ModuleEvent)
            {
                if (@event.Raw.ValueKind == JsonValueKind.Object
                    && @event.Raw.TryGetProperty(RecordFields.Fields, out var selected)
                    && selected.ValueKind == JsonValueKind.Array)
                {
                    foreach (var entry in selected.EnumerateArray())
                    {
                        if (entry.ValueKind == JsonValueKind.String && entry.GetString() is { Length: > 0 } name)
                        {
                            fields.Add(name);
                        }
                        else if (entry.ValueKind == JsonValueKind.Object && Str(entry, RecordFields.Name) is { Length: > 0 } named)
                        {
                            fields.Add(named);
                        }
                    }
                }

                continue;
            }

            var listName = @event.Type == RecordTypes.DomainEvent ? RecordFields.Carries : RecordFields.Payload;

            if (@event.Raw.ValueKind == JsonValueKind.Object
                && @event.Raw.TryGetProperty(listName, out var list)
                && list.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in list.EnumerateArray())
                {
                    if (Str(entry, RecordFields.Name) is { Length: > 0 } name)
                    {
                        fields.Add(name);
                    }
                }
            }
        }

        return fields;
    }

    /// <summary>The input field names of the use cases a job runs or commands.</summary>
    /// <param name="model">The loaded project model that resolves the run use cases.</param>
    /// <param name="job">The job whose run effects are read.</param>
    /// <returns>The field names the job's run window carries.</returns>
    private static List<string> RunInputNames(ProjectModel model, Record job)
    {
        var names = new List<string>();

        foreach (var effect in EffectEntries(job))
        {
            foreach (var id in LinkStrings(effect, RecordFields.Runs)
                .Concat(LinkStrings(effect, RecordFields.Commands)))
            {
                if (model.ById.TryGetValue(Identifiers.BaseId(id), out var useCase)
                    && useCase.Type == RecordTypes.UseCase)
                {
                    names.AddRange(InputNames(useCase));
                }
            }
        }

        return names;
    }
}
