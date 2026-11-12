using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Intent;

/// <summary>The requirement check: each exception a requirement names has a fallback, and the fallback carries no exception of its own.</summary>
/// <follows>litespec/core/intent#state-each-exception-with-its-fallback</follows>
internal static class RequirementChecks
{
    /// <summary>Reports a requirement naming an except with no fallback, or a fallback carrying its own except.</summary>
    /// <param name="model">The loaded project model that holds every requirement record and fallback target.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var requirement in model.Records.Where(record => record.Type == RecordTypes.Requirement))
        {
            foreach (var entry in ExceptEntries(requirement))
            {
                var fallbacks = LinkStrings(entry, RecordFields.Fallback);

                if (fallbacks.Count == 0)
                {
                    findings.Add(new Finding("except-fallback-missing", $"Requirement '{requirement.Id}' names an except with no fallback.", requirement.File, requirement.Line, SubjectOf(requirement)));
                    continue;
                }

                foreach (var cited in fallbacks.Order(StringComparer.Ordinal))
                {
                    // An unknown target is left to reference-unknown.
                    if (!model.ById.TryGetValue(cited, out var target))
                    {
                        continue;
                    }

                    if (target.Type == RecordTypes.Requirement && ExceptEntries(target).Count > 0)
                    {
                        findings.Add(new Finding("except-fallback-missing", $"Requirement '{requirement.Id}' names fallback '{cited}' which carries its own except.", requirement.File, requirement.Line, SubjectOf(requirement)));
                    }
                }
            }
        }
    }
}
