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
    private readonly IReadOnlyList<IRecordFields> records;
    /// <summary>The first record carrying each id.</summary>
    private readonly Dictionary<string, IRecordFields> byId;

    /// <summary>Initializes a new instance of the <see cref="RecordIndex"/> class.Builds the index over the records.</summary>
    /// <param name="records">Every record, in read order.</param>
    public RecordIndex(IReadOnlyList<IRecordFields> records)
    {
        this.records = records;
        byId = new Dictionary<string, IRecordFields>(StringComparer.Ordinal);

        foreach (var record in records)
        {
            if (record.Id.Length > 0 && !byId.ContainsKey(record.Id))
            {
                byId.Add(record.Id, record);
            }
        }
    }

    /// <summary>Gets every record, in read order.</summary>
    public IReadOnlyList<IRecordFields> Records => records;

    /// <summary>Reads the record with the id, or null when none carries it.</summary>
    /// <param name="id">The identifier.</param>
    /// <returns>The record, or null.</returns>
    public IRecordFields? ById(string id) =>
        byId.TryGetValue(id, out var record) ? record : null;

    /// <summary>Reads every record of the type, in read order.</summary>
    /// <param name="type">The record type, kebab-case.</param>
    /// <returns>The type's records.</returns>
    public IReadOnlyList<IRecordFields> ByType(string type)
    {
        var found = new List<IRecordFields>();

        foreach (var record in records)
        {
            if (record.Type == type)
            {
                found.Add(record);
            }
        }

        return found;
    }
}
