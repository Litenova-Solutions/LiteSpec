using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     The records a host reads from page files, with lookup by id and by type.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
public sealed class RecordIndex
{
    /// <summary>Every record, in read order.</summary>
    private readonly IReadOnlyList<IRecordFacts> records;
    /// <summary>The first record carrying each id.</summary>
    private readonly Dictionary<string, IRecordFacts> byId;
    /// <summary>The first use case carrying each name.</summary>
    private readonly Dictionary<string, IRecordFacts> useCaseByName;

    /// <summary>Initializes a new instance of the <see cref="RecordIndex"/> class.Builds the index over the records and computes their effective status.</summary>
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

    /// <summary>Gets every record, in read order.</summary>
    public IReadOnlyList<IRecordFacts> Records => records;

    /// <summary>Gets the effective status by record id.</summary>
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
    /// <param name="record">The result or scenario record.</param>
    /// <returns>The holding use case, or null when no use case holds it.</returns>
    private IRecordFacts? Holder(IRecordFacts record)
    {
        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            return null;
        }

        return useCaseByName.TryGetValue(home, out var holder) ? holder : null;
    }
}
