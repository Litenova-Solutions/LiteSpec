using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Governance;

/// <summary>The rule statement check: a statement past the length a single condition needs is a signal to split it.</summary>
/// <follows>litespec/core/domain-model#keep-each-rule-statement-short</follows>
internal static class RuleStatementChecks
{
    /// <summary>The longest statement, in characters, that the check accepts.</summary>
    internal const int MaximumLength = 600;

    /// <summary>Reports rule-statement-wide.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var rule in model.Records.Where(record => record.Type == RecordTypes.Rule))
        {
            if (rule.Field(RecordFields.Statement) is { Length: > MaximumLength } statement)
            {
                findings.Add(new Finding(
                    "rule-statement-wide",
                    $"Rule '{rule.Id}' states {statement.Length} characters, more than {MaximumLength}; split it into one rule per condition.",
                    rule.File,
                    rule.Line,
                    SubjectOf(rule)));
            }
        }
    }
}
