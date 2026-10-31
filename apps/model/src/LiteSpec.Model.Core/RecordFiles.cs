using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     Reads the records one Markdown page yields, for hosts with no CLI.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
public static class RecordFiles
{
    /// <summary>
    ///     Reads the page record and the entries of the page: the frontmatter between the
    ///     two fence lines, with each entry's owner and status inherited from the page.
    ///     A page with no frontmatter, or frontmatter that is no JSON object with a type,
    ///     yields nothing.
    /// </summary>
    /// <param name="path">The absolute file path.</param>
    /// <param name="text">The file's text.</param>
    /// <returns>The page's records, the page first, then its entries in order.</returns>
    public static IReadOnlyList<IndexRecord> Read(string path, string text)
    {
        var records = new List<IndexRecord>();
        var lines = text.Split('\n');

        if (lines.Length < 3 || lines[0].TrimEnd('\r') != "---")
        {
            return records;
        }

        var closing = -1;

        for (var index = 1; index < lines.Length; index++)
        {
            if (lines[index].TrimEnd('\r') == "---")
            {
                closing = index;
                break;
            }
        }

        if (closing < 0)
        {
            return records;
        }

        JsonText root;

        try
        {
            root = JsonText.Parse(string.Join("\n", lines, 1, closing - 1));
        }
        catch (JsonTextException)
        {
            return records;
        }

        if (root.Kind != JsonKind.Object
            || !root.TryGetProperty("type", out var typeValue)
            || typeValue.GetString() is not string type)
        {
            return records;
        }

        string? pageId = null;
        string? pageOwner = null;
        string? pageStatus = null;

        if (RecordTypes.All.Contains(type))
        {
            var record = new IndexRecord(root, path, LineOf(text, "\"type\""), false, null, null, null);

            pageId = record.Id;
            pageOwner = record.Owner;
            pageStatus = record.Status;
            records.Add(record);
        }
        else if (RecordTypes.PageStyles.Contains(type))
        {
            pageOwner = StringOf(root, "owner");
            pageStatus = StringOf(root, "status");
        }

        if (root.TryGetProperty("records", out var entries) && entries.Kind == JsonKind.Array)
        {
            foreach (var entry in entries.Items)
            {
                if (entry.Kind != JsonKind.Object)
                {
                    continue;
                }

                var entryType = StringOf(entry, "type");
                var entryId = StringOf(entry, "id");
                var line = entryId is not null ? LineOf(text, entryId) : LineOf(text, "\"records\"");

                if (entryType is null || !RecordTypes.All.Contains(entryType))
                {
                    continue;
                }

                records.Add(new IndexRecord(entry, path, line, true, pageId, pageOwner, pageStatus));
            }
        }

        return records;
    }

    /// <summary>Reads one optional string field from a JSON object.</summary>
    /// <param name="node">The JSON object.</param>
    /// <param name="name">The field name.</param>
    /// <returns>The field text, or null when the object holds no such string field.</returns>
    private static string? StringOf(JsonText node, string name) =>
        node.TryGetProperty(name, out var value) ? value.GetString() : null;

    /// <summary>The 1-based line where the needle first occurs, or 1.</summary>
    /// <param name="text">The file text.</param>
    /// <param name="needle">The text to locate.</param>
    /// <returns>The 1-based line of the first occurrence, or 1 when it occurs nowhere.</returns>
    private static int LineOf(string text, string needle)
    {
        var at = text.IndexOf(needle, StringComparison.Ordinal);

        if (at < 0)
        {
            return 1;
        }

        var line = 1;

        for (var index = 0; index < at; index++)
        {
            if (text[index] == '\n')
            {
                line++;
            }
        }

        return line;
    }
}
