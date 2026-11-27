using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.RecordShape;

/// <summary>The prose checks: every record except a requirement or a direction holds a summary, every goal, direction, problem, requirement and rule holds a statement, a Behavior statement names what happens rather than a vague word, a refusal message names only a way out the records provide and carries no refusal code, and a machine-read field carries no banned marketing or LiteSpec-ese phrase. The missing prose, vague words, leaked codes and banned phrases are reported by model review; the refusal next step blocks.</summary>
/// <follows>litespec/core/use-cases#summarize-every-use-case</follows>
/// <follows>litespec/core/model#state-what-happens-in-every-behavior-statement</follows>
/// <follows>litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence</follows>
/// <follows>litespec/core/writing#keep-raw-refusal-codes-out-of-product-copy</follows>
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

    /// <summary>The banned marketing and LiteSpec-ese phrases no machine-read field carries.</summary>
    private static readonly Regex BannedPhrase = new(
        @"\bseamlessly\b|\binstantly\b|\beffortlessly\b|\bunlock the power\b|\bgains a home\b|\brefuses other folders\b|\bthe homes of records\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>The multi-part raw tokens a refusal code leaks as: kebab-case or dotted words, and SCREAMING_SNAKE_CASE words.</summary>
    private static readonly Regex CodeToken = new(
        @"[A-Za-z0-9]+(?:[-.][A-Za-z0-9]+)+|[A-Z0-9]+(?:_[A-Z0-9]+)+",
        RegexOptions.CultureInvariant);

    /// <summary>The machine-read fields a banned phrase is judged in.</summary>
    private static readonly string[] MachineReadFields = [RecordFields.Statement, RecordFields.Summary, RecordFields.Meaning, RecordFields.Title, RecordFields.Given, RecordFields.When, RecordFields.Then, RecordFields.Message];

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
            CheckRefusalMessageCode(model, record, findings);
            CheckBannedPhrases(record, findings);
        }
    }

    /// <summary>
    ///     Reports a refusal message naming a use case, screen, or setting no record holds, or
    ///     promising another way out with an alternative cue. The cue is a heuristic: the message
    ///     names the way out in prose no check can resolve, so a person confirms the records provide it.
    /// </summary>
    /// <param name="model">The loaded project model that resolves the named records.</param>
    /// <param name="record">The result record whose refusal message is read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckNextStep(ProjectModel model, Record record, List<Finding> findings)
    {
        if (record.Type != RecordTypes.Result
            || record.Field(RecordFields.Outcome) != "refusal"
            || record.Field(RecordFields.Message) is not { Length: > 0 } sentence)
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
                findings.Add(new Finding("refusal-next-step-unknown", $"Result '{record.Id}' names '{id}' in its refusal message, which no record holds.", record.File, record.Line, SubjectOf(record)));
            }
        }

        var cue = AlternativeCue.Match(sentence);

        if (cue.Success)
        {
            findings.Add(new Finding("refusal-next-step-unknown", $"Result '{record.Id}' carries a refusal message promising another way out with '{cue.Value}'.", record.File, record.Line, SubjectOf(record)));
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

    /// <summary>Reports a refusal message or one of its translations naming a refusal code.</summary>
    /// <param name="model">The loaded project model that holds every declared refusal code.</param>
    /// <param name="record">The result record whose refusal message and translations are read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRefusalMessageCode(ProjectModel model, Record record, List<Finding> findings)
    {
        if (record.Type != RecordTypes.Result
            || record.Field(RecordFields.Outcome) != "refusal"
            || record.Field(RecordFields.Refusal) is not { Length: > 0 } refusal)
        {
            return;
        }

        var texts = new List<string>();

        if (record.Field(RecordFields.Message) is { Length: > 0 } message)
        {
            texts.Add(message);
        }

        if (record.Raw.ValueKind == JsonValueKind.Object
            && record.Raw.TryGetProperty(RecordFields.Translations, out var translations)
            && translations.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in translations.EnumerateObject())
            {
                if (entry.Value.ValueKind == JsonValueKind.String
                    && entry.Value.GetString() is { Length: > 0 } translation)
                {
                    texts.Add(translation);
                }
            }
        }

        if (texts.Count == 0)
        {
            return;
        }

        var declared = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in model.RefusalSets)
        {
            declared.Add(entry.Code);
        }

        foreach (var result in model.Records.Where(candidate => candidate.Type == RecordTypes.Result && candidate.Field(RecordFields.Outcome) == "refusal"))
        {
            if (RefusalCode.CodeOf(result.Field(RecordFields.Refusal)) is { Length: > 0 } code)
            {
                declared.Add(code);
            }
        }

        var own = RefusalCode.CodeOf(refusal);

        foreach (var text in texts)
        {
            if (LeakedCode(text, own, declared) is { Length: > 0 } leaked)
            {
                findings.Add(new Finding("refusal-code-shown", $"Result '{record.Id}' names refusal code '{leaked}' in its refusal message.", record.File, record.Line, SubjectOf(record)));
                return;
            }
        }
    }

    /// <summary>The refusal code a refusal message or translation leaks, or null when it names none.</summary>
    /// <param name="text">The message or translation under test.</param>
    /// <param name="own">The wire code the refusal names, or null when it names none.</param>
    /// <param name="declared">Every refusal code the model declares, in dotted form.</param>
    /// <returns>The leaked code in dotted form, or null.</returns>
    private static string? LeakedCode(string text, string? own, HashSet<string> declared)
    {
        if (own is { Length: > 0 } && text.Contains(own, StringComparison.Ordinal))
        {
            return own;
        }

        foreach (var code in declared.Order(StringComparer.Ordinal))
        {
            if (!string.Equals(code, own, StringComparison.Ordinal) && text.Contains(code, StringComparison.Ordinal))
            {
                return code;
            }
        }

        foreach (Match match in CodeToken.Matches(text))
        {
            foreach (var code in declared.Order(StringComparer.Ordinal))
            {
                if (TokenEqualsCode(match.Value, code))
                {
                    return code;
                }
            }
        }

        return null;
    }

    /// <summary>Whether a raw token spells a declared refusal code: its words match the code or the code's name.</summary>
    /// <param name="token">The raw kebab-case, dotted, or SCREAMING_SNAKE_CASE token.</param>
    /// <param name="code">The declared refusal code in dotted form.</param>
    /// <returns>True when the token spells the code or its name with separators and case ignored.</returns>
    private static bool TokenEqualsCode(string token, string code)
    {
        var words = token.ToLowerInvariant().Split(['.', '-', '_'], StringSplitOptions.RemoveEmptyEntries);

        if (words.Length < 2)
        {
            return false;
        }

        var parts = code.ToLowerInvariant().Split(['.', '-', '_'], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
            return false;
        }

        if (words.SequenceEqual(parts, StringComparer.Ordinal))
        {
            return true;
        }

        var name = parts[1..];

        return name.Length > 1 && words.SequenceEqual(name, StringComparer.Ordinal);
    }

    /// <summary>Reports a machine-read field carrying a banned marketing or LiteSpec-ese phrase.</summary>
    /// <param name="record">The record whose machine-read fields are read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckBannedPhrases(Record record, List<Finding> findings)
    {
        foreach (var field in MachineReadFields)
        {
            if (record.Field(field) is not { Length: > 0 } text)
            {
                continue;
            }

            var match = BannedPhrase.Match(text);

            if (match.Success)
            {
                findings.Add(new Finding("statement-phrase-vague", $"{ProseLabel(record.Type)} '{record.Id}' carries banned phrase '{match.Value}' in {field}.", record.File, record.Line, SubjectOf(record)));
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
