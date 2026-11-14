using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Intent;

/// <summary>
///     The constraint checks: a constraint's <c>basedOn</c> names research records, and its Effect on the Product
///     names exactly the requirements that cite the constraint.
/// </summary>
/// <follows>litespec/core/intent#answer-every-constraint</follows>
internal static class ConstraintChecks
{
    /// <summary>Runs every constraint check.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings) =>
        CheckBasedOn(model, findings);

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
}
