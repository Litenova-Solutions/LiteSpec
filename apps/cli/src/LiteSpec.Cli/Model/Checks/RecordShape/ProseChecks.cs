using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.RecordShape;

/// <summary>The prose check: every record holds a summary, and every goal, problem, requirement and rule holds a statement.</summary>
/// <follows>litespec/core/use-cases#write-prose-on-every-use-case</follows>
/// <follows>litespec/core/access#write-a-summary-on-every-actor-and-role-page</follows>
internal static class ProseChecks
{
    /// <summary>Reports prose-missing.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records
            .Where(record => !record.Derived)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            if (!record.IsEntry && record.Field(RecordFields.Summary) is not { Length: > 0 })
            {
                findings.Add(new Finding("prose-missing", $"{ProseLabel(record.Type)} '{record.Id}' holds no summary field.", record.File, record.Line, SubjectOf(record)));
            }

            if (record.Type is RecordTypes.Goal or RecordTypes.Problem or RecordTypes.Requirement or RecordTypes.Rule
                && record.Field(RecordFields.Statement) is not { Length: > 0 })
            {
                findings.Add(new Finding("prose-missing", $"{ProseLabel(record.Type)} '{record.Id}' holds no statement field.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>
    ///     The prose label a prose finding shows for a record type: the type in
    ///     words with its first letter turned, such as Use case.
    /// </summary>
    /// <param name="type">The record type identifier.</param>
    /// <returns>The capitalized prose label, or the generic record label when the type holds none.</returns>
    private static string ProseLabel(string type)
    {
        if (type.Length == 0)
        {
            return "Record";
        }

        var words = type.Replace('-', ' ');

        return char.ToUpper(words[0], System.Globalization.CultureInfo.InvariantCulture) + words[1..];
    }
}
