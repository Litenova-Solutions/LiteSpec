using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.RecordShape;

/// <summary>The identifier checks: every identifier is unique, follows the type and home grammar, sits in the folder its home names, carries a name used once in the product, takes a decision number used once, and takes its name from its title with its file named for it.</summary>
/// <follows>litespec/core/language#follow-the-identifier-form-of-each-record-type</follows>
/// <follows>litespec/core/language#name-records-with-one-to-six-words</follows>
/// <follows>litespec/core/language#use-every-identifier-once</follows>
/// <follows>litespec/core/model#move-each-identifier-with-its-module</follows>
/// <follows>litespec/core/model#keep-each-name-unique-in-the-product</follows>
/// <follows>litespec/core/model#take-decision-numbers-from-one-sequence</follows>
/// <follows>litespec/core/model#name-each-identifier-from-its-title</follows>
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

    /// <summary>Record file names fixed by type: the file carries the type's name instead of the record's name.</summary>
    private static readonly HashSet<string> FixedFileNames = new(StringComparer.Ordinal)
    {
        "product.yaml",
        "module.yaml",
        "aggregate.yaml",
        "entry-point.yaml",
        "permissions.yaml",
        "glossary.yaml",
    };

    /// <summary>Reports identifier-invalid, identifier-duplicate, identifier-folder-drift, identifier-title-invalid, name-duplicate and decision-number-duplicate.</summary>
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

            // A requirement's home is the feature whose folder holds it, so the home must name that folder.
            if (record.Type == RecordTypes.Requirement && home is not null && FeatureFolderOf(record.File) is { } feature
                && !string.Equals(home, feature, StringComparison.Ordinal))
            {
                findings.Add(new Finding(
                    "identifier-invalid",
                    $"Requirement '{record.Id}' names feature '{home}', but its folder sits in feature '{feature}'.",
                    record.File,
                    record.Line,
                    SubjectOf(record)));
            }
        }

        CheckFolderDrift(model, findings);
        CheckNameDuplicate(model, findings);
        CheckDecisionNumbers(model, findings);
        CheckIdentifierTitle(model, findings);
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

    /// <summary>Reports a page record whose identifier name is no word subsequence of its title, or whose record file is named for neither its identifier name nor the fixed file name of its type.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckIdentifierTitle(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records)
        {
            // Entries live in their page's file, and a derived record carries no written title.
            if (record.Derived || record.IsEntry || !RecordTypes.All.Contains(record.Type))
            {
                continue;
            }

            if (record.Id.Length == 0 || record.Title.Length == 0)
            {
                continue;
            }

            if (!Identifiers.TrySplit(record.Id, out var prefix, out var home, out var name) || prefix != record.Type)
            {
                continue;
            }

            var wantsHome = RecordTypes.WithHome.Contains(record.Type);

            if ((wantsHome && home is null) || (!wantsHome && home is not null))
            {
                continue;
            }

            if (!Identifiers.ValidName(name, record.Type == RecordTypes.Decision) || Identifiers.AllDigitsName(name))
            {
                continue;
            }

            // The owner prefix and the decision number name the home and the sequence, not the record, so neither faces the title.
            var bare = record.Type == RecordTypes.Decision ? StripDecisionNumber(name) : name;
            var nameWords = bare.Split('-', StringSplitOptions.RemoveEmptyEntries);

            if (!IsSubsequence(nameWords, TitleWords(record.Title)))
            {
                findings.Add(new Finding(
                    "identifier-title-invalid",
                    $"{record.Title} '{record.Id}' carries an identifier name that is not a word subsequence of its title.",
                    record.File,
                    record.Line,
                    SubjectOf(record)));
            }

            // A fixed file name carries the type's name instead of the record's, and the
            // folder check already keeps each named folder on its file's stem.
            var fileName = Path.GetFileName(record.File);

            if (FixedFileNames.Contains(fileName))
            {
                continue;
            }

            var expected = record.Type == RecordTypes.Screen && home is { Length: > 0 }
                ? home + "." + name
                : name;

            if (!string.Equals(RecordFolders.StemOf(fileName), expected, StringComparison.Ordinal))
            {
                findings.Add(new Finding(
                    "identifier-title-invalid",
                    $"{record.Title} '{record.Id}' sits in file '{fileName}' but its identifier names '{expected}'.",
                    record.File,
                    record.Line,
                    SubjectOf(record)));
            }
        }
    }

    /// <summary>The identifier name without a leading four-digit decision number and its hyphen.</summary>
    /// <param name="name">The identifier name after its home.</param>
    /// <returns>The name without the decision number, or the name itself when it carries none.</returns>
    private static string StripDecisionNumber(string name)
    {
        if (name.Length > 5
            && char.IsDigit(name[0])
            && char.IsDigit(name[1])
            && char.IsDigit(name[2])
            && char.IsDigit(name[3])
            && name[4] == '-')
        {
            return name[5..];
        }

        return name;
    }

    /// <summary>The title as lowercase words: split on spaces and hyphens, stripped of every character outside a-z and 0-9, with empty words dropped.</summary>
    /// <param name="title">The record title.</param>
    /// <returns>The title words in title order.</returns>
    private static List<string> TitleWords(string title)
    {
        var words = new List<string>();

        foreach (var part in title.ToLowerInvariant().Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries))
        {
            var cleaned = new string(part.Where(character => (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9')).ToArray());

            if (cleaned.Length > 0)
            {
                words.Add(cleaned);
            }
        }

        return words;
    }

    /// <summary>Whether the name words appear in the title words in the same order, with title words allowed in between.</summary>
    /// <param name="nameWords">The identifier name words in name order.</param>
    /// <param name="titleWords">The title words in title order.</param>
    /// <returns>True when every name word matches a later title word in order; otherwise false.</returns>
    private static bool IsSubsequence(string[] nameWords, List<string> titleWords)
    {
        var at = 0;

        foreach (var word in nameWords)
        {
            while (at < titleWords.Count && !string.Equals(titleWords[at], word, StringComparison.Ordinal))
            {
                at++;
            }

            if (at == titleWords.Count)
            {
                return false;
            }

            at++;
        }

        return true;
    }

    /// <summary>The name of the feature whose folder holds a requirement's own folder, or null when the path holds none.</summary>
    /// <param name="file">The requirement record file path, <c>features/&lt;feature&gt;/&lt;name&gt;/&lt;name&gt;.yaml</c>.</param>
    /// <returns>The feature folder name, or null when the file sits too shallow.</returns>
    private static string? FeatureFolderOf(string file)
    {
        var feature = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(file)));
        var name = feature is null ? null : Path.GetFileName(feature);

        return string.IsNullOrEmpty(name) ? null : name;
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
