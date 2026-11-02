using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The feature checks: every non-refused feature advances a goal, and a shipped feature is proven.</summary>
/// <follows>litespec/core/intent#state-each-feature-with-its-lifecycle</follows>
/// <follows>litespec/core/model#flag-a-shipped-feature-below-proven</follows>
internal static class FeatureChecks
{
    /// <summary>Reports a non-refused feature advancing no goal, and a shipped feature whose completion is below proven.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record => record.Type == RecordTypes.Feature))
        {
            if (record.Status == RecordStatuses.Retired)
            {
                continue;
            }

            if (record.Field(RecordFields.Lifecycle) == "shipped"
                && CompletionOf(model, record) is { Stage: not "proven" } completion)
            {
                findings.Add(new Finding(
                    "feature-shipped-drift",
                    $"Feature '{record.Id}' is shipped while its completion is {completion.Stage}.",
                    record.File, record.Line, SubjectOf(record)));
            }

            if (record.Field(RecordFields.Lifecycle) == "refused")
            {
                continue;
            }

            if (record.Ids(RecordFields.Advances).Count == 0)
            {
                findings.Add(RiskGraded("feature-goal-missing", $"Feature '{record.Id}' advances no goal.", record, model));
            }
        }
    }

    /// <summary>The feature's completion, computed on demand when the builder has not computed it.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="feature">The feature whose completion is wanted.</param>
    /// <returns>The feature completion, or null when no completion can be computed for it.</returns>
    static Completion? CompletionOf(ProjectModel model, Record feature)
    {
        if (!model.Completion.ContainsKey(feature.Id))
        {
            CompletionCalculator.Compute(model);
        }

        return model.Completion.TryGetValue(feature.Id, out var completion) ? completion : null;
    }
}
