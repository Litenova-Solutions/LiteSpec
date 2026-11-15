using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.RecordShape;

/// <summary>The prose checks, advisory: every record holds a summary, every goal, problem, requirement and rule holds a statement, and a Behavior statement names what happens rather than a vague word.</summary>
/// <follows>litespec/core/use-cases#summarize-every-use-case</follows>
/// <follows>litespec/core/access#write-a-summary-on-every-actor-and-role-page</follows>
/// <follows>litespec/core/model#state-what-happens-in-every-behavior-statement</follows>
internal static class ProseChecks
{
    /// <summary>The words that say something happens without saying what, as the Behavior page lists them.</summary>
    private static readonly Regex VagueWord = new(
        @"\b(handles?|handled|processes|process|manages?|various|appropriately|appropriate|properly)\b|\bas needed\b|\band so on\b|\betc\.",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>The Behavior record fields a vague word is judged in.</summary>
    private static readonly string[] BehaviorFields = [RecordFields.Statement, RecordFields.Meaning, RecordFields.Given, RecordFields.When, RecordFields.Then];

    /// <summary>Reports prose-missing.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records
            .Where(record => !record.Derived)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            // A requirement page carries no summary: its statement is its lead.
            if (!record.IsEntry && record.Type != RecordTypes.Requirement && record.Field(RecordFields.Summary) is not { Length: > 0 })
            {
                findings.Add(new Finding("prose-missing", $"{ProseLabel(record.Type)} '{record.Id}' holds no summary field.", record.File, record.Line, SubjectOf(record)));
            }

            if (record.Type is RecordTypes.Goal or RecordTypes.Vision or RecordTypes.Problem or RecordTypes.Requirement or RecordTypes.Rule
                && record.Field(RecordFields.Statement) is not { Length: > 0 })
            {
                findings.Add(new Finding("prose-missing", $"{ProseLabel(record.Type)} '{record.Id}' holds no statement field.", record.File, record.Line, SubjectOf(record)));
            }

            CheckVagueWords(record, findings);
        }
    }

    /// <summary>Reports a Behavior statement that names a vague word, which says something happens without saying what.</summary>
    /// <param name="record">The Behavior record whose statement, meaning and scenario steps are read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckVagueWords(Record record, List<Finding> findings)
    {
        if (record.Type is not (RecordTypes.Rule or RecordTypes.Result or RecordTypes.Scenario or RecordTypes.DomainEvent
            or RecordTypes.Reaction or RecordTypes.Job or RecordTypes.UseCase))
        {
            return;
        }

        foreach (var field in BehaviorFields)
        {
            if (record.Field(field) is not { Length: > 0 } text)
            {
                continue;
            }

            var match = VagueWord.Match(text);

            if (match.Success)
            {
                findings.Add(new Finding("statement-word-vague", $"{ProseLabel(record.Type)} '{record.Id}' states '{match.Value}' in {field}, which says something happens without saying what.", record.File, record.Line, SubjectOf(record)));
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
