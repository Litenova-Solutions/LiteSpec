using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The calculation checks: every calculation rule is shown by a scenario with an examples table.</summary>
/// <follows>litespec/core/domain-model#tabulate-every-calculation-with-examples</follows>
internal static class CalculationChecks
{
    /// <summary>Reports a calculation rule shown by no scenario examples table.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var rule in model.Records.Where(record => record.Type == RecordTypes.Rule && record.Field(RecordFields.Kind) == "calculation"))
        {
            var covered = GovernedUseCases(model, rule)
                .SelectMany(useCase => ScenariosOf(model, useCase))
                .Any(scenario => scenario.Status != RecordStatuses.Retired && HasExamples(scenario));

            if (!covered)
            {
                findings.Add(new Finding("calculation-table-missing", $"Rule '{rule.Id}' is a calculation with no scenario examples table.", rule.File, rule.Line, SubjectOf(rule)));
            }
        }
    }

    /// <summary>The use cases a rule governs: directly, or through the aggregate that holds them.</summary>
        /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="rule">The calculation rule whose governed use cases are collected.</param>
    /// <returns>The governed use cases in the order the rule lists them.</returns>
    private static List<Record> GovernedUseCases(ProjectModel model, Record rule)
    {
        var found = new List<Record>();

        foreach (var target in rule.Ids(RecordFields.Governs))
        {
            if (!model.ById.TryGetValue(target, out var governed))
            {
                continue;
            }

            if (governed.Type == RecordTypes.UseCase)
            {
                found.Add(governed);
            }
            else if (governed.Type == RecordTypes.Aggregate)
            {
                found.AddRange(model.Records.Where(record =>
                    record.Type == RecordTypes.UseCase && ModelBuilder.AggregateOf(model, record)?.Id == governed.Id));
            }
        }

        return found;
    }

    /// <summary>The scenarios on a use case page.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="useCase">The use case record whose scenario records are collected.</param>
    /// <returns>The scenario records held on the use case page, or an empty list when the use case identifier does not split.</returns>
    private static List<Record> ScenariosOf(ProjectModel model, Record useCase)
    {
        if (!Identifiers.TrySplit(useCase.Id, out _, out _, out var name))
        {
            return [];
        }

        return model.Records
            .Where(record => record.Type == RecordTypes.Scenario
                && Identifiers.TrySplit(record.Id, out _, out var home, out _)
                && home == name)
            .ToList();
    }

    /// <summary>Whether the scenario carries a non-empty examples table.</summary>
    /// <param name="scenario">The scenario record whose examples table is inspected.</param>
    /// <returns>True when the scenario holds a non-empty examples array; otherwise false.</returns>
    private static bool HasExamples(Record scenario) =>
        scenario.Raw.ValueKind == JsonValueKind.Object
        && scenario.Raw.TryGetProperty(RecordFields.Examples, out var value)
        && value.ValueKind == JsonValueKind.Array
        && value.GetArrayLength() > 0;
}
