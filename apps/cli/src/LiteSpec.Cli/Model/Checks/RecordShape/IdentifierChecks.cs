using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.RecordShape;

/// <summary>The identifier checks: every identifier is unique, follows the type and home grammar, sits in the folder its home names, carries a name used once in the product, and takes a decision number used once.</summary>
/// <follows>litespec/core/language#follow-the-identifier-form-of-each-record-type</follows>
/// <follows>litespec/core/language#name-records-with-one-to-six-words</follows>
/// <follows>litespec/core/language#use-every-identifier-once</follows>
/// <follows>litespec/core/model#move-each-identifier-with-its-module</follows>
/// <follows>litespec/core/model#keep-each-name-unique-in-the-product</follows>
/// <follows>litespec/core/model#take-decision-numbers-from-one-sequence</follows>
internal static class IdentifierChecks
{
    /// <summary>Types whose identifier home names the owning module, so the record sits in that module folder.</summary>
    private static readonly HashSet<string> ModuleHomedTypes = new(StringComparer.Ordinal)
    {
        RecordTypes.Aggregate,
        RecordTypes.UseCase,
        RecordTypes.Reaction,
        RecordTypes.Job,
        RecordTypes.Notification,
        RecordTypes.Setting,
    };

    /// <summary>Types whose name segment stays unique in the product.</summary>
    private static readonly HashSet<string> UniqueNameTypes = new(StringComparer.Ordinal)
    {
        RecordTypes.Feature,
        RecordTypes.UseCase,
        RecordTypes.Aggregate,
        RecordTypes.Permission,
        RecordTypes.IntegrationEvent,
        RecordTypes.ExternalSystem,
        RecordTypes.Runbook,
        RecordTypes.Departure,
    };

    /// <summary>Reports identifier-invalid, identifier-duplicate, identifier-folder-drift, name-duplicate and decision-number-duplicate.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            // A derived generic result carries its use case home and name, not the grammar.
            if (record.Id.Length == 0 || record.Derived)
            {
                continue;
            }

            if (!seen.Add(record.Id))
            {
                findings.Add(new Finding("identifier-duplicate", $"Identifier '{record.Id}' is used twice.", record.File, record.Line, record.Id));
            }

            if (!Identifiers.TrySplit(record.Id, out var prefix, out var home, out var name))
            {
                // A home-parented record reports its missing home through
                // record-parent-missing, so identifier-invalid stays silent here.
                if (ParentChecks.ReportsMissingHome(record))
                {
                    continue;
                }

                findings.Add(new Finding("identifier-invalid", $"Identifier '{record.Id}' is not type/name or type/home.name.", record.File, record.Line, SubjectOf(record)));
                continue;
            }

            if (prefix != record.Type)
            {
                findings.Add(new Finding("identifier-invalid", $"Identifier '{record.Id}' names type {prefix} on a {record.Type} record.", record.File, record.Line, SubjectOf(record)));
            }

            var wantsHome = RecordTypes.WithHome.Contains(record.Type);

            if (wantsHome && home is null)
            {
                if (!ParentChecks.ReportsMissingHome(record))
                {
                    findings.Add(new Finding("identifier-invalid", $"Identifier '{record.Id}' names no home.", record.File, record.Line, SubjectOf(record)));
                }
            }
            else if (!wantsHome && home is not null)
            {
                findings.Add(new Finding("identifier-invalid", $"Identifier '{record.Id}' holds a home and the {record.Type} type holds no home.", record.File, record.Line, SubjectOf(record)));
            }

