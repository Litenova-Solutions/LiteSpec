using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     The records a host reads from page files, with lookup by id and by type.
/// </summary>
public sealed class RecordIndex
{
    private readonly IReadOnlyList<IRecordFacts> records;
    private readonly Dictionary<string, IRecordFacts> byId;
    private readonly Dictionary<string, IRecordFacts> useCaseByName;

    /// <summary>Builds the index over the records and computes their effective status.</summary>
    /// <param name="records">Every record, in read order.</param>
    public RecordIndex(IReadOnlyList<IRecordFacts> records)
    {
        this.records = records;
        byId = new Dictionary<string, IRecordFacts>(StringComparer.Ordinal);
        useCaseByName = new Dictionary<string, IRecordFacts>(StringComparer.Ordinal);

        foreach (var record in records)
        {
            if (record.Id.Length > 0 && !byId.ContainsKey(record.Id))
            {
                byId.Add(record.Id, record);
            }

            if (record.Type == "use-case"
                && Identifiers.TrySplit(record.Id, out _, out _, out var name)
                && name.Length > 0
                && !useCaseByName.ContainsKey(name))
            {
                useCaseByName.Add(name, record);
            }
        }

        EffectiveStatus = Authority.EffectiveStatus(records, Holder);
    }

    /// <summary>Every record, in read order.</summary>
    public IReadOnlyList<IRecordFacts> Records => records;

    /// <summary>The effective status by record id.</summary>
    public IReadOnlyDictionary<string, string> EffectiveStatus { get; }

    /// <summary>Reads the record with the id, or null when none carries it.</summary>
    /// <param name="id">The identifier.</param>
    /// <returns>The record, or null.</returns>
    public IRecordFacts? ById(string id) =>
        byId.TryGetValue(id, out var record) ? record : null;

    /// <summary>Reads every record of the type, in read order.</summary>
    /// <param name="type">The record type, kebab-case.</param>
    /// <returns>The type's records.</returns>
    public IReadOnlyList<IRecordFacts> ByType(string type)
    {
        var found = new List<IRecordFacts>();

        foreach (var record in records)
        {
            if (record.Type == type)
            {
                found.Add(record);
            }
        }

        return found;
    }

    /// <summary>
    ///     The use case holding a result or scenario: the use case its home names.
    /// </summary>
    private IRecordFacts? Holder(IRecordFacts record)
    {
        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            return null;
        }

        return useCaseByName.TryGetValue(home, out var holder) ? holder : null;
    }
}
