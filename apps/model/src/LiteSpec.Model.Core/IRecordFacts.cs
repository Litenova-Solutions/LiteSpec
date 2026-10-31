using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     The facts every check host reads from a record: its identity, its place,
///     and its fields.
/// </summary>
public interface IRecordFacts
{
    /// <summary>Gets the record type, kebab-case.</summary>
    string Type { get; }

    /// <summary>Gets the identifier.</summary>
    string Id { get; }

    /// <summary>Gets the display title.</summary>
    string Title { get; }

    /// <summary>Gets the resolved status: the entry's own, else its page's.</summary>
    string? Status { get; }

    /// <summary>Gets the absolute file path.</summary>
    string File { get; }

    /// <summary>Gets the 1-based line.</summary>
    int Line { get; }

    /// <summary>Reads one optional string field.</summary>
    /// <param name="name">The field name.</param>
    /// <returns>The field text, or null when the record holds no such string field.</returns>
    string? Field(string name);

    /// <summary>Reads one field that holds one id or an array of ids.</summary>
    /// <param name="name">The field name.</param>
    /// <returns>The ids the field holds, in field order; empty when it holds none.</returns>
    IReadOnlyList<string> Ids(string name);
}
