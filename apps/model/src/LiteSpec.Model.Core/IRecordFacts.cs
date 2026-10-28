using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     The facts every check host reads from a record: its identity, its place,
///     and its fields.
/// </summary>
public interface IRecordFacts
{
    /// <summary>The record type, kebab-case.</summary>
    string Type { get; }

    /// <summary>The identifier.</summary>
    string Id { get; }

    /// <summary>The display title.</summary>
    string Title { get; }

    /// <summary>The resolved status: the entry's own, else its page's.</summary>
    string? Status { get; }

    /// <summary>The absolute file path.</summary>
    string File { get; }

    /// <summary>The 1-based line.</summary>
    int Line { get; }

    /// <summary>Reads one optional string field.</summary>
    string? Field(string name);

    /// <summary>Reads one field that holds one id or an array of ids.</summary>
    IReadOnlyList<string> Ids(string name);
}
