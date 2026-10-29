using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     One record read from a page file: a page, or an entry on a page.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
public sealed class IndexRecord : IRecordFacts
{
    private readonly JsonText raw;

    /// <summary>Builds the record from its JSON object.</summary>
    /// <param name="raw">The record's JSON object.</param>
    /// <param name="file">The absolute file path.</param>
    /// <param name="line">The 1-based line.</param>
    /// <param name="isEntry">Whether the record is an entry rather than a page.</param>
    /// <param name="pageId">The carrying page's record id, for an entry.</param>
    /// <param name="inheritedOwner">The page's owner, for an entry carrying none.</param>
    /// <param name="inheritedStatus">The page's status, for an entry carrying none.</param>
    public IndexRecord(
        JsonText raw,
        string file,
        int line,
        bool isEntry,
        string? pageId,
        string? inheritedOwner,
        string? inheritedStatus)
    {
        this.raw = raw;
        File = file;
        Line = line;
        IsEntry = isEntry;
        PageId = pageId;
        Type = StringOf(raw, "type") ?? "";
        Id = StringOf(raw, "id") ?? "";
        Title = StringOf(raw, "title") ?? "";
        Owner = StringOf(raw, "owner") ?? inheritedOwner;
        Status = StringOf(raw, "status") ?? inheritedStatus;
    }

    /// <summary>The record type, kebab-case.</summary>
    public string Type { get; }

    /// <summary>The identifier.</summary>
    public string Id { get; }

    /// <summary>The display title.</summary>
    public string Title { get; }

    /// <summary>The resolved owner: the entry's own, else its page's.</summary>
    public string? Owner { get; }

    /// <summary>The resolved status: the entry's own, else its page's.</summary>
    public string? Status { get; }

    /// <summary>The absolute file path.</summary>
    public string File { get; }

    /// <summary>The 1-based line.</summary>
    public int Line { get; }

    /// <summary>Whether the record is an entry rather than a page.</summary>
    public bool IsEntry { get; }

    /// <summary>The carrying page's record id, for an entry.</summary>
    public string? PageId { get; }

    /// <summary>Reads one optional string field.</summary>
    public string? Field(string name) => StringOf(raw, name);

    /// <summary>Reads one field that holds one id or an array of ids.</summary>
    public IReadOnlyList<string> Ids(string name)
    {
        if (!raw.TryGetProperty(name, out var value))
        {
            return Array.Empty<string>();
        }

        if (value.Kind == JsonKind.String && value.GetString() is string single)
        {
            return new string[] { single };
        }

        if (value.Kind == JsonKind.Array)
        {
            var ids = new List<string>();

            foreach (var entry in value.Items)
            {
                if (entry.Kind == JsonKind.String && entry.GetString() is string id)
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        return Array.Empty<string>();
    }

    private static string? StringOf(JsonText node, string name) =>
        node.TryGetProperty(name, out var value) ? value.GetString() : null;
}
