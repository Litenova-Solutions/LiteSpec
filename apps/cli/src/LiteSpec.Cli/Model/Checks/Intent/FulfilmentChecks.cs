using System.Text.Json;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Intent;

/// <summary>The fulfilment checks: every requirement is fulfilled or decided and cites a reason, and every applying constraint is cited.</summary>
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

    /// <summary>Reports a requirement fulfilled by no rule, use case, reaction, job, notification or setting.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
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

        foreach (var requirement in model.Records.Where(record => record.Type == RecordTypes.Requirement))
        {
            if (!FulfilledKinds.Contains(requirement.Field(RecordFields.Kind) ?? ""))
            {
                continue;
            }

            // A quality requirement is also fulfilled when a technical decision serves
            // it: the decision names it in basedOn instead of a rule or use case.
            var decided = requirement.Field(RecordFields.Kind) == "quality" && served.Contains(requirement.Id);

            if (!fulfilled.Contains(requirement.Id) && !decided)
            {
                findings.Add(new Finding("requirement-fulfillment-missing", $"Requirement '{requirement.Id}' is fulfilled by no {OrList(RecordTypes.FulfillingTypes)}.", requirement.File, requirement.Line, SubjectOf(requirement)));
            }
        }
    }

    /// <summary>Reports a requirement naming no reason in cites.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunRequirementCited(ProjectModel model, List<Finding> findings)
    {
        foreach (var requirement in model.Records.Where(record => record.Type == RecordTypes.Requirement))
        {
            if (requirement.Ids(RecordFields.Cites).Count == 0)
            {
                findings.Add(new Finding(
                    "requirement-cites-missing",
                    $"Requirement '{requirement.Id}' cites no goal, problem, feature, constraint, research or decision.",
                    requirement.File, requirement.Line, SubjectOf(requirement))
                {
                    TierOverride = "A",
                });
            }
        }
    }

    /// <summary>Reports an applying constraint no requirement cites.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunConstraintCited(ProjectModel model, List<Finding> findings)
    {
        var cited = model.Records
            .Where(record => record.Type == RecordTypes.Requirement)
            .SelectMany(record => record.Ids(RecordFields.Cites))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var constraint in model.Records.Where(record => record.Type == RecordTypes.Constraint))
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
