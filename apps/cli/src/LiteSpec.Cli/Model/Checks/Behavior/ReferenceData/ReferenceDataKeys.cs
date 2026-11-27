using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior.ReferenceData;

/// <summary>The reference-data key checks: every row key stays in kebab-case, every row role stays in kebab-case, and every cross-reference names a row key its table holds.</summary>
/// <follows>litespec/core/knowledge#validate-reference-data-before-serving</follows>
internal static class ReferenceDataKeys
{
    /// <summary>Gets the name of the `refersTo` field.</summary>
    const string RefersTo = "refersTo";

    /// <summary>A kebab-case row key or role: a lowercase letter or digit opening, then lowercase letters, digits or hyphens.</summary>
    private static readonly Regex KebabName = new("^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>A reference-data table identifier: the base a row reference resolves through.</summary>
    private static readonly Regex TableId = new("^reference-data/[a-z0-9][a-z0-9-]*\\.[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>The rows of a reference-data table: the object entries of its rows array.</summary>
    /// <param name="table">The reference-data record whose rows are read.</param>
    /// <returns>The row objects in written order, or an empty list when the table holds no rows array.</returns>
    internal static IReadOnlyList<JsonElement> RowsOf(Record table) => table.Objects(RecordFields.Rows);

    /// <summary>Reports a reference-data row whose cross-reference names a key no row of the named table holds.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunKeyResolves(ProjectModel model, List<Finding> findings)
    {
        var keys = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var table in model.Records.Where(record => record.Type == RecordTypes.ReferenceData))
        {
            keys.TryAdd(
                table.Id,
                RowsOf(table)
                    .Select(row => Str(row, RecordFields.Key))
                    .OfType<string>()
                    .ToList());
        }

        foreach (var table in model.Records
            .Where(record => record.Type == RecordTypes.ReferenceData)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            CheckRowKeys(table, findings);

            var index = 0;

            foreach (var row in RowsOf(table))
            {
                if (Str(row, RecordFields.Role) is { } role && !KebabName.IsMatch(role))
                {
                    findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds row {index} with role '{role}' outside lowercase kebab-case.", table.File, table.Line, SubjectOf(table)));
                }

                if (Str(row, RefersTo) is { } target)
                {
                    CheckRefersTo(model, keys, table, target, index, findings);
                }

                index++;
            }
        }
    }

    /// <summary>Reports a row key outside kebab-case. One key repeats across disjoint spans to hold the dated history; the period check reports the spans of one key that overlap.</summary>
    /// <param name="table">The reference-data record whose row keys are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckRowKeys(Record table, List<Finding> findings)
    {
        var index = 0;

        foreach (var row in RowsOf(table))
        {
            if (Str(row, RecordFields.Key) is { } key && !KebabName.IsMatch(key))
            {
                findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds row {index} with key '{key}' outside lowercase kebab-case.", table.File, table.Line, SubjectOf(table)));
            }

            index++;
        }
    }

    /// <summary>Reports a cross-reference that names no reference-data row: a malformed target, or a key no row of the named table holds.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record.</param>
    /// <param name="keys">The row keys of each reference-data table, by table id.</param>
    /// <param name="table">The reference-data record holding the referring row.</param>
    /// <param name="target">The cross-reference as written.</param>
    /// <param name="index">The referring row index.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckRefersTo(ProjectModel model, Dictionary<string, List<string>> keys, Record table, string target, int index, List<Finding> findings)
    {
        var hash = target.IndexOf('#');
        var wellFormed = hash > 0 && hash < target.Length - 1
            && TableId.IsMatch(target[..hash])
            && KebabName.IsMatch(target[(hash + 1)..]);

        if (!wellFormed)
        {
            // A citation-shaped value that resolves nowhere belongs to the citation check, so one defect draws one finding.
            foreach (Match match in Identifiers.Citation.Matches(target))
            {
                if (!model.ById.ContainsKey(Identifiers.TrimCitation(match.Value)))
                {
                    return;
                }
            }

            findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds row {index} referring to '{target}', which names no reference-data row; write 'reference-data/<module>.<table>#<key>'.", table.File, table.Line, SubjectOf(table)));

            return;
        }

        var baseId = target[..hash];
        var key = target[(hash + 1)..];

        if (!keys.TryGetValue(baseId, out var held))
        {
            return;
        }

        if (!held.Contains(key, StringComparer.Ordinal))
        {
            findings.Add(new Finding("reference-data-key-unknown", $"Reference data '{table.Id}' holds row {index} referring to '{target}', which names a key no row of '{baseId}' holds.", table.File, table.Line, SubjectOf(table)));
        }
    }
}
