using System.Text.Json;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Records;

/// <summary>
///     One record: a page, an entry on a page, a term, or a derived generic result.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
sealed class Record : IRecordFields
{
    /// <summary>Gets the record type, kebab-case.</summary>
    public string Type { get; init; } = "";

    /// <summary>Gets the identifier.</summary>
    public string Id { get; init; } = "";

    /// <summary>Gets the folder the record file sits in, relative to the consumer root; empty for a derived record.</summary>
    public string Folder { get; init; } = "";

    /// <summary>Gets the module folder the record file sits under, or null when no module folder holds it.</summary>
    public string? Module { get; init; }

    /// <summary>Gets the display title.</summary>
    public string Title { get; init; } = "";

    /// <summary>Gets a value indicating whether whether the builder derived the record rather than reading it from a page.</summary>
    public bool Derived { get; init; }

    /// <summary>Gets the absolute record file path.</summary>
    public string File { get; init; } = "";

    /// <summary>Gets the 1-based line of the record's node in its file.</summary>
    public int Line { get; init; }

    /// <summary>Gets the absolute explanation file path, for a page record with one beside it.</summary>
    public string? ExplanationFile { get; init; }

    /// <summary>Gets the explanation file's full text, for a page record with one beside it.</summary>
    public string? Explanation { get; init; }

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
    ///     Reads the targets of one edge verb (runs, sends, calls): the entries of
    ///     <c>effects</c> carrying the verb, then a top-level field of the same name.
    /// </summary>
    /// <param name="verb">The edge verb to read: runs, sends or calls.</param>
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

    /// <summary>
    ///     The requirements the entry point's served content fulfills: the fulfills of each
    ///     served-content entry, in entry order. Only an entry point serves content; the
    ///     shape check owns which kind may carry it.
    /// </summary>
    /// <returns>The fulfilled requirement identifiers, or an empty list when the record serves no content.</returns>
    public IReadOnlyList<string> ServedFulfills()
    {
        if (Type != RecordTypes.EntryPoint)
        {
            return [];
        }

        var ids = new List<string>();

        foreach (var entry in Objects(RecordFields.ServedContent))
        {
            if (!entry.TryGetProperty(RecordFields.Fulfills, out var fulfills))
            {
                continue;
            }

            if (fulfills.ValueKind == JsonValueKind.String && fulfills.GetString() is { } single)
            {
                ids.Add(single);
            }
            else if (fulfills.ValueKind == JsonValueKind.Array)
            {
                ids.AddRange(fulfills.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString()!));
            }
        }

        return ids;
    }

    /// <summary>The flow's step targets: flow/name#step for each step carrying an id.</summary>
    /// <returns>The step targets of the flow, or an empty list for any other record type.</returns>
    public IReadOnlyList<string> StepIds()
    {
        if (Type != RecordTypes.Flow
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

    /// <summary>
    ///     The requirement identifiers a flow carries: the union of its steps' <c>cites</c>, in step
    ///     order with each identifier once. A flow holds no citations of its own, so this is its
    ///     computed citation list.
    /// </summary>
    /// <returns>The cited requirement identifiers of the flow, or an empty list for any other record type.</returns>
    public IReadOnlyList<string> StepCitations()
    {
        if (Type != RecordTypes.Flow
            || Raw.ValueKind != JsonValueKind.Object
            || !Raw.TryGetProperty(RecordFields.Steps, out var steps)
            || steps.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var ids = new List<string>();

        foreach (var step in steps.EnumerateArray())
        {
            if (step.ValueKind != JsonValueKind.Object || !step.TryGetProperty(RecordFields.Cites, out var cites))
            {
                continue;
            }

            var citedIds = cites.ValueKind == JsonValueKind.Array
                ? cites.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.String).Select(entry => entry.GetString()!)
                : Enumerable.Empty<string>();

            foreach (var cited in citedIds)
            {
                if (!ids.Contains(cited, StringComparer.Ordinal))
                {
                    ids.Add(cited);
                }
            }
        }

        return ids;
    }

    /// <summary>The object entries of an array field, such as a reaction's outcomes or an aggregate's transitions.</summary>
    /// <param name="name">The field name to read.</param>
    /// <returns>Each object entry of the array, in order; empty when the field is not an array.</returns>
    public IReadOnlyList<JsonElement> Objects(string name)
    {
        if (Raw.ValueKind != JsonValueKind.Object
            || !Raw.TryGetProperty(name, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object).ToList();
    }

    /// <summary>Reads a boolean field, such as a setting's <c>dated</c> or a flag a use case states.</summary>
    /// <param name="name">The field name to read.</param>
    /// <returns>True when the field is the boolean true; otherwise false.</returns>
    public bool Flag(string name) =>
        Raw.ValueKind == JsonValueKind.Object
        && Raw.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.True;

    /// <summary>
    ///     The names a reaction or job declares as its outcomes: the <c>name</c> of each entry of its
    ///     <c>outcomes</c>, in order. One of completed, skipped, suppressed, failed, retry-scheduled or nothing-selected.
    /// </summary>
    /// <returns>The outcome names, or an empty list when the record declares none.</returns>
    public IReadOnlyList<string> OutcomeNames() =>
        Objects(RecordFields.Outcomes)
            .Select(entry => entry.TryGetProperty(RecordFields.Name, out var name) && name.ValueKind == JsonValueKind.String ? name.GetString() ?? "" : "")
            .Where(name => name.Length > 0)
            .ToList();

    /// <summary>
    ///     The transitions an aggregate declares, each with its source and target state, and the job that moves it
    ///     (<c>movedBy</c>) and the duration after entering the source state (<c>after</c>) when it is timed.
    /// </summary>
    /// <returns>The transitions in declaration order; empty for any other record type or when none are declared.</returns>
    public IReadOnlyList<TransitionFact> Transitions() =>
        Objects(RecordFields.Transitions)
            .Select(entry => new TransitionFact(
                TextOf(entry, RecordFields.From),
                TextOf(entry, RecordFields.To),
                TextOf(entry, RecordFields.MovedBy),
                TextOf(entry, RecordFields.After)))
            .ToList();

    /// <summary>
    ///     The aggregate data classes an external system processes (<c>processes</c>): each aggregate with the
    ///     data class of that aggregate the system holds.
    /// </summary>
    /// <returns>The processed aggregate and data class pairs, in declaration order.</returns>
    public IReadOnlyList<ProcessFact> Processes() =>
        Objects(RecordFields.Processes)
            .Select(entry => new ProcessFact(
                TextOf(entry, RecordFields.Aggregate),
                TextOf(entry, RecordFields.DataClass)))
            .ToList();

    /// <summary>Reads one string member of a JSON object, or an empty string when it is missing or not a string.</summary>
    /// <param name="entry">The object to read.</param>
    /// <param name="name">The member name.</param>
    /// <returns>The string value, or an empty string.</returns>
    private static string TextOf(JsonElement entry, string name) =>
        entry.ValueKind == JsonValueKind.Object
        && entry.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    /// <summary>Whether the record carries the field.</summary>
    /// <param name="name">The field name to test for.</param>
    /// <returns>True when the record carries a field of that name.</returns>
    public bool Has(string name) =>
        Raw.ValueKind == JsonValueKind.Object && Raw.TryGetProperty(name, out _);
}
