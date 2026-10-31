using System.Text.Json;

namespace LiteSpec.Model.Diff;

/// <summary>Compares two scenario records: given, when, then, result, and examples.</summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
/// <follows>litespec/core/release#review-behavior-changes-through-the-behavior-diff</follows>
static class ScenarioDiffer
{
    /// <summary>Compares a scenario, adding detail lines.</summary>
    /// <param name="before">The base scenario.</param>
    /// <param name="after">The head scenario.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    public static void CompareScenario(Record before, Record after, List<string> details)
    {
        var handled = new HashSet<string>(StringComparer.Ordinal) { RecordFields.Given, RecordFields.When, RecordFields.Then, RecordFields.Result, RecordFields.Examples };

        DiffValues.CompareValue("Given", DiffValues.Field(before, RecordFields.Given), DiffValues.Field(after, RecordFields.Given), details);
        DiffValues.CompareValue("When", DiffValues.Field(before, RecordFields.When), DiffValues.Field(after, RecordFields.When), details);
        DiffValues.CompareValue("Then", DiffValues.Field(before, RecordFields.Then), DiffValues.Field(after, RecordFields.Then), details);
        DiffValues.CompareValue("Result", DiffValues.Field(before, RecordFields.Result), DiffValues.Field(after, RecordFields.Result), details);
        CompareExamples(DiffValues.Field(before, RecordFields.Examples), DiffValues.Field(after, RecordFields.Examples), details);
        DiffValues.CompareFields(before, after, details, handled);
    }

    /// <summary>The one-line summary of a scenario: its given, when, and then.</summary>
    /// <param name="record">The scenario record.</param>
    /// <returns>The one-line summary of the scenario.</returns>
    public static string Line(Record record) =>
        $"Given {record.Field(RecordFields.Given) ?? "nothing"}; when {record.Field(RecordFields.When) ?? "nothing"}; then {record.Field(RecordFields.Then) ?? "nothing"}.";

    /// <summary>Compares a scenario's examples by row.</summary>
    /// <param name="before">The base examples, or null when the field is absent.</param>
    /// <param name="after">The head examples, or null when the field is absent.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    public static void CompareExamples(JsonElement? before, JsonElement? after, List<string> details)
    {
        var oldRows = ExampleRows(before);
        var newRows = ExampleRows(after);

        if (oldRows is null || newRows is null)
        {
            if (!DiffValues.JsonEquals(before, after))
            {
                details.Add("Examples changed.");
            }

            return;
        }

        var added = newRows.Except(oldRows).Count();
        var removed = oldRows.Except(newRows).Count();

        if (added > 0 && removed > 0)
        {
            details.Add($"Examples changed: +{added} rows, -{removed} rows.");
        }
        else if (added > 0)
        {
            details.Add($"Examples changed: +{added} rows.");
        }
        else if (removed > 0)
        {
            details.Add($"Examples changed: -{removed} rows.");
        }
    }

    /// <summary>The canonical rows of an examples value, or null when it is not an array.</summary>
    /// <param name="value">The examples value, or null when the field is absent.</param>
    /// <returns>The canonical rows of the examples value, or null when it is not an array.</returns>
    public static HashSet<string>? ExampleRows(JsonElement? value)
    {
        if (value is null)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        if (value.Value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return value.Value.EnumerateArray().Select(CanonicalJson.Canonical).ToHashSet(StringComparer.Ordinal);
    }
}
