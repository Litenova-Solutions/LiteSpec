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
/// </summary>
/// <follows>litespec/core/use-cases#carry-every-key-placeholder</follows>
internal static class IdempotencyKeyChecks
{
    /// <summary>A placeholder in a key template, written between braces.</summary>
    private static readonly Regex Placeholder = new(@"\{(?<name>[^{}]+)\}", RegexOptions.CultureInvariant);

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

            var names = Placeholder.Matches(key)
                .Select(match => match.Groups["name"].Value.Trim())
                .Where(name => name.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            if (names.Count == 0)
            {
                continue;
            }

            var carried = CarriedFields(model, record);

            foreach (var name in names)
            {
                if (carried.Contains(name, StringComparer.Ordinal))
                {
                    continue;
                }

                findings.Add(new Finding("idempotency-key-uncarried",
                    $"{Label(record.Type)} '{record.Id}' holds idempotency key '{key}' where placeholder '{name}' is carried by no input or triggering event; carried: {string.Join(", ", carried.Order(StringComparer.Ordinal))}.",
                    record.File, record.Line, SubjectOf(record)));
            }
        }
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
    /// <param name="record">The record whose inputs array is read.</param>
    /// <returns>The input field names in written order.</returns>
    private static List<string> InputNames(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Inputs, out var input)
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
                    }
                }

                continue;
            }

            var listName = RecordFields.Fields;

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

    /// <summary>The input field names of the use cases a job runs.</summary>
    /// <param name="model">The loaded project model that resolves the run use cases.</param>
    /// <param name="job">The job whose run effects are read.</param>
    /// <returns>The field names the job's run window carries.</returns>
    private static List<string> RunInputNames(ProjectModel model, Record job)
    {
        var names = new List<string>();

        foreach (var effect in EffectEntries(job))
        {
            foreach (var id in LinkStrings(effect, RecordFields.Runs))
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
