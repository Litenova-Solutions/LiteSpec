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
    private static IReadOnlyList<Record> GovernedUseCases(ProjectModel model, Record rule)
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
    private static IReadOnlyList<Record> ScenariosOf(ProjectModel model, Record useCase)
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
    private static bool HasExamples(Record scenario) =>
        scenario.Raw.ValueKind == JsonValueKind.Object
        && scenario.Raw.TryGetProperty(RecordFields.Examples, out var value)
        && value.ValueKind == JsonValueKind.Array
        && value.GetArrayLength() > 0;
}
