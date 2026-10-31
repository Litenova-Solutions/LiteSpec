using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     Reads the records one record file yields, for hosts with no CLI.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
public static class RecordFiles
{
    /// <summary>
    ///     Reads the page record and the entries of the record file: the YAML mapping,
    ///     with each entry's owner and status inherited from the page. A file that does
    ///     not parse, or whose top level is no object with a type, yields nothing.
    /// </summary>
    /// <param name="path">The absolute file path.</param>
    /// <param name="text">The file's text.</param>
    /// <returns>The page's records, the page first, then its entries in order.</returns>
    public static IReadOnlyList<IndexRecord> Read(string path, string text)
    {
        var records = new List<IndexRecord>();

        JsonText root;

        try
        {
            root = YamlText.Parse(text);
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
            var record = new IndexRecord(root, path, root.Line, false, null, null, null);

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

                if (entryType is null || !RecordTypes.All.Contains(entryType))
                {
                    continue;
                }

                records.Add(new IndexRecord(entry, path, entry.Line, true, pageId, pageOwner, pageStatus));
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
}
