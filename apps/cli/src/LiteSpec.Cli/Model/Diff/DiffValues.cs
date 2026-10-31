using System.Text.Json;

namespace LiteSpec.Model.Diff;

/// <summary>
///     The field values the differ compares: one raw field of a record, the JSON
///     equality of two values, and the generic field walk every comparator ends in.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
/// <follows>litespec/core/release#review-behavior-changes-through-the-behavior-diff</follows>
static class DiffValues
{
    /// <summary>
    ///     Fields that carry no intent: identity, ownership, review planning and the
    ///     entries a page carries, which diff as records of their own.
    /// </summary>
    internal static readonly HashSet<string> MetaFields = new(StringComparer.Ordinal)
    {
        "type", "id", "title", "owner", "status", "decider", "decideBy",
        "terms", "questions", "records",
    };

    /// <summary>Compares every field neither the caller handled nor metadata owns.</summary>
    /// <param name="before">The base record.</param>
    /// <param name="after">The head record.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    /// <param name="handled">The fields the caller already compared, or null for none.</param>
    public static void CompareFields(
        Record before, Record after, List<string> details, HashSet<string>? handled = null)
    {
        if (before.Raw.ValueKind != JsonValueKind.Object || after.Raw.ValueKind != JsonValueKind.Object)
        {
            if (!JsonEquals(before.Raw, after.Raw))
            {
                details.Add("Fields changed.");
            }

            return;
        }

        var names = before.Raw.EnumerateObject().Select(property => property.Name)
            .Concat(after.Raw.EnumerateObject().Select(property => property.Name))
            .Where(name => !MetaFields.Contains(name) && !(handled?.Contains(name) ?? false))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal);

        foreach (var name in names)
        {
            CompareValue(Label(before.Type, name), Field(before, name), Field(after, name), details);
        }
    }

    /// <summary>The display label of one field of one record type.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="name">The field name.</param>
    /// <returns>The display label of the field.</returns>
    public static string Label(string type, string name) => (type, name) switch
    {
        (RecordTypes.Requirement, RecordFields.DueBy) => "Due by",
        _ => name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name[1..],
    };

    /// <summary>Compares one field, adding a detail line when it changed.</summary>
    /// <param name="label">The field's display label.</param>
    /// <param name="before">The base value, or null when the field is absent.</param>
    /// <param name="after">The head value, or null when the field is absent.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    /// <returns>Whether the field changed.</returns>
    public static bool CompareValue(string label, JsonElement? before, JsonElement? after, List<string> details)
    {
        if (before is null && after is null)
        {
            return false;
        }

        if (before is null)
        {
            details.Add(after!.Value.ValueKind == JsonValueKind.String
                ? $"{label} added: \"{after.Value.GetString()}\"."
                : $"{label} added.");
            return true;
        }

        if (after is null)
        {
            details.Add($"{label} removed.");
            return true;
        }

        if (JsonEquals(before.Value, after.Value))
        {
            return false;
        }

        if (before.Value.ValueKind == JsonValueKind.String && after.Value.ValueKind == JsonValueKind.String)
        {
            details.Add($"{label} changed: \"{after.Value.GetString()}\" (was: \"{before.Value.GetString()}\").");
            return true;
        }

        if (before.Value.ValueKind == JsonValueKind.True || before.Value.ValueKind == JsonValueKind.False)
        {
            if (after.Value.ValueKind == JsonValueKind.True || after.Value.ValueKind == JsonValueKind.False)
            {
                details.Add($"{label} changed from {Flat(before.Value)} to {Flat(after.Value)}.");
                return true;
            }
        }

        if (before.Value.ValueKind == JsonValueKind.Number && after.Value.ValueKind == JsonValueKind.Number)
        {
            details.Add($"{label} changed from {before.Value.GetRawText()} to {after.Value.GetRawText()}.");
            return true;
        }

        if (before.Value.ValueKind == JsonValueKind.Array && after.Value.ValueKind == JsonValueKind.Array
            && before.Value.EnumerateArray().All(entry => entry.ValueKind == JsonValueKind.String)
            && after.Value.EnumerateArray().All(entry => entry.ValueKind == JsonValueKind.String))
        {
            var oldItems = before.Value.EnumerateArray().Select(entry => entry.GetString()!)
                .ToHashSet(StringComparer.Ordinal);
            var newItems = after.Value.EnumerateArray().Select(entry => entry.GetString()!)
                .ToHashSet(StringComparer.Ordinal);
            var parts = newItems.Except(oldItems).OrderBy(item => item, StringComparer.Ordinal)
                    .Select(item => $"+{item}")
                .Concat(oldItems.Except(newItems).OrderBy(item => item, StringComparer.Ordinal)
                    .Select(item => $"-{item}"))
                .ToList();

            if (parts.Count == 0)
            {
                return false;
            }

            details.Add($"{label} changed: {string.Join(", ", parts)}.");
            return true;
        }

        details.Add($"{label} changed.");
        return true;
    }

    /// <summary>One JSON value as flat text for a changed line.</summary>
    /// <param name="value">The JSON value.</param>
    /// <returns>The value as flat text.</returns>
    public static string Flat(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => "none",
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.String => $"\"{value.GetString()}\"",
        _ => value.ValueKind.ToString().ToLowerInvariant(),
    };

    /// <summary>One raw field of a record, or null when the record carries no such field.</summary>
    /// <param name="record">The record.</param>
    /// <param name="name">The field name.</param>
    /// <returns>The raw field, or null when the record carries no such field.</returns>
    public static JsonElement? Field(Record record, string name) =>
        record.Raw.ValueKind == JsonValueKind.Object
        && record.Raw.TryGetProperty(name, out var value)
            ? value
            : null;

    /// <summary>Whether two optional JSON values are equal.</summary>
    /// <param name="left">The first value, or null when absent.</param>
    /// <param name="right">The second value, or null when absent.</param>
    /// <returns>True when the two values are equal.</returns>
    public static bool JsonEquals(JsonElement? left, JsonElement? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        return JsonEquals(left.Value, right.Value);
    }

    /// <summary>Whether two JSON values are equal.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>True when the two values are equal.</returns>
    public static bool JsonEquals(JsonElement left, JsonElement right) =>
        string.Equals(CanonicalJson.Canonical(left), CanonicalJson.Canonical(right), StringComparison.Ordinal);
}
