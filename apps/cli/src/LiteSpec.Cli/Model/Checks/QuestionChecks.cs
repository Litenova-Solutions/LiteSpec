using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The question checks: no approved or reviewed record carries an open question, and an open question on a record without risk names a recommendation.</summary>
/// <follows>litespec/core/model#hold-approval-behind-open-questions</follows>
/// <follows>litespec/core/model#answer-each-question-on-its-record</follows>
internal static class QuestionChecks
{
    /// <summary>Reports an approved or reviewed record with open questions, and an open question naming no recommendation.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var record in model.Records)
        {
            if ((record.Status == RecordStatuses.Approved || record.Status == RecordStatuses.Reviewed) && HasOpenQuestions(record, model.Config, today))
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' is {record.Status} with open questions.", record.File, record.Line, SubjectOf(record)));
            }

            CheckQuestionRecommendation(record, findings);
        }
    }

    /// <summary>
    ///     Whether the record carries open questions: entries that are neither
    ///     answered nor auto-approved. An answered or auto-approved question is
    ///     closed, not open.
    /// </summary>
    /// <param name="record">The record whose questions are inspected.</param>
    /// <param name="config">The project configuration that supplies the auto-approval window.</param>
    /// <param name="today">The current date, used to decide whether a question has auto-approved.</param>
    /// <returns>True when at least one question on the record is neither answered nor auto-approved; otherwise false.</returns>
    private static bool HasOpenQuestions(Record record, ConsumerConfig config, DateOnly today) =>
        record.Raw.ValueKind == JsonValueKind.Object
        && record.Raw.TryGetProperty(RecordFields.Questions, out var value)
        && value.ValueKind == JsonValueKind.Array
        && value.EnumerateArray().Any(entry => !Answered(entry) && !AutoApproved(entry, record, config, today));

    /// <summary>Reports an open question naming no recommendation on a record carrying no risk.</summary>
    /// <param name="record">The record whose open questions are inspected.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckQuestionRecommendation(Record record, List<Finding> findings)
    {
        if (record.Status == RecordStatuses.Retired || record.Derived || HasRisk(record))
        {
            return;
        }

        foreach (var entry in QuestionEntries(record))
        {
            if (!Answered(entry) && Str(entry, RecordFields.Recommendation) is not { Length: > 0 })
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds a question that names no recommendation.", record.File, record.Line, SubjectOf(record)));
                break;
            }
        }
    }
}
