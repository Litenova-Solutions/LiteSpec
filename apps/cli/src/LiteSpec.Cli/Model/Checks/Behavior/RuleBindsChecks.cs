using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The rule binds check: binds merged into governs, so a rule spanning more than one aggregate or store already names every governed aggregate or store in governs.</summary>
/// <follows>litespec/core/domain-model#name-every-spanned-aggregate-in-binds</follows>
internal static class RuleBindsChecks
{
    /// <summary>Reports a rule spanning aggregates or stores that names none or not all of them in binds.</summary>
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
                .Where(id => Identifiers.TrySplit(id, out var prefix, out _, out _) && prefix is RecordTypes.Aggregate)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList();

            if (governed.Count < 2)
            {
                continue;
            }

            // Binds merged into governs: the governed aggregates are their own span, so the check always holds.
            var binds = rule.Ids(RecordFields.Governs).ToHashSet(StringComparer.Ordinal);

            if (governed.All(binds.Contains))
            {
                continue;
            }

            var bound = binds.Count == 0
                ? "nothing"
                : $"'{string.Join("', '", binds.Order(StringComparer.Ordinal))}'";

            findings.Add(new Finding("rule-binds-missing", $"Rule '{rule.Id}' spans '{string.Join("', '", governed)}' but governs {bound}.", rule.File, rule.Line, SubjectOf(rule)));
        }
    }
}
