using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks;

/// <summary>Reads the values the checks judge out of records: string fields, id links, exception and effect entries, reaction triggers, aggregate transitions and data, and the screens and claims they reach.</summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
internal static class RecordEntries
{
    /// <summary>The string value of one property on a JSON object, read without throwing on shape drift.</summary>
    /// <param name="element">The JSON value expected to hold the property.</param>
    /// <param name="name">The property name whose string value is wanted.</param>
    /// <returns>The property string, or null when the value is no object, the property is absent, or it holds no string.</returns>
    internal static string? Str(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>The record's exception entries: its single exception object or the objects in its exception array.</summary>
    /// <param name="record">The record whose exception field is read.</param>
    /// <returns>The exception objects, or an empty list when the record holds no exception field.</returns>
    internal static IReadOnlyList<JsonElement> ExceptEntries(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Except, out var except))
        {
            return [];
        }

        if (except.ValueKind == JsonValueKind.Object)
        {
            return [except];
        }

        return except.ValueKind == JsonValueKind.Array
            ? except.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object).ToList()
            : [];
    }

    /// <summary>The record's effect entries: objects in its effects array. A reaction or job entry without timing reads as async.</summary>
    /// <param name="record">The record whose effects array is read.</param>
    /// <returns>The effect objects in array order, or an empty list when the record holds no effects array.</returns>
    internal static IReadOnlyList<JsonElement> EffectEntries(Record record)
    {
        if (record.Raw.ValueKind == JsonValueKind.Object
            && record.Raw.TryGetProperty(RecordFields.Effects, out var effects)
            && effects.ValueKind == JsonValueKind.Array)
        {
            return effects.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object).ToList();
        }

        return [];
    }

    /// <summary>The aggregate's transitions; entries without both states are left to the shape check.</summary>
    /// <param name="aggregate">The aggregate record whose transitions array is read.</param>
    /// <returns>The transitions with both states named, or an empty list when the aggregate holds no transitions array.</returns>
    internal static IReadOnlyList<AggregateTransition> TransitionsOf(Record aggregate)
    {
        if (aggregate.Raw.ValueKind != JsonValueKind.Object
            || !aggregate.Raw.TryGetProperty(RecordFields.Transitions, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var found = new List<AggregateTransition>();

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty(RecordFields.From, out var from)
                && from.ValueKind == JsonValueKind.String
                && from.GetString() is { Length: > 0 } leave
                && entry.TryGetProperty(RecordFields.To, out var to)
                && to.ValueKind == JsonValueKind.String
                && to.GetString() is { Length: > 0 } enter)
            {
                found.Add(new AggregateTransition(
                    leave,
                    enter,
                    entry.TryGetProperty(RecordFields.By, out var by) && by.ValueKind == JsonValueKind.String ? by.GetString() : null));
            }
        }

        return found;
    }

    /// <summary>The aggregate's data fields; entries without a name and classification are left to the shape check.</summary>
    /// <param name="aggregate">The aggregate record whose data array is read.</param>
    /// <returns>The data fields with a name and a classification, or an empty list when the aggregate holds no data array.</returns>
    internal static IReadOnlyList<AggregateField> DataOf(Record aggregate)
    {
        if (aggregate.Raw.ValueKind != JsonValueKind.Object
            || !aggregate.Raw.TryGetProperty(RecordFields.Data, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var found = new List<AggregateField>();

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty(RecordFields.Name, out var name)
                && name.ValueKind == JsonValueKind.String
                && name.GetString() is { Length: > 0 } field
                && entry.TryGetProperty(RecordFields.Classification, out var classification)
                && classification.ValueKind == JsonValueKind.String
                && classification.GetString() is { Length: > 0 } level)
            {
                found.Add(new AggregateField(field, level));
            }
        }

        return found;
    }

    /// <summary>The screen's absolute file: screens keep the path relative to the consumer root.</summary>
    /// <param name="model">The loaded project model that supplies the consumer root.</param>
    /// <param name="file">The screen path, rooted or relative to the consumer root with forward slashes.</param>
    /// <returns>The absolute screen file path.</returns>
    internal static string ScreenFile(ProjectModel model, string file) =>
        Path.IsPathRooted(file)
            ? file
            : Path.GetFullPath(Path.Combine(model.Config.Root, file.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>Whether the claim confirms through the test body: a scenario or a rule.</summary>
    /// <param name="claim">The claimed record identifier under test.</param>
    /// <returns>True when the claim names a scenario or a rule; otherwise false.</returns>
    internal static bool IsBehaviorClaim(string claim) =>
        claim.StartsWith("scenario/", StringComparison.Ordinal)
        || claim.StartsWith("rule/", StringComparison.Ordinal);


    /// <summary>The screen records of the model.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>Each screen record in model order.</returns>
    internal static IReadOnlyList<Record> ScreenRecords(ProjectModel model) =>
        [.. model.Records.Where(record => record.Type == RecordTypes.Screen)];

    /// <summary>The reaction event triggers: the events its triggeredBy names, one id or a list.</summary>
    /// <param name="record">The reaction record whose triggeredBy field is read.</param>
    /// <returns>The triggering event identifiers the reaction names.</returns>
    internal static List<string> ReactionEventTriggers(Record record) =>
        record.Ids(RecordFields.TriggeredBy).Where(id => id.Length > 0).ToList();

    /// <summary>One field holding one id or an array of ids, read from any element.</summary>
    /// <param name="element">The JSON object that holds the field.</param>
    /// <param name="name">The field name whose identifiers are read.</param>
    /// <returns>The single identifier or the array identifiers, or an empty list when the field is absent or misshaped.</returns>
    internal static IReadOnlyList<string> LinkStrings(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return [];
        }

        if (value.ValueKind == JsonValueKind.String && value.GetString() is { } single)
        {
            return [single];
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            return
            [
                .. value.EnumerateArray()
                          .Where(entry => entry.ValueKind == JsonValueKind.String)
                          .Select(entry => entry.GetString()!),
            ];
        }

        return [];
    }
}
