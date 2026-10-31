namespace LiteSpec.Model;

/// <summary>The result check: every written result is shown by a scenario.</summary>
/// <follows>litespec/core/behavior#prove-every-written-result-with-a-scenario</follows>
static partial class Checker
{
    /// <summary>Reports a written result shown by no scenario.</summary>
    private static void CheckResultWithoutScenario(ProjectModel model, List<Finding> findings)
    {
        // Scenarios are a large-size type, so a small project shows its results through tests alone.
        if (model.Config.Size != "large")
        {
            return;
        }

        var shown = model.Records
            .Where(record => record.Type == "scenario")
            .SelectMany(scenario => scenario.Ids("result"))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var result in model.Records.Where(record => record.Type == "result"))
        {
            if (result.Derived)
            {
                continue;
            }

            if (result.Status == "retired")
            {
                continue;
            }

            if (shown.Contains(result.Id))
            {
                continue;
            }

            if (result.Field("outcome") == "success")
            {
                findings.Add(new Finding(
                    "result-scenario-missing",
                    $"Result '{result.Id}' is shown by no scenario.",
                    result.File, result.Line, SubjectOf(result))
                {
                    TierOverride = "G",
                });
            }
            else if (result.Field("outcome") == "refusal")
            {
                var code = result.Field("code");
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
