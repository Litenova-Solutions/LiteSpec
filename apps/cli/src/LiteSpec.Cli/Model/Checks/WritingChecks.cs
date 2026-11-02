using System.Text.RegularExpressions;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>
///     The writing checks on record prose: title length, modal words in statements, and sentence length.
///     Title case and a final period on a title, and modal words inside a requirement or rule statement,
///     stay with <see cref="LanguageChecks" />, which already reports them as <c>value-invalid</c>;
///     this check adds the coverage that check does not hold, so no record earns two findings for one defect.
/// </summary>
/// <follows>litespec/core/writing#title-every-record-in-title-case</follows>
/// <follows>litespec/core/writing#state-each-statement-without-modal-words</follows>
/// <follows>litespec/core/writing#keep-each-record-sentence-within-twenty-five-words</follows>
internal static class WritingChecks
{
    /// <summary>The most words a record title holds.</summary>
    private const int TitleWordLimit = 8;

    /// <summary>The most words one sentence in a record prose field holds.</summary>
    private const int SentenceWordLimit = 25;

    /// <summary>The modal words a statement never holds as a whole word, read outside backticks.</summary>
    private static readonly Regex ModalWord = new(@"\b(must|should|shall|may|can|will)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>The prose fields whose sentences the length check reads; `description` names the journey description field.</summary>
    private static readonly string[] ProseFields = [RecordFields.Statement, RecordFields.Summary, RecordFields.Situation, "description"];

    /// <summary>Reports value-invalid for a long title or sentence, and field-invalid for a modal word in a statement.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records)
        {
            if (record.Derived)
            {
                continue;
            }

            CheckTitleLength(record, findings);
            CheckModal(record, findings);
            CheckSentenceLength(record, findings);
        }
    }

    /// <summary>The prose label a finding shows for a record type: the type capitalized with spaces.</summary>
    /// <param name="type">The record type identifier, or empty for an untyped record.</param>
    /// <returns>The capitalized prose label, or the generic record label when the type is empty.</returns>
    private static string LabelOf(string type)
    {
        if (type.Length == 0)
        {
            return "Record";
        }

        var words = type.Replace('-', ' ');

        return char.ToUpperInvariant(words[0]) + words[1..];
    }

    /// <summary>Reports a title over the word limit; a missing title is record-invalid elsewhere.</summary>
    /// <param name="record">The record whose written title is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckTitleLength(Record record, List<Finding> findings)
    {
        if (record.Field(RecordFields.Title) is not { Length: > 0 } title)
        {
            return;
        }

        var words = title.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

        if (words > TitleWordLimit)
        {
            findings.Add(new Finding(
                "value-invalid",
                $"{LabelOf(record.Type)} '{record.Id}' has a title of {words} words, over the limit of {TitleWordLimit}.",
                record.File,
                record.Line,
                SubjectOf(record)));
        }
    }

    /// <summary>
    ///     Reports each modal word in a statement outside a requirement or rule; the language check
    ///     already reports modal words inside a requirement or rule, so those types stop here.
    /// </summary>
    /// <param name="record">The record whose statement is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckModal(Record record, List<Finding> findings)
    {
        if (record.Type is RecordTypes.Requirement or RecordTypes.Rule)
        {
            return;
        }

        if (record.Field(RecordFields.Statement) is not { Length: > 0 } statement)
        {
            return;
        }

        var prose = BacktickSpan.Replace(statement, " ");
        var modals = ModalWord.Matches(prose)
            .Select(match => match.Value.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(word => word, StringComparer.Ordinal);

        foreach (var modal in modals)
        {
            findings.Add(new Finding(
                "field-invalid",
                $"{LabelOf(record.Type)} '{record.Id}' has a statement that uses the modal word '{modal}'.",
                record.File,
                record.Line,
                SubjectOf(record)));
        }
    }

    /// <summary>Reports the longest sentence of each prose field when it runs over the word limit.</summary>
    /// <param name="record">The record whose prose fields are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSentenceLength(Record record, List<Finding> findings)
    {
        foreach (var field in ProseFields)
        {
            if (record.Field(field) is not { Length: > 0 } text)
            {
                continue;
            }

            var prose = BacktickSpan.Replace(text, " ");
            var longest = prose
                .Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries)
                .Select(WordCount)
                .DefaultIfEmpty(0)
                .Max();

            if (longest > SentenceWordLimit)
            {
                findings.Add(new Finding(
                    "value-invalid",
                    $"{LabelOf(record.Type)} '{record.Id}' has a {field} sentence of {longest} words, over the limit of {SentenceWordLimit}.",
                    record.File,
                    record.Line,
                    SubjectOf(record)));
            }
        }
    }

    /// <summary>The words of one sentence: its whitespace tokens that hold a letter or a digit.</summary>
    /// <param name="sentence">The sentence text whose words are counted.</param>
    /// <returns>The count of tokens holding a letter or a digit.</returns>
    private static int WordCount(string sentence) =>
        sentence.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Count(token => token.Any(char.IsLetterOrDigit));
}
