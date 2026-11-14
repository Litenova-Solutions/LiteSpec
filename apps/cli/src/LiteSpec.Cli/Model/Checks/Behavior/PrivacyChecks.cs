using System.Text.Json;
using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The privacy checks: each aggregate data class and personal field holds an erasure position, each data class names what enforces its retention, and each retention schedule row names the reaction or job record that enforces it.</summary>
/// <follows>litespec/core/privacy#declare-every-data-class-with-its-erasure-position</follows>
/// <follows>litespec/core/privacy#schedule-every-data-class-for-retention</follows>
internal static class PrivacyChecks
{
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
        Str(element, RecordFields.EnforcedBy),
        LinkStrings(element, RecordFields.Fields),
        ExceptionsOf(element));

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

    /// <summary>Reports an aggregate data class or field holding personal data with no erasure position.</summary>
    /// <param name="model">The loaded project model that holds every aggregate record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunErasure(ProjectModel model, List<Finding> findings)
    {
        foreach (var aggregate in model.Records.Where(record => record.Type == RecordTypes.Aggregate))
        {
            var classes = DataClassesOf(aggregate);

            foreach (var position in classes.OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                if (Personal(position.Classification) && position.OnErasure is not { Length: > 0 })
                {
                    findings.Add(new Finding("erasure-undeclared", $"Aggregate '{aggregate.Id}' holds data class '{position.Name}' with {position.Classification} data and no erasure position.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }
            }

            var listed = classes.Any(position => position.Fields.Count > 0);

            foreach (var field in DataOf(aggregate)
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

    /// <summary>Reports an aggregate data class or field with no enforcedBy, and a retention schedule row whose enforcedBy names no reaction or job record.</summary>
    /// <param name="model">The loaded project model that holds every aggregate record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunRetention(ProjectModel model, List<Finding> findings)
    {
        foreach (var aggregate in model.Records.Where(record => record.Type == RecordTypes.Aggregate))
        {
            foreach (var position in DataClassesOf(aggregate).OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                if (position.EnforcedBy is not { Length: > 0 })
                {
                    findings.Add(new Finding("retention-enforcement-missing", $"Aggregate '{aggregate.Id}' holds data class '{position.Name}' with no enforcedBy.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }

                foreach (var exception in position.Exceptions.OrderBy(entry => entry.Field, StringComparer.Ordinal))
                {
                    if (exception.OnErasure is { Length: > 0 }
                        && exception.EnforcedBy is not { Length: > 0 }
                        && position.EnforcedBy is not { Length: > 0 })
                    {
                        findings.Add(new Finding("retention-enforcement-missing", $"Aggregate '{aggregate.Id}' holds field '{exception.Field}' of data class '{position.Name}' with no enforcedBy.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                    }
                }
            }
        }

        RunRetentionRows(model, findings);
    }

    /// <summary>Reports a retention schedule row whose enforcedBy is no reaction or job identifier, or names no record.</summary>
    /// <param name="model">The loaded project model that resolves each enforcedBy target.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void RunRetentionRows(ProjectModel model, List<Finding> findings)
    {
        // The retention schedule is a container page, not a record, so its rows
        // reach no record check; this check reads the page beside the records.
        var file = Path.Combine(model.Config.BehaviorDocs, "retention-schedule.yaml");

        if (!File.Exists(file))
        {
            return;
        }

        string text;

        try
        {
            text = SourceTree.Read(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        // A page the reader refuses, and rows without their shape, are left to record-invalid.
        if (!RecordFile.TryParseTree(text, out var tree, out _)
            || tree.Kind != JsonKind.Object
            || !tree.TryGetProperty(RecordFields.Rows, out var rows)
            || rows.Kind != JsonKind.Array)
        {
            return;
        }

        foreach (var row in rows.Items)
        {
            if (row.Kind != JsonKind.Object
                || !row.TryGetProperty(RecordFields.DataClass, out var dataClass)
                || dataClass.GetString() is not { Length: > 0 } name
                || !row.TryGetProperty(RecordFields.EnforcedBy, out var enforced)
                || enforced.GetString() is not { Length: > 0 } target)
            {
                continue;
            }

            if (!Identifiers.TrySplit(target, out _, out _, out _))
            {
                findings.Add(new Finding("field-invalid", $"Retention schedule row '{name}' holds '{target}' in enforcedBy, which is not a reaction or job identifier.", file, enforced.Line, null));
                continue;
            }

            if (!model.ById.TryGetValue(Identifiers.BaseId(target), out var resolved))
            {
                findings.Add(new Finding("reference-unknown", $"Retention schedule row '{name}' holds enforcedBy '{target}', which no record holds.", file, enforced.Line, null));
                continue;
            }

            if (resolved.Type is not (RecordTypes.Reaction or RecordTypes.Job))
            {
                findings.Add(new Finding("field-invalid", $"Retention schedule row '{name}' links enforcedBy to '{target}', which is a {resolved.Type}.", file, enforced.Line, null));
            }
        }
    }
}
