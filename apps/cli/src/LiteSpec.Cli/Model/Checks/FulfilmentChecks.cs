using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The fulfilment checks: every live requirement is fulfilled or decided and cites a reason, and every applying constraint is cited.</summary>
/// <follows>litespec/core/intent#restrict-requirement-fulfillment-to-behavior</follows>
/// <follows>litespec/core/intent#cite-the-reasons-of-every-requirement</follows>
/// <follows>litespec/core/intent#cite-each-applying-constraint-from-a-requirement</follows>
internal static class FulfilmentChecks
{
    /// <summary>Requirement kinds that code makes true, so some rule, use case, reaction, job, notification or setting fulfills them.</summary>
    private static readonly HashSet<string> FulfilledKinds = new(StringComparer.Ordinal)
    {
        "functional", "quality", "policy",
    };

    /// <summary>Feature lifecycles under which a requirement is not yet, or never, made true by code.</summary>
    private static readonly HashSet<string> UndeliveredStates = new(StringComparer.Ordinal)
    {
        "planned", "later", "refused",
    };

    /// <summary>Reports a requirement fulfilled by no rule, use case, reaction, job, notification or setting.</summary>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var fulfilled = model.Records
            .Where(record => RecordTypes.FulfillingTypes.Contains(record.Type, StringComparer.Ordinal))
            .SelectMany(record => record.Ids(RecordFields.Fulfills))
            .ToHashSet(StringComparer.Ordinal);

        var served = model.Records
            .Where(record => record.Type == RecordTypes.Decision && MarkChecks.UnderImplementationDecisions(model.Config, record.File))
            .SelectMany(record => record.Ids(RecordFields.BasedOn))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var requirement in model.Records.Where(record => record.Type == RecordTypes.Requirement && record.Status != RecordStatuses.Retired))
        {
            if (!FulfilledKinds.Contains(requirement.Field(RecordFields.Kind) ?? ""))
            {
                continue;
            }

            var undelivered = requirement.Ids(RecordFields.Cites).Any(id =>
                model.ById.TryGetValue(id, out var cited)
                && cited.Type == RecordTypes.Feature
                && UndeliveredStates.Contains(cited.Field(RecordFields.Lifecycle) ?? ""));

            // A quality requirement is also fulfilled when a technical decision serves
            // it: the decision names it in basedOn instead of a rule or use case.
            var decided = requirement.Field(RecordFields.Kind) == "quality" && served.Contains(requirement.Id);

            if (!fulfilled.Contains(requirement.Id) && !undelivered && !decided)
            {
                findings.Add(new Finding("requirement-fulfillment-missing", $"Requirement '{requirement.Id}' is fulfilled by no {OrList(RecordTypes.FulfillingTypes)}.", requirement.File, requirement.Line, SubjectOf(requirement)));
            }
        }
    }

    /// <summary>Reports a requirement naming no reason in cites: tier A, tier G when it carries risk.</summary>
    internal static void RunRequirementCited(ProjectModel model, List<Finding> findings)
    {
        foreach (var requirement in model.Records.Where(record => record.Type == RecordTypes.Requirement && record.Status != RecordStatuses.Retired))
        {
            if (requirement.Ids(RecordFields.Cites).Count == 0)
            {
                findings.Add(Tiered("requirement-cites-missing", $"Requirement '{requirement.Id}' cites no goal, problem, feature, constraint, research or decision.", requirement, model));
            }
        }
    }

    /// <summary>Reports an applying constraint no requirement cites.</summary>
    internal static void RunConstraintCited(ProjectModel model, List<Finding> findings)
    {
        var cited = model.Records
            .Where(record => record.Type == RecordTypes.Requirement && record.Status != RecordStatuses.Retired)
            .SelectMany(record => record.Ids(RecordFields.Cites))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var constraint in model.Records.Where(record => record.Type == RecordTypes.Constraint && record.Status != RecordStatuses.Retired))
        {
            if (constraint.Raw.ValueKind == JsonValueKind.Object
                && constraint.Raw.TryGetProperty(RecordFields.Applies, out var applies)
                && applies.ValueKind == JsonValueKind.True
                && !cited.Contains(constraint.Id))
            {
                findings.Add(new Finding("constraint-unreached", $"Constraint '{constraint.Id}' applies and is cited by no requirement.", constraint.File, constraint.Line, SubjectOf(constraint)));
            }
        }
    }
}
