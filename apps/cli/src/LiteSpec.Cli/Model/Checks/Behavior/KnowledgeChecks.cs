using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The knowledge variation checks: every table whose rows name a variant states its dimension, only knowledge tables vary, and every variant a knowledge area names holds rows in each varying table.</summary>
/// <follows>litespec/core/knowledge#give-each-knowledge-area-one-model</follows>
internal static class KnowledgeChecks
{
    /// <summary>A kebab-case dimension or variant name: a lowercase letter or digit opening, then lowercase letters, digits or hyphens.</summary>
    private static readonly Regex KebabName = new("^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Reports a reference-data table whose rows name a variant while the table states no variation dimension, and the misshapen variation a table states: a dimension or variant outside kebab-case, variation on a module-owned table, a stated dimension with no varying row, and rows with and without variant in one table.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunVariationUndeclared(ProjectModel model, List<Finding> findings)
    {
        foreach (var table in model.Records
            .Where(record => !record.Derived && record.Type == RecordTypes.ReferenceData)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            CheckVariationShape(table, findings);

            var rows = RowsOf(table);
            CheckVariantShape(table, rows, findings);

            // A module-owned table that varies is moved to a knowledge area first, so its other variation findings wait for the move.
            if (CheckModuleVariation(model, table, rows, findings))
            {
                continue;
            }

            var named = rows.Count(row => Str(row, RecordFields.Variant) is { Length: > 0 });
            var declared = table.Field(RecordFields.Variation) is { Length: > 0 };

            if (named > 0 && !declared)
            {
                findings.Add(new Finding(
                    "knowledge-variation-undeclared",
                    $"Reference data '{table.Id}' holds rows naming a variant while the table states no variation dimension.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
            }

            if (declared && named == 0)
            {
                findings.Add(new Finding(
                    "record-invalid",
                    $"Reference data '{table.Id}' states variation '{table.Field(RecordFields.Variation)}' but no row names a variant.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
            }

            if (named > 0 && named < rows.Count)
            {
                findings.Add(new Finding(
                    "record-invalid",
                    $"Reference data '{table.Id}' holds rows with and rows without variant; state variant on every row or on none.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
            }
        }
    }

    /// <summary>Reports a variant a knowledge area names that holds no row in a varying reference-data table, a row naming a variant outside its area's list, and a variant name outside kebab-case.</summary>
    /// <param name="model">The loaded project model that holds every reference-data and knowledge area record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunSetMissing(ProjectModel model, List<Finding> findings)
    {
        foreach (var area in model.Records
            .Where(record => !record.Derived && record.Type == RecordTypes.KnowledgeArea)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            CheckAreaShape(area, findings);
        }

        foreach (var table in model.Records
            .Where(record => !record.Derived && record.Type == RecordTypes.ReferenceData)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            if (AreaOf(model, table) is not { } area)
            {
                continue;
            }

            // A variant outside kebab-case already draws its shape finding, so membership judges the well-formed names only.
            var variants = area.Strings(RecordFields.Variants).Where(name => KebabName.IsMatch(name)).ToList();

            if (variants.Count == 0)
            {
                continue;
            }

            var rows = RowsOf(table);
            var held = rows
                .Select(row => Str(row, RecordFields.Variant))
                .Where(variant => variant is { Length: > 0 })
                .ToHashSet(StringComparer.Ordinal);
            var listed = variants.ToHashSet(StringComparer.Ordinal);

            var index = 0;

            foreach (var row in rows)
            {
                if (Str(row, RecordFields.Variant) is { Length: > 0 } variant
                    && KebabName.IsMatch(variant)
                    && !listed.Contains(variant))
                {
                    findings.Add(new Finding(
                        "record-invalid",
                        $"Reference data '{table.Id}' holds row {index} naming variant '{variant}' outside the variants of knowledge area '{area.Id}'.",
                        table.File,
                        table.Line,
                        SubjectOf(table)));
                }

                index++;
            }

            // A table that states no dimension, or whose rows name no variant, varies by nothing: only a varying table owes every listed variant its rows.
            if (table.Field(RecordFields.Variation) is not { Length: > 0 } || held.Count == 0)
            {
                continue;
            }

            foreach (var variant in variants)
            {
                if (!held.Contains(variant))
                {
                    findings.Add(new Finding(
                        "knowledge-set-missing",
                        $"Reference data '{table.Id}' holds no row of variant '{variant}' named by knowledge area '{area.Id}'.",
                        table.File,
                        table.Line,
                        SubjectOf(table)));
                }
            }
        }
    }

    /// <summary>Reports a variation dimension outside lowercase kebab-case.</summary>
    /// <param name="table">The reference-data record whose dimension is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckVariationShape(Record table, List<Finding> findings)
    {
        if (table.Field(RecordFields.Variation) is { } variation && !KebabName.IsMatch(variation))
        {
            findings.Add(new Finding(
                "record-invalid",
                $"Reference data '{table.Id}' states variation '{variation}' outside lowercase kebab-case.",
                table.File,
                table.Line,
                SubjectOf(table)));
        }
    }

    /// <summary>Reports a row variant outside lowercase kebab-case.</summary>
    /// <param name="table">The reference-data record whose row variants are judged.</param>
    /// <param name="rows">The row objects in written order.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckVariantShape(Record table, IReadOnlyList<JsonElement> rows, List<Finding> findings)
    {
        var index = 0;

        foreach (var row in rows)
        {
            if (Str(row, RecordFields.Variant) is { } variant && !KebabName.IsMatch(variant))
            {
                findings.Add(new Finding(
                    "record-invalid",
                    $"Reference data '{table.Id}' holds row {index} with variant '{variant}' outside lowercase kebab-case.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
            }

            index++;
        }
    }

    /// <summary>Reports a module-owned table that states variation or holds rows naming a variant: only knowledge tables vary, so the table moves to a knowledge area.</summary>
    /// <param name="model">The loaded project model that resolves the module named by the home.</param>
    /// <param name="table">The reference-data record whose home is inspected.</param>
    /// <param name="rows">The row objects in written order.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <returns>True when the table varies on a module home and drew the finding; otherwise false.</returns>
    private static bool CheckModuleVariation(ProjectModel model, Record table, IReadOnlyList<JsonElement> rows, List<Finding> findings)
    {
        if (!Identifiers.TrySplit(table.Id, out _, out var home, out _) || home is not { Length: > 0 })
        {
            return false;
        }

        // A home naming a knowledge area is judged as one, and a home naming neither is owned by the parent check.
        if (model.ById.ContainsKey($"knowledge-area/{home}") || !model.ById.ContainsKey($"module/{home}"))
        {
            return false;
        }

        var declared = table.Field(RecordFields.Variation) is { Length: > 0 };
        var named = rows.Any(row => Str(row, RecordFields.Variant) is { Length: > 0 });

        if (!declared && !named)
        {
            return false;
        }

        var detail = (declared, named) switch
        {
            (true, true) => $"states variation '{table.Field(RecordFields.Variation)}' and holds rows naming a variant",
            (true, false) => $"states variation '{table.Field(RecordFields.Variation)}'",
            _ => "holds rows naming a variant",
        };

        findings.Add(new Finding(
            "record-invalid",
            $"Reference data '{table.Id}' is owned by module '{home}' and {detail}; module-owned tables state no variation and name no variant.",
            table.File,
            table.Line,
            SubjectOf(table)));

        return true;
    }

    /// <summary>Reports a knowledge area variant outside lowercase kebab-case.</summary>
    /// <param name="area">The knowledge area record whose variants are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckAreaShape(Record area, List<Finding> findings)
    {
        foreach (var variant in area.Strings(RecordFields.Variants))
        {
            if (!KebabName.IsMatch(variant))
            {
                findings.Add(new Finding(
                    "record-invalid",
                    $"Knowledge area '{area.Id}' names variant '{variant}' outside lowercase kebab-case.",
                    area.File,
                    area.Line,
                    SubjectOf(area)));
            }
        }
    }

    /// <summary>The knowledge area a table belongs to: the area its identifier home names, or null when the home names none.</summary>
    /// <param name="model">The loaded project model that resolves the area.</param>
    /// <param name="table">The reference-data record whose area is sought.</param>
    /// <returns>The knowledge area record, or null when the identifier names no home or no area carries that name.</returns>
    private static Record? AreaOf(ProjectModel model, Record table)
    {
        if (!Identifiers.TrySplit(table.Id, out _, out var home, out _) || home is not { Length: > 0 })
        {
            return null;
        }

        return model.ById.TryGetValue($"knowledge-area/{home}", out var area) && area.Type == RecordTypes.KnowledgeArea
            ? area
            : null;
    }

    /// <summary>The rows of a reference-data table: the object entries of its rows array.</summary>
    /// <param name="table">The reference-data record whose rows are read.</param>
    /// <returns>The row objects in written order, or an empty list when the table holds no rows array.</returns>
    private static IReadOnlyList<JsonElement> RowsOf(Record table) => table.Objects(RecordFields.Rows);
}
