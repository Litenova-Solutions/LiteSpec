using LiteSpec.Cli.Model.Checks.Behavior;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Proof;

/// <summary>The guard race check: every guarded command is proven against a competing command.</summary>
/// <follows>litespec/core/use-cases#prove-every-guard-against-a-competing-command</follows>
internal static class GuardRaceChecks
{
    /// <summary>Reports a guarded command with no scenario against a competing command.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        // Scenarios are a large-size type, so a small project proves its guards through tests alone.
        if (model.Config.Size != "large")
        {
            return;
        }

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "command"))
        {
            if (ConsistencyChecks.GuardsOf(useCase).Count == 0)
            {
                continue;
            }

            if (!ScenariosOf(model, useCase).Any(HasCompetitor))
            {
                findings.Add(new Finding(
                    "guard-race-scenario-missing",
                    $"Command '{useCase.Id}' guards an aggregate with no scenario against a competing command.",
                    useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>The scenarios on a use case page.</summary>
    /// <param name="model">The loaded project model that indexes records by the home their identifier names.</param>
    /// <param name="useCase">The use case record whose scenario records are collected.</param>
    /// <returns>The scenario records held on the use case page, or an empty list when the use case identifier does not split.</returns>
    private static IEnumerable<Record> ScenariosOf(ProjectModel model, Record useCase) =>
        Identifiers.TrySplit(useCase.Id, out _, out _, out var name)
            ? model.RecordsByHome[name].Where(record => record.Type == RecordTypes.Scenario)
            : [];

    /// <summary>Whether the scenario runs against a competing command.</summary>
    /// <param name="scenario">The scenario record whose competitor is inspected.</param>
    /// <returns>True when the scenario names a competing command; otherwise false.</returns>
    private static bool HasCompetitor(Record scenario) =>
        scenario.Ids(RecordFields.CompetesWith).Count > 0;
}
