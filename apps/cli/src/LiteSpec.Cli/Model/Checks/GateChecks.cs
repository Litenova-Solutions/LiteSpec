using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The gate checks: no record stands approved behind a draft decision, every decision names what it decides or what it rests on, exceptions expire, and a negating rule keeps its restriction in its name.</summary>
/// <follows>litespec/core/model#hold-approval-behind-draft-decisions</follows>
/// <follows>litespec/core/model#name-what-every-decision-decides</follows>
/// <follows>litespec/core/model#keep-rules-and-targets-out-of-technical-decisions</follows>
/// <follows>litespec/core/model#record-accepted-gaps-as-exceptions</follows>
/// <follows>litespec/core/domain-model#keep-the-negation-word-in-the-rule-name</follows>
internal static class GateChecks
{
    /// <summary>Rule-name words that keep a restriction visible, so a negating statement needs no echo.</summary>
    private static readonly HashSet<string> RuleNameRestrictions = new(StringComparer.Ordinal)
    {
        "not", "no", "never", "cannot", "none", "nothing", "without", "unless", "only",
        "refused", "refuses", "refusal", "forbidden", "blocked", "rejected", "non",
    };

    /// <summary>Reports approval-invalid, decision-decides-missing, decision-basis-missing, exception-expired and rule-name-invalid.</summary>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // No record auto-approves: only a question does, after its decide-by date
        // plus the veto window. Every draft decision holds what it decides.
        var drafts = model.Records
            .Where(record => record.Type == RecordTypes.Decision && record.Status == RecordStatuses.Draft)
            .SelectMany(decision => decision.Ids(RecordFields.Decides))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            if ((record.Status == RecordStatuses.Approved || record.Status == RecordStatuses.Reviewed) && drafts.Contains(record.Id))
            {
                findings.Add(new Finding("approval-invalid", $"Record '{record.Id}' is {record.Status} while its decision is a draft.", record.File, record.Line, SubjectOf(record)));
            }
        }

        foreach (var decision in model.Records.Where(candidate =>
            candidate.Type == RecordTypes.Decision && candidate.Status != RecordStatuses.Retired && !MarkChecks.UnderImplementationDecisions(model.Config, candidate.File)))
        {
            if (!decision.Ids(RecordFields.Decides).Any())
            {
                findings.Add(new Finding("decision-decides-missing", $"Decision '{decision.Id}' names nothing it decides.", decision.File, decision.Line, SubjectOf(decision)));
            }
        }

        foreach (var decision in model.Records.Where(candidate =>
            candidate.Type == RecordTypes.Decision && candidate.Status != RecordStatuses.Retired && MarkChecks.UnderImplementationDecisions(model.Config, candidate.File)))
        {
            if (!decision.Ids(RecordFields.BasedOn).Any())
            {
                findings.Add(new Finding("decision-basis-missing", $"Decision '{decision.Id}' names no constraint or requirement in basedOn.", decision.File, decision.Line, SubjectOf(decision)));
            }
        }

        foreach (var exception in model.Records.Where(record => record.Type == RecordTypes.Exception))
        {
            if (exception.Field(RecordFields.ExpiresOn) is { } expiresOn
                && IsoDate(expiresOn, out var expires)
                && today > expires)
            {
                findings.Add(new Finding("exception-expired", $"Exception '{exception.Id}' stands past its expiresOn {expiresOn}.", exception.File, exception.Line, SubjectOf(exception)));
            }
        }

        foreach (var rule in model.Records.Where(record => record.Type == RecordTypes.Rule))
        {
            if (rule.Field(RecordFields.Statement) is not { } statement)
            {
                continue;
            }

            if (!Identifiers.TrySplit(rule.Id, out _, out _, out var name))
            {
                continue;
            }

            // Any word that restricts keeps the negation: "no", "never" and "refused" say it
            // directly, and "only" or a uniqueness "one ... per" states a limit a reader cannot miss.
            var words = name.Split('-', StringSplitOptions.RemoveEmptyEntries);

            if (words.Any(RuleNameRestrictions.Contains) || (words.Contains("one") && words.Contains("per")))
            {
                continue;
            }

            var match = Regex.Match(statement, @"\b(not|no|never|cannot)\b", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                findings.Add(new Finding("rule-name-invalid", $"Rule '{rule.Id}' says '{match.Value.ToLowerInvariant()}' but its name keeps no restricting word.", rule.File, rule.Line, SubjectOf(rule)));
            }
        }
    }
}
