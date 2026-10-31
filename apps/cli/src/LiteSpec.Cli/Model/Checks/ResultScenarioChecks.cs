using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The result check: every written result is shown by a scenario.</summary>
/// <follows>litespec/core/use-cases#prove-every-written-result-with-a-scenario</follows>
internal static class ResultScenarioChecks
{
    /// <summary>Reports a written result shown by no scenario.</summary>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        // Scenarios are a large-size type, so a small project shows its results through tests alone.
        if (model.Config.Size != "large")
        {
            return;
        }

        var shown = model.Records
            .Where(record => record.Type == RecordTypes.Scenario)
            .SelectMany(scenario => scenario.Ids(RecordFields.Result))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var result in model.Records.Where(record => record.Type == RecordTypes.Result))
        {
            if (result.Derived)
            {
                continue;
            }

            if (result.Status == RecordStatuses.Retired)
            {
                continue;
            }

            if (shown.Contains(result.Id))
            {
                continue;
            }

            if (result.Field(RecordFields.Outcome) == "success")
            {
                findings.Add(new Finding(
                    "result-scenario-missing",
                    $"Result '{result.Id}' is shown by no scenario.",
                    result.File, result.Line, SubjectOf(result))
                {
                    TierOverride = "G",
                });
            }
            else if (result.Field(RecordFields.Outcome) == "refusal")
            {
                var code = result.Field(RecordFields.Code);
                var message = code is { Length: > 0 }
                    ? $"Result '{result.Id}' names code '{code}', which is shown by no scenario."
                    : $"Result '{result.Id}' is shown by no scenario.";

                findings.Add(new Finding(
                    "result-scenario-missing",
                    message,
                    result.File, result.Line, SubjectOf(result)));
            }
        }
    }
}
