using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The identifier checks: every identifier is unique, follows the type and home grammar, and names the home its type requires.</summary>
/// <follows>litespec/backend/identifiers#follow-the-identifier-form-of-each-record-type</follows>
/// <follows>litespec/backend/identifiers#name-records-with-one-to-six-words</follows>
/// <follows>litespec/backend/identifiers#use-every-identifier-once</follows>
/// <follows>litespec/backend/identifiers#keep-aggregate-names-unique-across-the-project</follows>
/// <follows>litespec/backend/identifiers#keep-use-case-names-unique-across-the-project</follows>
internal static class IdentifierChecks
{
    /// <summary>Reports identifier-invalid, identifier-duplicate, aggregate-name-clash and use-case-name-clash.</summary>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var recordNames = new HashSet<string>(StringComparer.Ordinal);
        var useCaseNames = new Dictionary<string, List<Record>>(StringComparer.Ordinal);

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

            if (record.Type == RecordTypes.Requirement && home is not null)
            {
                CheckRequirementTopic(record, home, findings);
            }

            if (!Identifiers.ValidName(name, record.Type == RecordTypes.Decision) || Identifiers.AllDigitsName(name))
            {
                findings.Add(new Finding("identifier-invalid", $"Identifier '{record.Id}' breaks the name rules.", record.File, record.Line, SubjectOf(record)));
            }

            if (record.Type == RecordTypes.Aggregate && !recordNames.Add(name))
            {
                findings.Add(new Finding("aggregate-name-clash", $"Aggregate '{record.Id}' shares its name '{name}' with another aggregate.", record.File, record.Line, SubjectOf(record)));
            }

            if (record.Type == RecordTypes.UseCase)
            {
                if (!useCaseNames.TryGetValue(name, out var grouped))
                {
                    grouped = [];
                    useCaseNames[name] = grouped;
                }

                grouped.Add(record);
            }
        }

        foreach (var clash in useCaseNames.Where(entry => entry.Value.Count > 1))
        {
            foreach (var record in clash.Value)
            {
                findings.Add(new Finding("use-case-name-clash", $"Use case '{record.Id}' shares its name '{clash.Key}' with another use case.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Reports a requirement whose topic segment differs from its page's name.</summary>
    private static void CheckRequirementTopic(Record record, string home, List<Finding> findings)
    {
        var page = Path.GetFileNameWithoutExtension(record.File);

        if (!home.Equals(page, StringComparison.Ordinal))
        {
            findings.Add(new Finding("identifier-invalid", $"Requirement '{record.Id}' names topic '{home}', which differs from its page '{page}'.", record.File, record.Line, SubjectOf(record)));
        }
    }
}