            if (!Identifiers.ValidName(name, record.Type == RecordTypes.Decision) || Identifiers.AllDigitsName(name))
            {
                findings.Add(new Finding("identifier-invalid", $"Identifier '{record.Id}' breaks the name rules.", record.File, record.Line, SubjectOf(record)));
            }
        }

        CheckFolderDrift(model, findings);
        CheckNameDuplicate(model, findings);
        CheckDecisionNumbers(model, findings);
    }

    /// <summary>Reports a module-homed record whose identifier home disagrees with its folder module.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFolderDrift(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records)
        {
            if (record.Derived || !ModuleHomedTypes.Contains(record.Type))
            {
                continue;
            }

            if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is not { Length: > 0 })
            {
                continue;
            }

            var folder = FolderModuleOf(record.File);

            if (folder is not { Length: > 0 } || string.Equals(folder, home, StringComparison.Ordinal))
            {
                continue;
            }

            // A home naming no declared module is record-parent-missing, and a
            // folder naming none is module-undeclared, so drift stays silent here.
            if (!model.ById.ContainsKey($"module/{home}") || !model.ById.ContainsKey($"module/{folder}"))
            {
                continue;
            }

            findings.Add(new Finding(
                "identifier-folder-drift",
                $"{Label(record.Type)} '{record.Id}' sits in module '{folder}' but its identifier names module '{home}'.",
                record.File,
                record.Line,
                SubjectOf(record)));
        }
    }

    /// <summary>Reports a name segment used twice in the product within one record type.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckNameDuplicate(ProjectModel model, List<Finding> findings)
    {
        var grouped = new Dictionary<(string Type, string Name), List<Record>>();

        foreach (var record in model.Records)
        {
            if (record.Derived || !UniqueNameTypes.Contains(record.Type))
            {
                continue;
            }

            if (!Identifiers.TrySplit(record.Id, out _, out _, out var name) || name.Length == 0)
            {
                continue;
            }

            var key = (record.Type, name);

            if (!grouped.TryGetValue(key, out var records))
            {
                records = [];
                grouped[key] = records;
            }

            records.Add(record);
        }

        foreach (var ((type, name), records) in grouped.Where(entry => entry.Value.Count > 1))
        {
            // One identifier written twice is identifier-duplicate, not a reused name.
            if (records.Select(record => record.Id).Distinct(StringComparer.Ordinal).Count() == 1)
            {
                continue;
            }

            foreach (var record in records)
            {
                var label = Label(type);

                findings.Add(new Finding(
                    "name-duplicate",
                    $"{label} '{record.Id}' shares its name '{name}' with another {label.ToLowerInvariant()}.",
                    record.File,
                    record.Line,
                    SubjectOf(record)));
            }
        }
    }

    /// <summary>Reports a decision number used twice across every home.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckDecisionNumbers(ProjectModel model, List<Finding> findings)
    {
        var grouped = new Dictionary<string, List<Record>>(StringComparer.Ordinal);

        foreach (var record in model.Records.Where(record => !record.Derived && record.Type == RecordTypes.Decision))
        {
            if (!Identifiers.TrySplit(record.Id, out _, out _, out var name))
            {
                continue;
            }

            var dash = name.IndexOf('-', StringComparison.Ordinal);
            var number = dash < 0 ? name : name[..dash];

            if (number.Length != 4 || !number.All(char.IsDigit))
            {
                continue;
            }

            if (!grouped.TryGetValue(number, out var records))
            {
                records = [];
                grouped[number] = records;
            }

            records.Add(record);
        }

        foreach (var (number, records) in grouped.Where(entry => entry.Value.Count > 1))
        {
            // One identifier written twice is identifier-duplicate, not a reused number.
            if (records.Select(record => record.Id).Distinct(StringComparer.Ordinal).Count() == 1)
            {
                continue;
            }

            foreach (var record in records)
            {
                findings.Add(new Finding(
                    "decision-number-duplicate",
                    $"Decision '{record.Id}' shares its number '{number}' with another decision.",
                    record.File,
                    record.Line,
                    SubjectOf(record)));
            }
        }
    }

    /// <summary>The module folder the file sits under, or null when its path holds no modules folder.</summary>
    /// <param name="file">The record file path.</param>
    /// <returns>The module folder name, or null when the path holds no modules folder.</returns>
    private static string? FolderModuleOf(string file)
    {
        var segments = file.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        for (var at = 0; at + 1 < segments.Length; at++)
        {
            if (string.Equals(segments[at], "modules", StringComparison.Ordinal))
            {
                return segments[at + 1];
            }
        }

        return null;
    }

    /// <summary>The prose label a finding shows for the record type.</summary>
    /// <param name="type">The record type identifier.</param>
    /// <returns>The capitalized prose label.</returns>
    private static string Label(string type) => type switch
    {
        RecordTypes.UseCase => "Use case",
        RecordTypes.Aggregate => "Aggregate",
        RecordTypes.Feature => "Feature",
        RecordTypes.Permission => "Permission",
        RecordTypes.IntegrationEvent => "Integration event",
        RecordTypes.ExternalSystem => "External system",
        RecordTypes.Runbook => "Runbook",
        RecordTypes.Departure => "Departure",
        RecordTypes.Reaction => "Reaction",
        RecordTypes.Job => "Job",
        RecordTypes.Notification => "Notification",
        RecordTypes.Setting => "Setting",
        _ => "Record",
    };
}
