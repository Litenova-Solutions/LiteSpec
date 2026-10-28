using System;
using System.Collections.Generic;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>A record's facts by hand, for rule tests.</summary>
sealed class FakeRecord : IRecordFacts
{
    private readonly Dictionary<string, string> fields;
    private readonly Dictionary<string, IReadOnlyList<string>> lists;

    /// <summary>Builds the record with the identity, the fields and the id lists.</summary>
    /// <param name="type">The record type, kebab-case.</param>
    /// <param name="id">The identifier.</param>
    /// <param name="status">The resolved status.</param>
    /// <param name="fields">The string fields.</param>
    /// <param name="lists">The id-list fields.</param>
    /// <param name="file">The absolute file path.</param>
    /// <param name="line">The 1-based line.</param>
    public FakeRecord(
        string type,
        string id,
        string? status = "approved",
        Dictionary<string, string>? fields = null,
        Dictionary<string, IReadOnlyList<string>>? lists = null,
        string file = "docs/page.md",
        int line = 1)
    {
        Type = type;
        Id = id;
        Title = id;
        Status = status;
        File = file;
        Line = line;
        this.fields = fields ?? new Dictionary<string, string>(StringComparer.Ordinal);
        this.lists = lists ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
    }

    /// <summary>The record type, kebab-case.</summary>
    public string Type { get; }

    /// <summary>The identifier.</summary>
    public string Id { get; }

    /// <summary>The display title.</summary>
    public string Title { get; }

    /// <summary>The resolved status.</summary>
    public string? Status { get; }

    /// <summary>The absolute file path.</summary>
    public string File { get; }

    /// <summary>The 1-based line.</summary>
    public int Line { get; }

    /// <summary>Reads one optional string field.</summary>
    public string? Field(string name) =>
        fields.TryGetValue(name, out var value) ? value : null;

    /// <summary>Reads one field that holds one id or an array of ids.</summary>
    public IReadOnlyList<string> Ids(string name) =>
        lists.TryGetValue(name, out var value) ? value : Array.Empty<string>();
}
