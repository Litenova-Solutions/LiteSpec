using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Proof;

/// <summary>
///     The scenario result check: a scenario ends in a result its use case declares, written or
///     derived.
/// </summary>
/// <follows>litespec/core/use-cases#declare-the-result-a-scenario-ends-in</follows>
internal static class ScenarioResultChecks
{
    /// <summary>The generic refusals the model derives, which no result record declares.</summary>
    private static readonly string[] Derived =
        ["forbidden", "invalid-input", "not-found", "conflict", "unauthenticated", "sign-in-too-old"];

    /// <summary>Reports each scenario ending in a result no record declares.</summary>
    /// <param name="model">The loaded project model that holds every scenario and result.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var scenario in model.Records.Where(record => record.Type == RecordTypes.Scenario && !record.Derived))
        {
            var resultIds = scenario.Ids(RecordFields.Result);
            var id = resultIds.Count > 0 ? resultIds[0] : null;

            if (id is null)
            {
                continue;
            }

            if (model.ById.TryGetValue(id, out var result) && result.Type == RecordTypes.Result)
            {
                continue;
            }

            if (Identifiers.TrySplit(id, out _, out _, out var name)
                && Derived.Contains(name, StringComparer.Ordinal))
            {
                continue;
            }

            findings.Add(new Finding("scenario-result-unknown",
                $"Scenario '{scenario.Id}' ends in '{id}', which its use case declares in no result.",
                scenario.File, scenario.Line, SubjectOf(scenario)));
        }
    }
}
