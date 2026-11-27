using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.ProseText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.RecordShape.Schema;

/// <summary>The schema field rules: every required field holds a value of its shape, and every date parses as an ISO day or a timestamp.</summary>
/// <follows>litespec/core/model#state-the-common-fields-on-every-record</follows>
internal static class SchemaFieldRules
{
    /// <summary>Decides whether text states a date as an ISO day or a timestamp.</summary>
    /// <param name="text">The date text under test.</param>
    /// <returns>True when the text parses as an ISO date or a timestamp; otherwise false.</returns>
    internal static bool ValidDate(string text) =>
        IsoDate(text, out _)
        || DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _);

    /// <summary>Reports the record when the named field holds no non-empty string.</summary>
    /// <param name="record">The record whose field is required.</param>
    /// <param name="field">The field name that must hold text.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RequireString(Record record, string field, List<Finding> findings)
    {
        if (record.Field(field) is not { Length: > 0 })
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports the aggregate when any state entry lacks a name or a meaning.</summary>
    /// <param name="record">The aggregate record whose state entries are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RequireStateEntries(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.States, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object
                || !entry.TryGetProperty(RecordFields.Name, out var name)
                || name.ValueKind != JsonValueKind.String
                || string.IsNullOrEmpty(name.GetString())
                || !entry.TryGetProperty(RecordFields.Meaning, out var meaning)
                || meaning.ValueKind != JsonValueKind.String
                || string.IsNullOrEmpty(meaning.GetString()))
            {
                findings.Add(new Finding("record-invalid", $"Aggregate '{record.Id}' holds a state without a name and a meaning.", record.File, record.Line, SubjectOf(record)));
                return;
            }
        }
    }

    /// <summary>Reports the record when the named field is absent, holds no array, or holds fewer entries than required.</summary>
    /// <param name="record">The record whose field is required.</param>
    /// <param name="field">The field name that must hold an array.</param>
    /// <param name="minimum">The smallest entry count the field must hold.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RequireArray(Record record, string field, int minimum, List<Finding> findings)
    {
        if (!record.Has(field))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (record.Raw.TryGetProperty(field, out var value) && value.ValueKind != JsonValueKind.Array)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {field} as no array.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (value.GetArrayLength() < minimum)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds an empty {field}.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports the record when the named field names no identifier and at least one is required.</summary>
    /// <param name="record">The record whose field is required.</param>
    /// <param name="field">The field name that must name identifiers.</param>
    /// <param name="minimum">The smallest identifier count the field must name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RequireIds(Record record, string field, int minimum, List<Finding> findings)
    {
        var ids = record.Ids(field);

        if (!record.Has(field) || ids.Count == 0)
        {
            if (minimum > 0 || !record.Has(field))
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
            }

            return;
        }
    }

    /// <summary>Reports each holds entry that is not a role identifier, so a hold that carries a scope is refused.</summary>
    /// <param name="record">The actor record whose holds list is read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RequireRoleHolds(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.Holds, out var holds))
        {
            return;
        }

        if (holds.ValueKind != JsonValueKind.Array)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds no list in {RecordFields.Holds}.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        foreach (var entry in holds.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.String)
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds an entry in {RecordFields.Holds} that is not a role identifier.", record.File, record.Line, SubjectOf(record)));
            }
            else if (!Regex.IsMatch(entry.GetString() ?? "", @"^role/[a-z0-9][a-z0-9-]*$"))
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds '{entry.GetString()}' in {RecordFields.Holds}, which is not a role identifier.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Reports the record when the named field is absent or holds a value outside the allowed set.</summary>
    /// <param name="record">The record whose field is required.</param>
    /// <param name="field">The field name whose value is judged.</param>
    /// <param name="values">The allowed field values in message order.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RequireEnum(Record record, string field, string[] values, List<Finding> findings)
    {
        var value = record.Field(field);

        if (value is null)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
        else if (!values.Contains(value, StringComparer.Ordinal))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has {field} '{value}' outside {string.Join(", ", values)}.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a setBy outside the decider, operator and tenant who sets the value.</summary>
    /// <param name="record">The record whose setBy field is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckSetBy(Record record, List<Finding> findings)
    {
        if (record.Field(RecordFields.SetBy) is { } setBy && setBy is not ("product" or "operator" or "tenant"))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has setBy '{setBy}' outside product, operator, tenant.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports the record when the named field is absent or holds no boolean.</summary>
    /// <param name="record">The record whose field is required.</param>
    /// <param name="field">The field name that must hold a boolean.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RequireBoolean(Record record, string field, List<Finding> findings)
    {
        if (!record.Has(field))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
        else if (record.Raw.TryGetProperty(field, out var value)
            && value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {field} as no boolean.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports the record when the named field is absent or holds no date.</summary>
    /// <param name="record">The record whose field is required.</param>
    /// <param name="field">The field name that must hold a date.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RequireDate(Record record, string field, List<Finding> findings)
    {
        var value = record.Field(field);

        if (value is null)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
        else if (!ValidDate(value))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has {field} '{value}', which is not a date.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports the record when the named field is absent or its value breaks the required pattern.</summary>
    /// <param name="record">The record whose field is required.</param>
    /// <param name="field">The field name whose value is judged.</param>
    /// <param name="pattern">The regular expression the value must match in full.</param>
    /// <param name="shape">The prose name of the required form, shown in the finding.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RequirePattern(Record record, string field, string pattern, string shape, List<Finding> findings)
    {
        var value = record.Field(field);

        if (value is null)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
        else if (!Regex.IsMatch(value, pattern))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has {field} '{value}', which is not {shape}.", record.File, record.Line, SubjectOf(record)));
        }
    }
}
