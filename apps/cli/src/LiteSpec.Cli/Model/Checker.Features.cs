namespace LiteSpec.Model;

/// <summary>The feature check: every non-refused feature advances a goal.</summary>
/// <follows>litespec/core/intent#state-each-feature-with-its-lifecycle</follows>
static partial class Checker
{
    /// <summary>Reports a non-refused feature advancing no goal.</summary>
    private static void CheckFeatureGoal(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record => record.Type == "feature"))
        {
            if (record.Status == "retired")
            {
                continue;
            }

            if (record.Field("lifecycle") == "refused")
            {
                continue;
            }

            if (record.Ids("advances").Count == 0)
            {
                findings.Add(Tiered("feature-without-goal", $"Feature '{record.Id}' advances no goal.", record, model));
            }
        }
    }
}
