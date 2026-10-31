using System.Text.Json;
using System.Text.RegularExpressions;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The value-style check on record values.</summary>
/// <follows>litespec/core/language#write-each-value-in-the-value-style</follows>
internal static class LanguageChecks
{
    /// <summary>Lower-case modal words a requirement or rule statement never holds as a whole word.</summary>
    static readonly Regex ModalWord = new(@"\b(must|shall|should|may|might|can|could|will|would|ought)\b", RegexOptions.Compiled);

    /// <summary>A reason that opens with the word it completes.</summary>
    static readonly Regex BecauseStart = new(@"^\s*because\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>A condition that opens with the word it completes.</summary>
    static readonly Regex WhenStart = new(@"^\s*(when|if)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>A refusal code inside prose: a lower-case dotted name such as orders.not-found.</summary>
    static readonly Regex RefusalCodeSpan = new(@"\b[a-z][a-z0-9]+\.[a-z][a-z0-9-]*[a-z0-9]\b", RegexOptions.Compiled);

    // The words that stay lowercase inside a Title Case title, unless first or
    // last: articles, coordinating conjunctions, and prepositions of four letters
    // or fewer. Every other word starts with a capital.
    /// <summary>The words that stay lowercase inside a Title Case title unless first or last.</summary>
    static readonly HashSet<string> TitleSmallWords = new(
        ["a", "an", "the", "and", "but", "for", "nor", "or", "so", "yet",
         "as", "at", "by", "from", "in", "into", "of", "off", "on", "onto",
         "over", "per", "to", "up", "upon", "via", "with"],
        StringComparer.Ordinal);

    /// <summary>Matches one word token of a title, keeping dotted and signed names whole.</summary>
    static readonly Regex TitleToken = new("[A-Za-z0-9][A-Za-z0-9'./+-]*", RegexOptions.Compiled);

    /// <summary>Matches a title token that passes through unchanged: a version number or an all-capitals token.</summary>
    static readonly Regex TitleAtypical = new("^(?:v?\\d|[A-Z0-9.]+$)", RegexOptions.Compiled);

    /// <summary>Matches a camel-case join inside a title token, which marks a code name rather than prose.</summary>
    static readonly Regex TitleCamel = new("[a-z][A-Z]|[A-Z][a-z]+[A-Z]", RegexOptions.Compiled);

    /// <summary>Matches a title token that opens with a capital letter.</summary>
    static readonly Regex TitleCapital = new("^[A-Z]", RegexOptions.Compiled);

    /// <summary>Reports value-invalid.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var actors = ActorTitles(model);

        foreach (var record in model.Records)
        {
            if (record.Derived)
            {
                continue;
            }

            ReportTitle(record, findings);
            ReportStatement(record, findings);
            ReportMeaning(record, findings);
            ReportReasons(record, findings);
            ReportConditions(record, findings);
            ReportQuestion(record, findings);
            ReportFailure(record, findings);
            ReportSteps(record, actors, findings);
            ReportExcept(record, findings);
            ReportQuestions(record, findings);
            ReportMeanings(record, findings);
        }
    }

    /// <summary>The written title of every actor, which opens a journey step beside The.</summary>
    /// <param name="model">The loaded project model that holds every actor record.</param>
    /// <returns>The written title of each actor that journey steps may open with.</returns>
    private static HashSet<string> ActorTitles(ProjectModel model)
    {
        var titles = new HashSet<string>(StringComparer.Ordinal);

        foreach (var actor in model.Records.Where(record => record.Type == RecordTypes.Actor))
        {
            var title = actor.Field(RecordFields.Title) is { Length: > 0 } written ? written : actor.Title;

            if (title.Length > 0 && !title.Contains('/'))
            {
                titles.Add(title);
            }
        }

        return titles;
    }

    /// <summary>The subject title of a record type: the type capitalized with spaces.</summary>
    /// <param name="type">The record type identifier, or empty for an untyped record.</param>
    /// <returns>The capitalized prose label for the type, or the generic record label when empty.</returns>
    private static string LanguageLabel(string type)
    {
        if (type.Length == 0)
        {
            return "Record";
        }

        var words = type.Replace('-', ' ');

        return char.ToUpperInvariant(words[0]) + words[1..];
    }

    /// <summary>Adds a value-invalid finding on the record.</summary>
    /// <param name="record">The record the finding is placed on.</param>
    /// <param name="label">The prose label of the record kind that opens the message.</param>
    /// <param name="subject">The record identifier quoted in the message.</param>
    /// <param name="detail">The value-style defect the message states.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ValueFinding(Record record, string label, string subject, string detail, List<Finding> findings) =>
        findings.Add(new Finding("value-invalid", $"{label} '{subject}' {detail}", record.File, record.Line, SubjectOf(record)));

    /// <summary>Adds a value-invalid finding on a journey step.</summary>
    /// <param name="record">The journey record that holds the step.</param>
    /// <param name="step">The step identifier quoted in the message.</param>
    /// <param name="detail">The value-style defect the message states.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void StepFinding(Record record, string step, string detail, List<Finding> findings) =>
        findings.Add(new Finding("value-invalid", $"Journey step '{step}' {detail}", record.File, record.Line, step));

    /// <summary>The article a field name takes in a finding message.</summary>
    /// <param name="word">The field name whose opening sound decides the article.</param>
    /// <returns>An when the word opens with a vowel letter; otherwise a.</returns>
    private static string AOrAn(string word) =>
        word.Length > 0 && "aeiouAEIOU".Contains(word[0]) ? "an" : "a";

    /// <summary>Reports a title outside Title Case or ending in a period.</summary>
    /// <param name="record">The record whose written title is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportTitle(Record record, List<Finding> findings)
    {
        // A missing title is record-invalid; the value style judges only a written one.
        if (record.Field(RecordFields.Title) is not { Length: > 0 } title)
        {
            return;
        }

        if (title.TrimEnd().EndsWith('.'))
        {
            ValueFinding(record, LanguageLabel(record.Type), record.Id, "has a title that ends in a period.", findings);
        }

        if (!TitleCase(title))
        {
            ValueFinding(record, LanguageLabel(record.Type), record.Id, "has a title that is not Title Case.", findings);
        }
    }

    /// <summary>
    ///     Whether a title uses Title Case: every word starts with a capital except
    ///     a small word in the middle, with version numbers, all-capitals tokens,
    ///     camel-case names and dotted names passing through.
    /// </summary>
    /// <param name="value">The title text under test.</param>
    /// <returns>True when each word follows the Title Case rule; otherwise false.</returns>
    private static bool TitleCase(string value)
    {
        var tokens = TitleToken.Matches(value).Select(match => match.Value).ToList();

        return tokens.Select((token, index) => (token, index)).All(item =>
        {
            if (TitleAtypical.IsMatch(item.token) || TitleCamel.IsMatch(item.token) || item.token.Contains('.'))
            {
                return true;
            }

            if (item.index > 0 && item.index < tokens.Count - 1 && TitleSmallWords.Contains(item.token.ToLowerInvariant()))
            {
                return !item.token.Any(char.IsUpper);
            }

            return TitleCapital.IsMatch(item.token);
        });
    }

    /// <summary>Reports a statement missing its period, or holding a lower-case modal.</summary>
    /// <param name="record">The record whose statement is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportStatement(Record record, List<Finding> findings)
    {
        if (record.Field(RecordFields.Statement) is not { Length: > 0 } statement)
        {
            return;
        }

        var label = LanguageLabel(record.Type);

        if (!statement.TrimEnd().EndsWith('.'))
        {
            ValueFinding(record, label, record.Id, "has a statement that does not end in a period.", findings);
        }

        // Only a requirement or a rule carries the modal test; a goal, a problem
        // and a step state an outcome, a pain or an act, where the period is the test.
        if (record.Type is RecordTypes.Requirement or RecordTypes.Rule)
        {
            var prose = BacktickSpan.Replace(statement, " ");

            foreach (var modal in ModalWord.Matches(prose).Select(match => match.Value).Distinct(StringComparer.Ordinal).OrderBy(word => word, StringComparer.Ordinal))
            {
                ValueFinding(record, label, record.Id, $"has a statement that uses the modal word '{modal}'.", findings);
            }
        }
    }

    /// <summary>Reports a meaning outside the definition form.</summary>
    /// <param name="record">The record whose meaning field is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportMeaning(Record record, List<Finding> findings)
    {
        if (record.Field(RecordFields.Meaning) is not { Length: > 0 } meaning)
        {
            return;
        }

        ReportMeaningText(record, LanguageLabel(record.Type), record.Id, "meaning", meaning, findings);
    }

    /// <summary>Reports a meaning that opens outside A, An or The, or runs over 25 words.</summary>
    /// <param name="record">The record the finding is placed on.</param>
    /// <param name="label">The prose label of the record kind that opens the message.</param>
    /// <param name="subject">The record identifier quoted in the message.</param>
    /// <param name="field">The meaning field description quoted in the message.</param>
    /// <param name="meaning">The definition text under test.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportMeaningText(Record record, string label, string subject, string field, string meaning, List<Finding> findings)
    {
        if (!OpensWithArticle(meaning))
        {
            ValueFinding(record, label, subject, $"has {AOrAn(field)} {field} that does not start with 'A', 'An' or 'The'.", findings);
        }

        if (meaning.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length > 25)
        {
            ValueFinding(record, label, subject, $"has {AOrAn(field)} {field} over 25 words.", findings);
        }
    }

    /// <summary>Whether the definition opens with A, An or The as its first word.</summary>
    /// <param name="meaning">The definition text under test.</param>
    /// <returns>True when the first word is A, An or The; otherwise false.</returns>
    private static bool OpensWithArticle(string meaning)
    {
        var trimmed = meaning.TrimStart();

        return trimmed.StartsWith("A ", StringComparison.Ordinal)
            || trimmed.StartsWith("An ", StringComparison.Ordinal)
            || trimmed.StartsWith("The ", StringComparison.Ordinal)
            || trimmed is "A" or "An" or "The";
    }

    /// <summary>Reports a rationale or *Because that opens with because.</summary>
    /// <param name="record">The record whose reason fields are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportReasons(Record record, List<Finding> findings)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in record.Raw.EnumerateObject())
        {
            if (property.Name != "rationale" && !property.Name.EndsWith("Because", StringComparison.Ordinal))
            {
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.String
                && property.Value.GetString() is { Length: > 0 } reason
                && BecauseStart.IsMatch(reason))
            {
                ValueFinding(record, LanguageLabel(record.Type), record.Id, $"has {AOrAn(property.Name)} {property.Name} that starts with 'because'.", findings);
            }
        }
    }

