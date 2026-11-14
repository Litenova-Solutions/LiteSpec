using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.ProseText;

namespace LiteSpec.Cli.Model.Checks.Intent;

/// <summary>
///     The constraint checks: a constraint's <c>basedOn</c> names research records, and the requirements a constraint's
///     Effect on the Product section names are exactly the requirements that cite the constraint.
/// </summary>
/// <follows>litespec/core/intent#answer-every-constraint</follows>
/// <follows>litespec/core/intent#state-each-constraint-rule-with-its-condition</follows>
internal static class ConstraintChecks
{
    /// <summary>Runs every constraint check.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckBasedOn(model, findings);
        CheckEffects(model, findings);
    }

    /// <summary>Reports field-invalid for each constraint whose basedOn names a record that is not research.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckBasedOn(ProjectModel model, List<Finding> findings)
    {
        var research = model.Records
            .Where(record => record.Type == RecordTypes.Research)
            .Select(record => record.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var constraint in model.Records.Where(record => record.Type == RecordTypes.Constraint && !record.IsEntry))
        {
            foreach (var id in constraint.Ids(RecordFields.BasedOn).Where(id => !research.Contains(id)))
            {
                findings.Add(new Finding(
                    "field-invalid",
                    $"Constraint '{constraint.Id}' names '{id}' in basedOn, which is no research record.",
                    constraint.File,
                    constraint.Line,
                    SubjectOf(constraint)));
            }
        }
    }

    /// <summary>
    ///     Reports each requirement that cites a constraint but is not named in its Effect on the Product section, and each
    ///     requirement that section names without citing the constraint.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckEffects(ProjectModel model, List<Finding> findings)
    {
        var requirements = model.Records.Where(record => record.Type == RecordTypes.Requirement).ToList();

        foreach (var constraint in model.Records.Where(record => record.Type == RecordTypes.Constraint && !record.IsEntry))
        {
            if (constraint.ExplanationFile is null || constraint.Explanation is null)
            {
                continue;
            }

            if (SectionOf(constraint.Explanation, EffectSection) is not { } effect)
            {
                continue;
            }

            var named = RequirementIds.Matches(effect).Select(match => match.Value).ToHashSet(StringComparer.Ordinal);
            var citing = requirements
                .Where(requirement => requirement.Ids(RecordFields.Cites).Contains(constraint.Id, StringComparer.Ordinal))
                .Select(requirement => requirement.Id)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var id in citing.Where(id => !named.Contains(id)).Order(StringComparer.Ordinal))
            {
                findings.Add(new Finding(
                    "constraint-effect-unlisted",
                    $"Requirement '{id}' cites constraint '{constraint.Id}', but its Effect on the Product section does not name it.",
                    constraint.ExplanationFile,
                    1,
                    SubjectOf(constraint)));
            }

            foreach (var id in named.Where(id => !citing.Contains(id)).Order(StringComparer.Ordinal))
            {
                findings.Add(new Finding(
                    "constraint-effect-uncited",
                    $"Effect on the Product of '{constraint.Id}' names requirement '{id}', which does not cite the constraint.",
                    constraint.ExplanationFile,
                    1,
                    SubjectOf(constraint)));
            }
        }
    }

    /// <summary>The H2 heading of the section that names the requirements a constraint holds.</summary>
    private const string EffectSection = "Effect on the Product";

    /// <summary>Finds each requirement identifier in a text.</summary>
    private static readonly Regex RequirementIds = new(@"requirement/[a-z0-9](?:[a-z0-9.-]*[a-z0-9])?", RegexOptions.Compiled);
}
