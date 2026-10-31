using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The feature check: every non-refused feature advances a goal.</summary>
/// <follows>litespec/core/intent#state-each-feature-with-its-lifecycle</follows>
internal static class FeatureChecks
{
    /// <summary>Reports a non-refused feature advancing no goal.</summary>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record => record.Type == RecordTypes.Feature))
        {
            if (record.Status == RecordStatuses.Retired)
            {
                continue;
            }

            if (record.Field(RecordFields.Lifecycle) == "refused")
            {
                continue;
            }

            if (record.Ids(RecordFields.Advances).Count == 0)
            {
                findings.Add(Tiered("feature-goal-missing", $"Feature '{record.Id}' advances no goal.", record, model));
            }
        }
    }
}
