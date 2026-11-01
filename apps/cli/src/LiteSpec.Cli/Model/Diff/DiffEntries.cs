using System.Text.Json;

namespace LiteSpec.Model.Diff;

/// <summary>
///     The entries the differ parses out of a record: transitions, states, data,
///     saga steps, exits, drivers, the access grant, effects, data classes,
///     journey steps, exceptions, options and open questions, each keyed as its
///     comparator walks it.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
/// <follows>litespec/core/release#review-behavior-changes-through-the-behavior-diff</follows>
static class DiffEntries
{
    /// <summary>
    ///     One entry per transition, keyed by its from, to and use case id and
    ///     displayed with the use case title, so a retitled use case reads the same.
    /// </summary>
    /// <param name="record">The aggregate record.</param>
    /// <param name="model">The model the transition's use case is titled from.</param>
    /// <returns>One entry per transition, or null when the transitions are not an array of objects.</returns>
    public static Dictionary<string, string>? TransitionEntries(Record record, ProjectModel model)
    {
        if (DiffValues.Field(record, RecordFields.Transitions) is not { } value)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var lines = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var from = StringOf(entry, "from") ?? "none";
            var to = StringOf(entry, "to") ?? "none";
            var by = StringOf(entry, "by");
            var line = $"{from} -> {to}";

            if (by is not null)
            {
                line += model.ById.TryGetValue(by, out var useCase)
                    ? $" by \"{useCase.Title}\""
                    : $" by {by}";
            }

            lines[$"{from}\0{to}\0{by ?? ""}"] = line;
        }

