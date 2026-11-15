using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The rule binds check: a rule whose governs names more than one aggregate names every governed aggregate in binds.</summary>
/// <follows>litespec/core/domain-model#name-every-spanned-aggregate-in-binds</follows>
internal static class RuleBindsChecks
{
    /// <summary>Reports a rule spanning aggregates that names none or not all of them in binds.</summary>
    /// <param name="model">The loaded project model that holds every rule record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var rule in model.Records
            .Where(record => record.Type == RecordTypes.Rule)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            // Governs names aggregates and use cases; only the aggregate ids decide the span.
            var governed = rule.Ids(RecordFields.Governs)
                .Where(id => Identifiers.TrySplit(id, out var prefix, out _, out _) && prefix == RecordTypes.Aggregate)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList();

            if (governed.Count < 2)
            {
                continue;
            }

            var binds = rule.Ids(RecordFields.Binds).ToHashSet(StringComparer.Ordinal);

            if (governed.All(binds.Contains))
            {
                continue;
            }

            var bound = binds.Count == 0
                ? "nothing"
                : $"'{string.Join("', '", binds.Order(StringComparer.Ordinal))}'";

            findings.Add(new Finding("rule-binds-missing", $"Rule '{rule.Id}' spans '{string.Join("', '", governed)}' but binds {bound}.", rule.File, rule.Line, SubjectOf(rule)));
        }
    }
}
