using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The measured checks: every measured requirement names a check with a passing run, and a statement naming a number names a measure.</summary>
/// <follows>litespec/core/intent#state-the-measure-on-a-counted-requirement</follows>
internal static class MeasuredChecks
{
    /// <summary>Reports requirement-measure-missing.</summary>
    /// <param name="model">The loaded project model that holds every record, each measure run and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var requirement in model.Records.Where(record => record.Type == RecordTypes.Requirement && record.Field(RecordFields.Measure) is { Length: > 0 }))
        {
            var check = requirement.Field(RecordFields.Check);

            if (check is not { Length: > 0 })
            {
                findings.Add(new Finding("requirement-measure-missing", $"Requirement '{requirement.Id}' is measured but names no check.", requirement.File, requirement.Line, SubjectOf(requirement)));
            }
            else if (!model.Measures.Any(measure => measure.Passed && MeasureMatches(measure, check)))
            {
                findings.Add(new Finding("requirement-measure-missing", $"Requirement '{requirement.Id}' names check '{check}' with no passing run.", requirement.File, requirement.Line, SubjectOf(requirement)));
            }
        }

        CheckNumberedStatement(model, findings);
    }

    /// <summary>Reports a statement naming a number with no measure, at tier A.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckNumberedStatement(ProjectModel model, List<Finding> findings)
    {
        foreach (var requirement in model.Records.Where(record =>
            record.Type == RecordTypes.Requirement
            && record.Status != RecordStatuses.Retired
            && record.Field(RecordFields.Measure) is not { Length: > 0 }
            && record.Field(RecordFields.Statement) is { Length: > 0 } statement
            && NamesNumber(statement)))
        {
            findings.Add(new Finding("requirement-measure-missing", $"Requirement '{requirement.Id}' names a number with no measure.", requirement.File, requirement.Line, SubjectOf(requirement))
            {
                TierOverride = "A",
            });
        }
    }

    /// <summary>Whether the statement names a number: a digit outside any record citation.</summary>
    /// <param name="statement">The requirement statement text under inspection.</param>
    /// <returns>True when a digit appears outside any record citation or step fragment; otherwise false.</returns>
    private static bool NamesNumber(string statement)
    {
        var prose = Identifiers.Citation.Replace(statement, " ");
        prose = Identifiers.SharedCitation().Replace(prose, " ");
        prose = StepFragment.Replace(prose, " ");

        return prose.Any(char.IsDigit);
    }

    /// <summary>Matches a provision step fragment, whose digits name a step rather than a counted quantity.</summary>
    private static readonly Regex StepFragment = new(@"#[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*", RegexOptions.Compiled);

    /// <summary>Whether the tool result is the requirement's check: the check alone, or the tool and the check.</summary>
    /// <param name="measure">The tool result whose check name is compared.</param>
    /// <param name="check">The check name the requirement declares.</param>
    /// <returns>True when the result names the declared check alone or with its tool prefix; otherwise false.</returns>
    private static bool MeasureMatches(MeasureResult measure, string check) =>
        measure.Check.Equals(check, StringComparison.Ordinal)
        || $"{measure.Tool} {measure.Check}".Equals(check, StringComparison.Ordinal);
}