    /// <summary>Reports a *When or condition that opens with when or if.</summary>
    /// <param name="record">The record whose condition fields are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportConditions(Record record, List<Finding> findings)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in record.Raw.EnumerateObject())
        {
            // A scenario when is a Gherkin act, not a condition.
            if (property.Name != "condition" && !property.Name.EndsWith("When", StringComparison.Ordinal))
            {
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.String
                && property.Value.GetString() is { Length: > 0 } condition
                && WhenStart.Match(condition) is { Success: true } match)
            {
                var word = match.Groups[1].Value.ToLowerInvariant();
                ValueFinding(record, LanguageLabel(record.Type), record.Id, $"has {AOrAn(property.Name)} {property.Name} that starts with '{word}'.", findings);
            }
        }
    }

    /// <summary>Reports a question missing its question mark.</summary>
    /// <param name="record">The record whose question field is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportQuestion(Record record, List<Finding> findings)
    {
        if (record.Field(RecordFields.Question) is not { Length: > 0 } question)
        {
            return;
        }

        if (!question.TrimEnd().EndsWith('?'))
        {
            ValueFinding(record, LanguageLabel(record.Type), record.Id, "has a question that does not end in '?'.", findings);
        }
    }

    /// <summary>Reports a failure that carries a refusal code.</summary>
    /// <param name="record">The record whose failure field is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportFailure(Record record, List<Finding> findings)
    {
        if (record.Field(RecordFields.Failure) is not { Length: > 0 } failure)
        {
            return;
        }

        if (RefusalCodeSpan.IsMatch(failure))
        {
            ValueFinding(record, LanguageLabel(record.Type), record.Id, "has a failure that contains a refusal code.", findings);
        }
    }

    /// <summary>Reports a journey step whose statement misses its actor opening or period, or whose failure carries a code.</summary>
    /// <param name="record">The journey record whose steps are judged.</param>
    /// <param name="actors">The actor titles a step statement may open with.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportSteps(Record record, HashSet<string> actors, List<Finding> findings)
    {
        if (record.Type != RecordTypes.Journey
            || record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Steps, out var steps)
            || steps.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var step in steps.EnumerateArray())
        {
            if (step.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var id = Str(step, RecordFields.Id) is { Length: > 0 } stepId ? $"{record.Id}#{stepId}" : record.Id;

            if (Str(step, RecordFields.Statement) is { Length: > 0 } statement)
            {
                if (!statement.TrimEnd().EndsWith('.'))
                {
                    StepFinding(record, id, "has a statement that does not end in a period.", findings);
                }

                if (!OpensWithActor(statement, actors))
                {
                    StepFinding(record, id, "has a statement that starts with neither 'The' nor an actor.", findings);
                }
            }

            if (Str(step, RecordFields.Failure) is { Length: > 0 } failure && RefusalCodeSpan.IsMatch(failure))
            {
                StepFinding(record, id, "has a failure that contains a refusal code.", findings);
            }
        }
    }

    /// <summary>Whether the step opens with The or an actor title as its first words.</summary>
    /// <param name="statement">The step statement under test.</param>
    /// <param name="actors">The actor titles a step statement may open with.</param>
    /// <returns>True when the statement opens with The or an actor title as a whole word; otherwise false.</returns>
    private static bool OpensWithActor(string statement, HashSet<string> actors)
    {
        var trimmed = statement.TrimStart();

        if (trimmed.StartsWith("The", StringComparison.Ordinal)
            && (trimmed.Length == 3 || !char.IsLetterOrDigit(trimmed[3])))
        {
            return true;
        }

        return actors.Any(actor => trimmed.StartsWith(actor, StringComparison.Ordinal)
            && (trimmed.Length == actor.Length || !char.IsLetterOrDigit(trimmed[actor.Length])));
    }

    /// <summary>Reports an except condition that opens with when or if.</summary>
    /// <param name="record">The record whose exception entries are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportExcept(Record record, List<Finding> findings)
    {
        foreach (var entry in ExceptEntries(record))
        {
            if (Str(entry, RecordFields.Condition) is { Length: > 0 } condition
                && WhenStart.Match(condition) is { Success: true } match)
            {
                var word = match.Groups[1].Value.ToLowerInvariant();
                ValueFinding(record, LanguageLabel(record.Type), record.Id, $"has an except condition that starts with '{word}'.", findings);
            }
        }
    }

    /// <summary>Reports a question entry missing its question mark.</summary>
    /// <param name="record">The record whose question entries are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportQuestions(Record record, List<Finding> findings)
    {
        foreach (var entry in QuestionEntries(record))
        {
            if (Str(entry, RecordFields.Question) is { Length: > 0 } question && !question.TrimEnd().EndsWith('?'))
            {
                ValueFinding(record, LanguageLabel(record.Type), record.Id, "has a question that does not end in '?'.", findings);
            }
        }
    }

    /// <summary>Reports a nested meaning outside the definition form: a state, data, input, payload or output field meaning.</summary>
    /// <param name="record">The record whose nested meanings are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportMeanings(Record record, List<Finding> findings)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        ReportEntryMeanings(record, "states", "state", findings);
        ReportEntryMeanings(record, "data", "data", findings);
        ReportEntryMeanings(record, "input", "input", findings);
        ReportEntryMeanings(record, "payload", "payload field", findings);
        ReportOutputMeanings(record, findings);
    }

    /// <summary>Reports each meaning in the named array that opens outside A, An or The, or runs over 25 words.</summary>
    /// <param name="record">The record that holds the named entry array.</param>
    /// <param name="field">The array field whose entry meanings are judged.</param>
    /// <param name="kind">The entry kind description quoted in each finding.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportEntryMeanings(Record record, string field, string kind, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(field, out var entries) || entries.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (Str(entry, RecordFields.Meaning) is not { Length: > 0 } meaning)
            {
                continue;
            }

            var name = Str(entry, RecordFields.Name) is { Length: > 0 } entryName ? $" '{entryName}'" : "";
            ReportMeaningText(record, LanguageLabel(record.Type), record.Id, $"{kind}{name} meaning", meaning, findings);
        }
    }

    /// <summary>Reports each output meaning and visibleWhen outside the value style.</summary>
    /// <param name="record">The record whose output meanings and group conditions are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportOutputMeanings(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.Output, out var output))
        {
            return;
        }

        // A command lists its output fields; a query groups them under include.
        if (output.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in output.EnumerateArray())
            {
                ReportOutputField(record, entry, findings);
            }

            return;
        }

        if (output.ValueKind == JsonValueKind.Object
            && output.TryGetProperty(RecordFields.Include, out var include)
            && include.ValueKind == JsonValueKind.Object)
        {
            foreach (var group in include.EnumerateObject())
            {
                if (group.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (group.Value.TryGetProperty(RecordFields.Fields, out var fields) && fields.ValueKind == JsonValueKind.Array)
                {
                    foreach (var entry in fields.EnumerateArray())
                    {
                        ReportOutputField(record, entry, findings);
                    }
                }

                if (group.Value.TryGetProperty(RecordFields.VisibleWhen, out var visible)
                    && visible.ValueKind == JsonValueKind.String
                    && visible.GetString() is { Length: > 0 } condition
                    && WhenStart.Match(condition) is { Success: true } match)
                {
                    var word = match.Groups[1].Value.ToLowerInvariant();
                    ValueFinding(record, LanguageLabel(record.Type), record.Id, $"has an output group '{group.Name}' visibleWhen that starts with '{word}'.", findings);
                }
            }
        }
    }

    /// <summary>Reports one output field meaning outside the definition form.</summary>
    /// <param name="record">The record that holds the output field.</param>
    /// <param name="entry">The output field entry whose meaning is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportOutputField(Record record, JsonElement entry, List<Finding> findings)
    {
        if (entry.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (Str(entry, RecordFields.Meaning) is not { Length: > 0 } meaning)
        {
            return;
        }

        var name = Str(entry, RecordFields.Name) is { Length: > 0 } fieldName ? $" '{fieldName}'" : "";
        ReportMeaningText(record, LanguageLabel(record.Type), record.Id, $"output{name} meaning", meaning, findings);
    }
}
