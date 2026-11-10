using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     Reads the records one record file yields. The CLI and the analyzers both read
///     records here, so one rule decides what a record is.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
public static class RecordFiles
{
    /// <summary>
    ///     Reads the page record and the entries of the record file: the YAML mapping.
    ///     A file that does not parse, or whose top level is no object with a type,
    ///     yields nothing.
    /// </summary>
    /// <param name="path">The absolute file path.</param>
    /// <param name="text">The file's text.</param>
    /// <returns>The page's records, the page first, then its entries in order.</returns>
    public static IReadOnlyList<IndexRecord> Read(string path, string text) => Scan(path, text).Records;

    /// <summary>
    ///     Reads the record file and reports what is wrong with it: a file that does not
    ///     parse, a top level that names no record type or container style, and an entry
    ///     whose type is no record type.
    /// </summary>
    /// <param name="path">The absolute file path.</param>
    /// <param name="text">The file's text.</param>
    /// <returns>The parsed tree, the records it yields and the problems found.</returns>
    public static Scanned Scan(string path, string text)
    {
        var records = new List<IndexRecord>();
        var problems = new List<Finding>();

        JsonText root;

        try
        {
            root = YamlText.Parse(text);
        }
        catch (JsonTextException exception)
        {
            problems.Add(new Finding(
                "record-invalid",
                $"Record file '{path}' could not be read: {exception.Message}",
                path,
                exception.Line,
                null));

            return new Scanned(null, records, problems);
        }

        var type = root.Kind == JsonKind.Object ? StringOf(root, "type") : null;

        if (type is null)
        {
            problems.Add(new Finding(
                "record-invalid",
                $"Record file '{path}' holds no record: its top level names no record type.",
                path,
                1,
                null));
        }
        else if (!RecordTypes.All.Contains(type) && !RecordTypes.ContainerStyles.Contains(type))
        {
            problems.Add(new Finding(
                "record-invalid",
                $"Record file '{path}' holds no record: type '{type}' is neither a record type nor a container page style.",
                path,
                1,
                null));
        }

        string? pageId = null;

        if (type is not null && RecordTypes.All.Contains(type))
        {
            var record = new IndexRecord(root, path, root.Line, false, null);

            pageId = record.Id;
            records.Add(record);
        }

        if (root.Kind == JsonKind.Object
            && root.TryGetProperty(RecordFields.Records, out var entries)
            && entries.Kind == JsonKind.Array)
        {
            foreach (var entry in entries.Items)
            {
                if (entry.Kind != JsonKind.Object)
                {
                    continue;
                }

                var entryType = StringOf(entry, RecordFields.Type);

                if (entryType is null || !RecordTypes.All.Contains(entryType))
                {
                    problems.Add(new Finding(
                        "field-invalid",
                        $"Entry type '{entryType ?? "missing"}' is not a record type.",
                        path,
                        entry.Line,
                        pageId));
                    continue;
                }

                records.Add(new IndexRecord(entry, path, entry.Line, true, pageId));
            }
        }

        return new Scanned(root, records, problems);
    }

    /// <summary>Reads one optional string field from a JSON object.</summary>
    /// <param name="node">The JSON object.</param>
    /// <param name="name">The field name.</param>
    /// <returns>The field text, or null when the object holds no such string field.</returns>
    private static string? StringOf(JsonText node, string name) =>
        node.TryGetProperty(name, out var value) ? value.GetString() : null;

    /// <summary>
    ///     What one record file read found.
    /// </summary>
    /// <param name="Root">The parsed top level, or null when the text is not YAML.</param>
    /// <param name="Records">The page record first when the top level is a record type, then the entries that are record types.</param>
    /// <param name="Problems">The problems the read found, in file order.</param>
    public sealed record Scanned(JsonText? Root, IReadOnlyList<IndexRecord> Records, IReadOnlyList<Finding> Problems);
}
