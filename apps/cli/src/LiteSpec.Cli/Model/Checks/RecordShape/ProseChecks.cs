using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.RecordShape;

/// <summary>The prose checks: every record except a requirement or a direction holds a summary, every goal, direction, problem, requirement and rule holds a statement, a Behavior statement names what happens rather than a vague word, and a refusal sentence names only a way out the records provide. The missing prose and vague words are reported by model review; the refusal next step blocks.</summary>
/// <follows>litespec/core/use-cases#summarize-every-use-case</follows>
/// <follows>litespec/core/model#state-what-happens-in-every-behavior-statement</follows>
/// <follows>litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence</follows>
internal static class ProseChecks
{
    /// <summary>The words that say something happens without saying what, as the Behavior page lists them.</summary>
    private static readonly Regex VagueWord = new(
        @"\b(handles?|handled|processes|process|manages?|various|appropriately|appropriate|properly)\b|\bas needed\b|\band so on\b|\betc\.",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>The record identifiers a refusal sentence names as its way out: a use case, a screen, or a setting. The match ends in a word character, so a sentence-ending period stays outside it.</summary>
    private static readonly Regex NamedRecord = new(
        @"\b(use-case|screen|setting)/[A-Za-z0-9](?:[A-Za-z0-9._-]*[A-Za-z0-9])?",
        RegexOptions.CultureInvariant);

    /// <summary>The phrases promising another way out: instead, alternatively, you can also, or or you can. An instead of names a form, not a way out.</summary>
    private static readonly Regex AlternativeCue = new(
        @"\binstead\b(?!\s+of\b)|\balternatively\b|\byou can also\b|\bor you can\b",
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
            // A requirement and a rule carry no summary: the statement is the lead. A domain event, a setting and a knowledge area open with their meaning. A direction summary stays optional.
            if (!record.IsEntry && record.Type is not (RecordTypes.Requirement or RecordTypes.Rule or RecordTypes.DomainEvent or RecordTypes.Setting or RecordTypes.KnowledgeArea or RecordTypes.Direction) && record.Field(RecordFields.Summary) is not { Length: > 0 })
            {
                findings.Add(new Finding("prose-missing", $"{ProseLabel(record.Type)} '{record.Id}' holds no summary field.", record.File, record.Line, SubjectOf(record)));
            }

            if (record.Type is RecordTypes.Goal or RecordTypes.Direction or RecordTypes.Problem or RecordTypes.Requirement or RecordTypes.Rule
                && record.Field(RecordFields.Statement) is not { Length: > 0 })
            {
                findings.Add(new Finding("prose-missing", $"{ProseLabel(record.Type)} '{record.Id}' holds no statement field.", record.File, record.Line, SubjectOf(record)));
            }

            CheckVagueWords(record, findings);
            CheckNextStep(model, record, findings);
        }
    }

    /// <summary>
    ///     Reports a refusal sentence naming a use case, screen, or setting no record holds, or
    ///     promising another way out with an alternative cue. The cue is a heuristic: the sentence
    ///     names the way out in prose no check can resolve, so a person confirms the records provide it.
    /// </summary>
    /// <param name="model">The loaded project model that resolves the named records.</param>
    /// <param name="record">The result record whose refusal sentence is read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckNextStep(ProjectModel model, Record record, List<Finding> findings)
    {
        if (record.Type != RecordTypes.Result
            || record.Field(RecordFields.Outcome) != "refusal"
            || record.Field(RecordFields.Sentence) is not { Length: > 0 } sentence)
        {
            return;
        }

        var named = new SortedSet<string>(StringComparer.Ordinal);

        foreach (Match match in NamedRecord.Matches(sentence))
        {
            named.Add(match.Value);
        }

        foreach (var id in named)
        {
            if (!model.ById.ContainsKey(id))
            {
                findings.Add(new Finding("refusal-next-step-unknown", $"Result '{record.Id}' names '{id}' in its refusal sentence, which no record holds.", record.File, record.Line, SubjectOf(record)));
            }
        }

        var cue = AlternativeCue.Match(sentence);

        if (cue.Success)
        {
            findings.Add(new Finding("refusal-next-step-unknown", $"Result '{record.Id}' carries a refusal sentence promising another way out with '{cue.Value}'.", record.File, record.Line, SubjectOf(record)));
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
