using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     Reads the records one record file yields. The CLI and the analyzers both read
///     records here, so one rule decides what a record is.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
public static class RecordFiles
{
    /// <summary>The record file extension.</summary>
    public const string Extension = ".yaml";

    /// <summary>The entry lists of a cast page, each with the entry type it holds.</summary>
    public static readonly IReadOnlyList<(string Field, string EntryType)> CastLists =
    [
        (RecordFields.Parties, RecordTypes.Party),
        (RecordFields.People, RecordTypes.Person),
        (RecordFields.Occasions, RecordTypes.Occasion),
        (RecordFields.Figures, RecordTypes.Figure),
    ];

    /// <summary>
    ///     The typed child arrays of each container page type, with the entry type each list
    ///     defaults to: a use case holds results and scenarios, a reaction or job holds scenarios,
    ///     a permission set holds permissions, and an entry point set holds entry points. An entry
    ///     states no type when it is the type its list holds.
    /// </summary>
    public static readonly IReadOnlyList<(string PageType, string Field, string EntryType)> ChildLists =
    [
        (RecordTypes.UseCase, RecordFields.Results, RecordTypes.Result),
        (RecordTypes.UseCase, RecordFields.Scenarios, RecordTypes.Scenario),
        (RecordTypes.Reaction, RecordFields.Scenarios, RecordTypes.Scenario),
        (RecordTypes.Job, RecordFields.Scenarios, RecordTypes.Scenario),
        (RecordTypes.PermissionSet, RecordFields.Permissions, RecordTypes.Permission),
        (RecordTypes.EntryPointSet, RecordFields.EntryPoints, RecordTypes.EntryPoint),
    ];

    /// <summary>Whether the path names a record file: a <c>.yaml</c> file, in any letter case.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>True for a <c>.yaml</c> file; otherwise false.</returns>
    public static bool IsRecordFile(string path) => path.EndsWith(Extension, StringComparison.OrdinalIgnoreCase);

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
                $"Record file '{path}' could not be read: {exception.Message}{YamlText.ColonHint(text, exception.Line) ?? string.Empty}",
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

        // A container page holds its entries in typed child arrays: each list defaults its
        // entries to the type the list holds, and a stated type must name that type. A refusal
        // set lists codes, not records: its entries are read by the refusal reader.
        if (root.Kind == JsonKind.Object && type is not null)
        {
            foreach (var (pageType, field, entryType) in ChildLists)
            {
                if (type != pageType
                    || !root.TryGetProperty(field, out var entries)
                    || entries.Kind != JsonKind.Array)
                {
                    continue;
                }

                foreach (var entry in entries.Items)
                {
                    if (entry.Kind != JsonKind.Object)
                    {
                        continue;
                    }

                    var stated = StringOf(entry, RecordFields.Type);

                    if (stated is not null && stated != entryType)
                    {
                        problems.Add(new Finding(
                            "record-invalid",
                            $"Entry in '{field}' states type '{stated}'; a '{field}' entry is a '{entryType}'.",
                            path,
                            entry.Line,
                            pageId));
                        continue;
                    }

                    records.Add(new IndexRecord(entry, path, entry.Line, true, pageId, entryType));
                }
            }
        }

        // A cast holds its entries in four named lists, one per entry type. An entry that
        // states another type is left to the schema, which names the list it breaks.
        if (root.Kind == JsonKind.Object && type == RecordTypes.Cast)
        {
            foreach (var (field, entryType) in CastLists)
            {
                if (!root.TryGetProperty(field, out var list) || list.Kind != JsonKind.Array)
                {
                    continue;
                }

                foreach (var entry in list.Items)
                {
                    if (entry.Kind != JsonKind.Object || StringOf(entry, RecordFields.Type) != entryType)
                    {
                        continue;
                    }

                    // A party or person is titled by its name; an occasion or figure by its title.
                    var title = StringOf(entry, RecordFields.Title) ?? StringOf(entry, RecordFields.Name);

                    records.Add(new IndexRecord(entry, path, entry.Line, true, pageId, null, title));
                }
            }
        }

        // A flow step is an entry of its flow's page, like a result or a scenario
        // entry: it states no type, so it reads as a step, and its title names
        // its act.
        if (root.Kind == JsonKind.Object
            && type == RecordTypes.Flow
            && root.TryGetProperty(RecordFields.Steps, out var steps)
            && steps.Kind == JsonKind.Array)
        {
            foreach (var step in steps.Items)
            {
                if (step.Kind != JsonKind.Object)
                {
                    continue;
                }

                records.Add(new IndexRecord(
                    step,
                    path,
                    step.Line,
                    true,
                    pageId,
                    RecordTypes.Step,
                    StringOf(step, RecordFields.Title)));
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
    /// <param name="Records">The page record first when the top level is a record type, then the entries that are record types, then each step of a flow.</param>
    /// <param name="Problems">The problems the read found, in file order.</param>
    public sealed record Scanned(JsonText? Root, IReadOnlyList<IndexRecord> Records, IReadOnlyList<Finding> Problems);
}
