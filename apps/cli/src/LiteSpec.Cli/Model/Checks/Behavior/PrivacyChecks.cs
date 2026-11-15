using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.ProseText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The privacy checks: each aggregate data class and personal field holds an erasure position, each data class states its classification and its field list and holds the fields that name it, and each data class states its retention period, the event it counts from, its basis and what enforces it.</summary>
/// <follows>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</follows>
internal static class PrivacyChecks
{
    /// <summary>The period ends that a data class may count from by a calendar rather than an event.</summary>
    private static readonly string[] CalendarStarts = ["financial-year-end", "calendar-year-end"];

    /// <summary>The aggregate data classes, in array or map form.</summary>
    /// <param name="aggregate">The aggregate record whose data classes field is read.</param>
    /// <returns>Each declared data class position, or an empty list when the aggregate declares none.</returns>
    private static List<DataClassPosition> DataClassesOf(Record aggregate)
    {
        if (aggregate.Raw.ValueKind != JsonValueKind.Object
            || !aggregate.Raw.TryGetProperty(RecordFields.DataClasses, out var classes))
        {
            return [];
        }

        var found = new List<DataClassPosition>();

        if (classes.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in classes.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.Object
                    && Str(entry, RecordFields.Name) is { Length: > 0 } name)
                {
                    found.Add(ReadClass(name, entry));
                }
            }
        }
        else if (classes.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in classes.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    found.Add(ReadClass(property.Name, property.Value));
                }
            }
        }

        return found;
    }

    /// <summary>Reads one data class position from its JSON object form.</summary>
    /// <param name="name">The data class name as declared.</param>
    /// <param name="element">The JSON object that holds the class position fields.</param>
    /// <returns>The data class position with each declared field read.</returns>
    private static DataClassPosition ReadClass(string name, JsonElement element) => new(
        name,
        Str(element, RecordFields.Classification) ?? "",
        Str(element, RecordFields.OnErasure),
        LinkStrings(element, RecordFields.EnforcedBy),
        LinkStrings(element, RecordFields.Fields),
        ExceptionsOf(element),
        Str(element, RecordFields.Period),
        Str(element, RecordFields.PeriodStartsAt),
        LinkStrings(element, RecordFields.Basis));

    /// <summary>Reads the field exceptions of one data class object.</summary>
    /// <param name="element">The JSON object that holds the exceptions array.</param>
    /// <returns>Each field exception with its erasure position, or an empty list when it declares none.</returns>
    private static List<DataClassException> ExceptionsOf(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(RecordFields.Exceptions, out var exceptions)
            || exceptions.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var found = new List<DataClassException>();

        foreach (var entry in exceptions.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object
                && Str(entry, RecordFields.Field) is { Length: > 0 } field)
            {
                found.Add(new DataClassException(
                    field,
                    Str(entry, RecordFields.OnErasure),
                    Str(entry, RecordFields.EnforcedBy)));
            }
        }

        return found;
    }

    /// <summary>One aggregate data field as the checks read it: its name, classification and the data class it names.</summary>
    /// <param name="aggregate">The aggregate record whose data array is read.</param>
    /// <returns>Each data entry with a name and a classification, with its data class when it names one.</returns>
    private static List<(string Name, string Classification, string? DataClass)> FieldsOf(Record aggregate)
    {
        var found = new List<(string Name, string Classification, string? DataClass)>();

        if (aggregate.Raw.ValueKind != JsonValueKind.Object
            || !aggregate.Raw.TryGetProperty(RecordFields.Data, out var list)
            || list.ValueKind != JsonValueKind.Array)
        {
            return found;
        }

        foreach (var entry in list.EnumerateArray())
        {
            if (Str(entry, RecordFields.Name) is { Length: > 0 } name
                && Str(entry, RecordFields.Classification) is { Length: > 0 } classification)
            {
                found.Add((name, classification, Str(entry, RecordFields.DataClass)));
            }
        }

        return found;
    }

    /// <summary>Reports an aggregate data class or field holding personal data with no erasure position, a field whose classification differs from its data class, a field naming a data class the aggregate does not declare, and a data class whose field list differs from the fields naming it.</summary>
    /// <param name="model">The loaded project model that holds every aggregate record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunErasure(ProjectModel model, List<Finding> findings)
    {
        foreach (var aggregate in model.Records.Where(record => record.Type == RecordTypes.Aggregate))
        {
            var classes = DataClassesOf(aggregate);
            var fields = FieldsOf(aggregate);

            foreach (var position in classes.OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                if (Personal(position.Classification) && position.OnErasure is not { Length: > 0 })
                {
                    findings.Add(new Finding("erasure-undeclared", $"Aggregate '{aggregate.Id}' holds data class '{position.Name}' with {position.Classification} data and no erasure position.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }

                var named = fields.Where(field => field.DataClass == position.Name).Select(field => field.Name).ToHashSet(StringComparer.Ordinal);

                if (!named.SetEquals(position.Fields))
                {
                    findings.Add(new Finding("data-class-fields-drift", $"Aggregate '{aggregate.Id}' data class '{position.Name}' lists fields '{string.Join("', '", position.Fields.Order(StringComparer.Ordinal))}' but the fields naming it are '{string.Join("', '", named.Order(StringComparer.Ordinal))}'.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }
            }

            foreach (var field in fields.OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                if (field.DataClass is not { Length: > 0 } className)
                {
                    continue;
                }

                if (classes.FirstOrDefault(position => position.Name == className) is not { } owner)
                {
                    findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' field '{field.Name}' names data class '{className}', which the aggregate does not declare.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }
                else if (owner.Classification != field.Classification)
                {
                    findings.Add(new Finding("classification-drift", $"Aggregate '{aggregate.Id}' field '{field.Name}' is {field.Classification} but its data class '{className}' is {owner.Classification}.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }
            }

            var listed = classes.Any(position => position.Fields.Count > 0);

            foreach (var field in fields
                .Where(entry => Personal(entry.Classification))
                .OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                if (!FieldCovered(classes, listed, field.Name))
                {
                    findings.Add(new Finding("erasure-undeclared", $"Aggregate '{aggregate.Id}' holds field '{field.Name}' with {field.Classification} data and no erasure position.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }
            }
        }
    }

    /// <summary>
    ///     Whether a declared erasure position covers the field: its own exception,
    ///     a class listing it, or any class where no class lists fields.
    /// </summary>
    /// <param name="classes">The data class positions declared on the aggregate.</param>
    /// <param name="listed">Whether any class lists its fields explicitly.</param>
    /// <param name="field">The data field name under test.</param>
    /// <returns>True when an exception, a listing class, or an unlisted class erasure covers the field; otherwise false.</returns>
    private static bool FieldCovered(IReadOnlyList<DataClassPosition> classes, bool listed, string field) =>
        classes.Any(position => position.Exceptions.Any(exception =>
            exception.Field == field && exception.OnErasure is { Length: > 0 }))
        || classes.Any(position => position is { OnErasure: { Length: > 0 } }
            && (!listed || position.Fields.Contains(field, StringComparer.Ordinal)));

    /// <summary>Decides whether a data classification holds personal data.</summary>
    /// <param name="classification">The classification text as declared.</param>
    /// <returns>True for the personal and sensitive classifications, ignoring case; otherwise false.</returns>
    private static bool Personal(string classification) =>
        classification.Equals("personal", StringComparison.OrdinalIgnoreCase)
        || classification.Equals("sensitive", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Reports a data class with no enforcedBy, an enforcer that is no job or use case, a period with no
    ///     start or basis, a period start that names no domain event or calendar end, and a basis that names no record.
    /// </summary>
    /// <param name="model">The loaded project model that holds every aggregate record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunRetention(ProjectModel model, List<Finding> findings)
    {
        foreach (var aggregate in model.Records.Where(record => record.Type == RecordTypes.Aggregate))
        {
            foreach (var position in DataClassesOf(aggregate).OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                if (position.EnforcedBy.Count == 0)
                {
                    findings.Add(new Finding("retention-enforcement-missing", $"Aggregate '{aggregate.Id}' holds data class '{position.Name}' with no enforcedBy.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }

                foreach (var enforcer in position.EnforcedBy.Order(StringComparer.Ordinal))
                {
                    if (!model.ById.TryGetValue(Identifiers.BaseId(enforcer), out var resolved))
                    {
                        findings.Add(new Finding("reference-unknown", $"Aggregate '{aggregate.Id}' data class '{position.Name}' names enforcer '{enforcer}', which no record holds.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                    }
                    else if (resolved.Type is not (RecordTypes.Job or RecordTypes.UseCase))
                    {
                        findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' data class '{position.Name}' names enforcer '{enforcer}', which is a {resolved.Type}.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                    }
                }

                if (position.Period is not null && !IsoDuration.IsMatch(position.Period))
                {
                    findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' data class '{position.Name}' holds period '{position.Period}', which is not an ISO 8601 duration.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }

                if (position.Period is not null && position.PeriodStartsAt is null)
                {
                    findings.Add(new Finding("record-invalid", $"Aggregate '{aggregate.Id}' data class '{position.Name}' holds a period with no periodStartsAt.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }

                if (position.Period is not null && position.Basis.Count == 0)
                {
                    findings.Add(new Finding("record-invalid", $"Aggregate '{aggregate.Id}' data class '{position.Name}' holds a period with no basis.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }

                if (position.PeriodStartsAt is { } start && !PeriodStartValid(model, start))
                {
                    findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' data class '{position.Name}' counts its period from '{start}', which is no domain event, state transition or calendar end.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }

                foreach (var basis in position.Basis.Order(StringComparer.Ordinal))
                {
                    if (!model.ById.ContainsKey(Identifiers.BaseId(basis)))
                    {
                        findings.Add(new Finding("reference-unknown", $"Aggregate '{aggregate.Id}' data class '{position.Name}' names basis '{basis}', which no record holds.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                    }
                }

                foreach (var exception in position.Exceptions.OrderBy(entry => entry.Field, StringComparer.Ordinal))
                {
                    if (exception.OnErasure is { Length: > 0 }
                        && exception.EnforcedBy is not { Length: > 0 }
                        && position.EnforcedBy.Count == 0)
                    {
                        findings.Add(new Finding("retention-enforcement-missing", $"Aggregate '{aggregate.Id}' holds field '{exception.Field}' of data class '{position.Name}' with no enforcedBy.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                    }
                }
            }
        }
    }

    /// <summary>Decides whether a period start names a domain event that exists, a state transition written as two states, or a calendar year end.</summary>
    /// <param name="model">The loaded project model that resolves a domain event start.</param>
    /// <param name="start">The periodStartsAt value under test.</param>
    /// <returns>True when the start is a known form; otherwise false.</returns>
    private static bool PeriodStartValid(ProjectModel model, string start)
    {
        if (CalendarStarts.Contains(start, StringComparer.Ordinal))
        {
            return true;
        }

        if (start.StartsWith(RecordTypes.DomainEvent + "/", StringComparison.Ordinal))
        {
            return model.ById.TryGetValue(Identifiers.BaseId(start), out var resolved) && resolved.Type == RecordTypes.DomainEvent;
        }

        var parts = start.Split(" to ", StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2 && parts.All(part => part.Length > 0);
    }
}