        return lines;
    }

    /// <summary>One entry per state, keyed by its name.</summary>
    /// <param name="record">The aggregate record.</param>
    /// <returns>One entry per state, or null when the states are not an array of objects.</returns>
    public static Dictionary<string, string>? StateEntries(Record record)
    {
        if (DiffValues.Field(record, RecordFields.States) is not { } value)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var states = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            states[StringOf(entry, "name") ?? "none"] = StringOf(entry, "meaning") ?? "";
        }

        return states;
    }

    /// <summary>Every state name of an aggregate, in name order.</summary>
    /// <param name="record">The aggregate record.</param>
    /// <returns>Every state name of the aggregate, in name order.</returns>
    public static List<string> StateNames(Record record) =>
        StateEntries(record)?.Keys.OrderBy(name => name, StringComparer.Ordinal).ToList() ?? [];

    /// <summary>One entry per data item, keyed by its name.</summary>
    /// <param name="record">The aggregate record.</param>
    /// <returns>One entry per data item, or null when the data are not an array of objects.</returns>
    public static Dictionary<string, (string Meaning, string Classification)>? DataEntries(Record record)
    {
        if (DiffValues.Field(record, RecordFields.Data) is not { } value)
        {
            return new Dictionary<string, (string, string)>(StringComparer.Ordinal);
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var data = new Dictionary<string, (string, string)>(StringComparer.Ordinal);

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            data[StringOf(entry, "name") ?? "none"] =
                (StringOf(entry, "meaning") ?? "", StringOf(entry, "classification") ?? "none");
        }

        return data;
    }

    /// <summary>One saga step per entry, keyed by its step id.</summary>
    /// <param name="record">The aggregate record.</param>
    /// <returns>One saga step per entry, or null when the steps are not an array of objects.</returns>
    public static Dictionary<string, JsonElement>? SagaStepEntries(Record record)
    {
        if (DiffValues.Field(record, RecordFields.Steps) is not { } value)
        {
            return new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var steps = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            steps[StringOf(entry, "id") ?? "none"] = entry;
        }

        return steps;
    }

    /// <summary>Every saga exit name: a bare string, or the name of an exit object.</summary>
    /// <param name="record">The aggregate record.</param>
    /// <returns>Every saga exit name, or null when the exits are not an array.</returns>
    public static List<string>? ExitNames(Record record)
    {
        if (DiffValues.Field(record, RecordFields.Exits) is not { } value)
        {
            return [];
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var names = new List<string>();

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.String && entry.GetString() is { } name)
            {
                names.Add(name);
            }
            else if (entry.ValueKind == JsonValueKind.Object)
            {
                names.Add(StringOf(entry, "name") ?? "none");
            }
            else
            {
                return null;
            }
        }

        return names;
    }

    /// <summary>Every saga driver as one id: its job or reaction, or the person driving it.</summary>
    /// <param name="value">The drivenBy value.</param>
    /// <returns>Every saga driver as one id.</returns>
    public static IReadOnlyList<string> DriverNames(JsonElement value)
    {
        var entries = value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray()
            : Enumerable.Repeat(value, 1);
        var names = new List<string>();

        foreach (var entry in entries)
        {
            if (entry.ValueKind == JsonValueKind.Object
                && (StringOf(entry, "job") ?? StringOf(entry, "reaction") ?? StringOf(entry, "person")) is { } name)
            {
                names.Add(name);
            }
        }

        return names;
    }

    /// <summary>One string property of a JSON object, or null when it carries none.</summary>
    /// <param name="element">The JSON object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The string property, or null when the object carries none.</returns>
    public static string? StringOf(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>The access grant a record declares, or null when it declares none in object form.</summary>
    /// <param name="record">The use case record.</param>
    /// <returns>The access grant the record declares, or null when it declares none in object form.</returns>
    public static AccessValue? AccessOf(Record record)
    {
        if (DiffValues.Field(record, RecordFields.Access) is not { } value)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var auth = new HashSet<string>(StringComparer.Ordinal);

        if (value.TryGetProperty("auth", out var kinds))
        {
            if (kinds.ValueKind == JsonValueKind.String && kinds.GetString() is { } single)
            {
                auth.Add(single);
            }
            else if (kinds.ValueKind == JsonValueKind.Array)
            {
                foreach (var kind in kinds.EnumerateArray())
                {
                    if (kind.ValueKind == JsonValueKind.String && kind.GetString() is { } name)
                    {
                        auth.Add(name);
                    }
                }
            }
            else
            {
                return null;
            }
        }

        return new AccessValue(auth, StringOf(value, "grant"), StringOf(value, "tenant"));
    }

    /// <summary>One entry per effect, keyed by its verb and target.</summary>
    /// <param name="record">The use case record.</param>
    /// <returns>One entry per effect, or null when the effects are not an array of objects.</returns>
    public static Dictionary<string, EffectEntry>? EffectEntries(Record record)
    {
        if (DiffValues.Field(record, RecordFields.Effects) is not { } value)
        {
            return new Dictionary<string, EffectEntry>(StringComparer.Ordinal);
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var effects = new Dictionary<string, EffectEntry>(StringComparer.Ordinal);

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var verb = StringOf(entry, "runs") is not null ? "runs"
                : StringOf(entry, "sends") is not null ? "sends"
                : StringOf(entry, "emits") is not null ? "emits"
                : StringOf(entry, "calls") is not null ? "calls"
                : null;

            if (verb is null)
            {
                return null;
            }

            var target = StringOf(entry, verb)!;

            effects[$"{verb}\0{target}"] = new EffectEntry(verb, target, StringOf(entry, "timing"));
        }

        return effects;
    }

    /// <summary>One entry per data class, keyed by its name.</summary>
    /// <param name="record">The record carrying the data classes.</param>
    /// <returns>One entry per data class, or null when the data classes are not an array of objects.</returns>
    public static Dictionary<string, DataClassValue>? DataClassEntries(Record record)
    {
        if (DiffValues.Field(record, RecordFields.DataClasses) is not { } value)
        {
            return new Dictionary<string, DataClassValue>(StringComparer.Ordinal);
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var classes = new Dictionary<string, DataClassValue>(StringComparer.Ordinal);

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            classes[StringOf(entry, "name") ?? "none"] =
                new DataClassValue(StringOf(entry, "classification"), StringOf(entry, "purpose"), StringOf(entry, "onErasure"));
        }

        return classes;
    }

    /// <summary>One entry per journey step, keyed by its step id.</summary>
    /// <param name="record">The journey record.</param>
    /// <returns>One entry per journey step, or null when the steps are not an array of objects.</returns>
    public static Dictionary<string, JourneyStep>? JourneySteps(Record record)
    {
        if (DiffValues.Field(record, RecordFields.Steps) is not { } value)
        {
            return new Dictionary<string, JourneyStep>(StringComparer.Ordinal);
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var steps = new Dictionary<string, JourneyStep>(StringComparer.Ordinal);

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            steps[StringOf(entry, "id") ?? "none"] = new JourneyStep(
                StringOf(entry, "statement") ?? "",
                StringOf(entry, "actor"),
                StringOf(entry, "failure"));
        }

        return steps;
    }

    /// <summary>One fallback per exception, keyed by its condition.</summary>
    /// <param name="record">The requirement record.</param>
    /// <returns>One fallback per exception, or null when the exceptions are not an array of objects.</returns>
    public static Dictionary<string, string>? ExceptEntries(Record record)
    {
        if (DiffValues.Field(record, RecordFields.Except) is not { } value)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var except = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            except[StringOf(entry, "condition") ?? "none"] = StringOf(entry, "fallback") ?? "none";
        }

        return except;
    }

    /// <summary>Every option name of a decision: a bare string, or the name of an option object.</summary>
    /// <param name="record">The decision record.</param>
    /// <returns>Every option name of the decision, or null when the options are not an array.</returns>
    public static List<string>? OptionNames(Record record)
    {
        if (DiffValues.Field(record, RecordFields.Options) is not { } value)
        {
            return [];
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var names = new List<string>();

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.String && entry.GetString() is { } name)
            {
                names.Add(name);
            }
            else if (entry.ValueKind == JsonValueKind.Object)
            {
                names.Add(StringOf(entry, "name") ?? "none");
            }
            else
            {
                return null;
            }
        }

        return names;
    }

    /// <summary>One entry per open question, keyed by the question text.</summary>
    /// <param name="record">The record carrying the questions.</param>
    /// <returns>One entry per open question, or null when the questions are not an array of objects.</returns>
    public static Dictionary<string, JsonElement>? QuestionEntries(Record record)
    {
        if (DiffValues.Field(record, RecordFields.Questions) is not { } value)
        {
            return new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var questions = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object || StringOf(entry, "question") is not { } question)
            {
                return null;
            }

            questions[question] = entry;
        }

        return questions;
    }
}
