using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;
using static LiteSpec.Cli.Model.Checks.Behavior.ReferenceData.ReferenceDataKeys;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The reference-data checks: every row value stays within its closed set, every cross-reference names a row key its table holds, every required role is played by a row, the rows leave no date uncovered, each overlap finding names the shared range, and the use cases computing one value agree on its sign.</summary>
/// <follows>litespec/core/domain-model#state-what-an-empty-table-will-hold</follows>
/// <follows>litespec/core/domain-model#close-the-value-set-of-every-meaning-column</follows>
/// <follows>litespec/core/knowledge#name-a-role-not-a-row</follows>
/// <follows>litespec/core/knowledge#validate-reference-data-before-serving</follows>
/// <follows>litespec/core/domain-model#cover-every-date-in-use</follows>
/// <follows>litespec/core/domain-model#state-the-sign-of-every-amount</follows>
internal static class ReferenceDataChecks
{
    /// <summary>Reports a reference-data row whose value is outside the closed set its table lists in choices.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunValueClosedSet(ProjectModel model, List<Finding> findings)
    {
        foreach (var table in model.Records
            .Where(record => record.Type == RecordTypes.ReferenceData)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            if (table.Raw.ValueKind != JsonValueKind.Object
                || !table.Raw.TryGetProperty(RecordFields.Choices, out var choices))
            {
                continue;
            }

            if (choices.ValueKind != JsonValueKind.Array)
            {
                findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds choices that are not a list.", table.File, table.Line, SubjectOf(table)));
                continue;
            }

            var allowed = choices.EnumerateArray().ToList();

            if (allowed.Count == 0)
            {
                findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds an empty closed set.", table.File, table.Line, SubjectOf(table)));
                continue;
            }

            var index = 0;

            foreach (var row in RowsOf(table))
            {
                if (row.TryGetProperty(RecordFields.Value, out var value)
                    && !allowed.Any(candidate => ValueEquals(candidate, value)))
                {
                    findings.Add(new Finding(
                        "reference-data-value-invalid",
                        $"Reference data '{table.Id}' holds row {index} with value {Show(value)} outside its choices.",
                        table.File,
                        table.Line,
                        SubjectOf(table)));
                }

                index++;
            }
        }
    }

    /// <summary>Reports a reference-data row whose cross-reference names a key no row of the named table holds.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunKeyResolves(ProjectModel model, List<Finding> findings) =>
        ReferenceData.ReferenceDataKeys.RunKeyResolves(model, findings);

    /// <summary>Reports a reference-data table whose required role is played by no row of a variant. Each variant answers lookups on its own, so each plays every required role.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunRolePlayed(ProjectModel model, List<Finding> findings) =>
        ReferenceData.ReferenceDataRoles.RunRolePlayed(model, findings);

    /// <summary>Reports a reference-data table whose rows of one variant leave dates with no row in force, or cover no row on the date the cast declares as today. Each variant covers its own dates.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record and the cast.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunCoverage(ProjectModel model, List<Finding> findings) =>
        ReferenceData.ReferenceDataCoverage.RunCoverage(model, findings);

    /// <summary>Reports amount fields one calculation rule computes whose sign conventions differ or are missing.</summary>
    /// <param name="model">The loaded project model that holds every use case and rule.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunAmountSign(ProjectModel model, List<Finding> findings) =>
        ReferenceData.ReferenceDataAmounts.RunAmountSign(model, findings);

    /// <summary>Reports a reference-data table with no rows yet that states no summary: the summary states what rows the table will hold.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunEmptySummary(ProjectModel model, List<Finding> findings)
    {
        foreach (var table in model.Records
            .Where(record => record.Type == RecordTypes.ReferenceData)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            if (RowsOf(table).Count == 0 && table.Field(RecordFields.Summary) is not { Length: > 0 })
            {
                findings.Add(new Finding(
                    "record-invalid",
                    $"Reference data '{table.Id}' holds no rows and states no summary; state in `summary` what rows the table will hold.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
            }
        }
    }

    /// <summary>Whether two row values are equal: strings by ordinal comparison, numbers by decimal value, and anything else by raw text.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>True when the values are equal.</returns>
    private static bool ValueEquals(JsonElement left, JsonElement right)
    {
        if (left.ValueKind != right.ValueKind)
        {
            return false;
        }

        return left.ValueKind switch
        {
            JsonValueKind.String => left.GetString() == right.GetString(),
            JsonValueKind.Number => left.TryGetDecimal(out var a) && right.TryGetDecimal(out var b) && a == b,
            JsonValueKind.True or JsonValueKind.False => true,
            _ => left.GetRawText() == right.GetRawText(),
        };
    }

    /// <summary>One row value as finding text: a string in quotes, anything else as written.</summary>
    /// <param name="value">The row value.</param>
    /// <returns>The value text.</returns>
    private static string Show(JsonElement value) =>
        value.ValueKind == JsonValueKind.String ? $"'{value.GetString()}'" : value.GetRawText();
}
