using System.Text.Json;

namespace LiteSpec.Model;

/// <summary>
///     One record: a page, an entry on a page, a term, or a derived generic result.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
sealed class Record : IRecordFacts
{
    /// <summary>Gets the record type, kebab-case.</summary>
    public string Type { get; init; } = "";

    /// <summary>Gets the identifier.</summary>
    public string Id { get; init; } = "";

    /// <summary>Gets the display title.</summary>
    public string Title { get; init; } = "";

    /// <summary>Gets the resolved owner: the entry's own, else its page's.</summary>
    public string? Owner { get; init; }

    /// <summary>Gets the resolved status: the entry's own, else its page's.</summary>
    public string? Status { get; init; }

    /// <summary>Gets the resolved decider.</summary>
    public string? Decider { get; init; }

    /// <summary>Gets the resolved decide-by date.</summary>
    public string? DecideBy { get; init; }

    /// <summary>Gets who approved or reviewed the record: an owner name, or auto (veto window).</summary>
    public string? ApprovedBy { get; init; }

    /// <summary>Gets the date the record was approved or reviewed.</summary>
    public string? ApprovedOn { get; init; }

    /// <summary>Gets the semantic hash the approval covers, written by the builder.</summary>
    public string? ApprovedHash { get; init; }

    /// <summary>Gets a value indicating whether whether the builder derived the record rather than reading it from a page.</summary>
    public bool Derived { get; init; }

    /// <summary>Gets the absolute file path.</summary>
    public string File { get; init; } = "";

    /// <summary>Gets the 1-based line.</summary>
    public int Line { get; init; }

    /// <summary>Gets a value indicating whether whether the record is an entry rather than a page.</summary>
    public bool IsEntry { get; init; }

    /// <summary>Gets the carrying page's record id, for an entry.</summary>
    public string? PageId { get; init; }

    /// <summary>Gets the raw fields as written.</summary>
    public JsonElement Raw { get; init; }

    /// <summary>Reads one optional string field.</summary>
    /// <param name="name">The field name to read.</param>
    /// <returns>The field string value, or null when the field is missing or not a string.</returns>
    public string? Field(string name) =>
        Raw.ValueKind == JsonValueKind.Object
        && Raw.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>Reads one string array field.</summary>
    /// <param name="name">The field name to read.</param>
    /// <returns>The string entries of the array field, or an empty list when the field is missing or not an array.</returns>
    public IReadOnlyList<string> Strings(string name)
    {
        if (Raw.ValueKind != JsonValueKind.Object
            || !Raw.TryGetProperty(name, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return
        [
            .. value.EnumerateArray()
                      .Where(entry => entry.ValueKind == JsonValueKind.String)
                      .Select(entry => entry.GetString()!),
        ];
    }

    /// <summary>
    ///     Reads the targets of one edge verb (runs, sends, emits, calls): the entries of
    ///     <c>effects</c> carrying the verb, then a top-level field of the same name.
    /// </summary>
    /// <param name="verb">The edge verb to read: runs, sends, emits or calls.</param>
    /// <returns>The target ids the verb names, from effects entries then a same-named field.</returns>
    public IReadOnlyList<string> EdgeIds(string verb)
    {
        var ids = new List<string>();

        if (Raw.ValueKind == JsonValueKind.Object
            && Raw.TryGetProperty(RecordFields.Effects, out var effects)
            && effects.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in effects.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.Object
                    && entry.TryGetProperty(verb, out var target)
                    && target.ValueKind == JsonValueKind.String
                    && target.GetString() is { Length: > 0 } id)
                {
                    ids.Add(id);
                }
            }
        }

        ids.AddRange(Ids(verb));

        return ids;
    }

    /// <summary>Reads one field that holds one id or an array of ids.</summary>
    /// <param name="name">The field name to read.</param>
    /// <returns>The single id or the array of ids the field holds, or an empty list when it holds neither.</returns>
    public IReadOnlyList<string> Ids(string name)
    {
        if (Raw.ValueKind != JsonValueKind.Object || !Raw.TryGetProperty(name, out var value))
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

    /// <summary>The journey's step targets: journey/name#step for each step carrying an id.</summary>
    /// <returns>The step targets of the journey, or an empty list for any other record type.</returns>
    public IReadOnlyList<string> StepIds()
    {
        if (Type != RecordTypes.Journey
            || Raw.ValueKind != JsonValueKind.Object
            || !Raw.TryGetProperty(RecordFields.Steps, out var steps)
            || steps.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var ids = new List<string>();

        foreach (var step in steps.EnumerateArray())
        {
            if (step.ValueKind == JsonValueKind.Object
                && step.TryGetProperty(RecordFields.Id, out var id)
                && id.ValueKind == JsonValueKind.String
                && id.GetString() is { Length: > 0 } stepId)
            {
                ids.Add($"{Id}#{stepId}");
            }
        }

        return ids;
    }

    /// <summary>Whether the record carries the field.</summary>
    /// <param name="name">The field name to test for.</param>
    /// <returns>True when the record carries a field of that name.</returns>
    public bool Has(string name) =>
        Raw.ValueKind == JsonValueKind.Object && Raw.TryGetProperty(name, out _);
}
