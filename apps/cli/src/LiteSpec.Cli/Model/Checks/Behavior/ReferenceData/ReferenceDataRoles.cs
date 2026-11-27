using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior.ReferenceData;

/// <summary>The reference-data role checks: every required role is played by a row of each variant, and every required role and row role stays in kebab-case.</summary>
/// <follows>litespec/core/knowledge#name-a-role-not-a-row</follows>
internal static class ReferenceDataRoles
{
    /// <summary>Gets the name of the `requiredRoles` field.</summary>
    const string RequiredRoles = "requiredRoles";

    /// <summary>A kebab-case row key or role: a lowercase letter or digit opening, then lowercase letters, digits or hyphens.</summary>
    private static readonly Regex KebabName = new("^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Reports a reference-data table whose required role is played by no row of a variant. Each variant answers lookups on its own, so each plays every required role.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunRolePlayed(ProjectModel model, List<Finding> findings)
    {
        foreach (var table in model.Records
            .Where(record => record.Type == RecordTypes.ReferenceData)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            if (table.Raw.ValueKind != JsonValueKind.Object
                || !table.Raw.TryGetProperty(RequiredRoles, out var required))
            {
                continue;
            }

            if (required.ValueKind != JsonValueKind.Array)
            {
                findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds required roles that are not a list.", table.File, table.Line, SubjectOf(table)));
                continue;
            }

            var wanted = new List<string>();

            foreach (var entry in required.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.String || entry.GetString() is not { } role)
                {
                    findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds a required role that is not a name.", table.File, table.Line, SubjectOf(table)));
                }
                else if (!KebabName.IsMatch(role))
                {
                    findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' requires role '{role}' outside lowercase kebab-case.", table.File, table.Line, SubjectOf(table)));
                }
                else
                {
                    wanted.Add(role);
                }
            }

            var rows = ReferenceDataKeys.RowsOf(table);

            foreach (var variant in VariantsOf(rows))
            {
                var played = rows
                    .Where(row => string.Equals(Str(row, RecordFields.Variant), variant, StringComparison.Ordinal))
                    .Select(row => Str(row, RecordFields.Role))
                    .Where(role => role is not null)
                    .ToHashSet(StringComparer.Ordinal);

                var whose = variant is null ? "no row plays" : $"no row of variant '{variant}' plays";

                foreach (var role in wanted.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
                {
                    if (!played.Contains(role))
                    {
                        findings.Add(new Finding(
                            "reference-data-role-missing",
                            $"Reference data '{table.Id}' requires role '{role}', which {whose}.",
                            table.File,
                            table.Line,
                            SubjectOf(table)));
                    }
                }
            }
        }
    }

    /// <summary>The distinct row variants of a table: the shared rows first, then each named variant in order.</summary>
    /// <param name="rows">The row objects in written order.</param>
    /// <returns>The distinct variants, with null for rows that name none.</returns>
    private static List<string?> VariantsOf(IReadOnlyList<JsonElement> rows)
    {
        var variants = new List<string?>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var shared = false;

        foreach (var row in rows)
        {
            if (Str(row, RecordFields.Variant) is { } variant)
            {
                if (seen.Add(variant))
                {
                    variants.Add(variant);
                }
            }
            else
            {
                shared = true;
            }
        }

        variants.Sort(StringComparer.Ordinal);

        return shared ? [null, .. variants] : variants;
    }
}
