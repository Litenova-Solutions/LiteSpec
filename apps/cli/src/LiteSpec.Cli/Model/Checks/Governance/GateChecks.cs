using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Governance;

/// <summary>The gate checks: every decision names what it decides or what it rests on, and a negating rule keeps its restriction in its name.</summary>
/// <follows>litespec/core/model#name-what-every-decision-decides</follows>
/// <follows>litespec/core/model#keep-rules-and-targets-out-of-technical-decisions</follows>
/// <follows>litespec/core/departures#record-accepted-gaps-as-departures</follows>
/// <follows>litespec/core/domain-model#keep-the-negation-word-in-the-rule-name</follows>
internal static class GateChecks
{
    /// <summary>Rule-name words that keep a restriction visible, so a negating statement needs no echo.</summary>
    private static readonly HashSet<string> RuleNameRestrictions = new(StringComparer.Ordinal)
    {
        "not", "no", "never", "cannot", "none", "nothing", "without", "unless", "only",
        "refused", "refuses", "refusal", "forbidden", "blocked", "rejected", "non",
    };

    /// <summary>Words that always open a subordinate clause, so a negation after one states scope, not the rule claim.</summary>
    private static readonly HashSet<string> SubordinateOpeners = new(StringComparer.OrdinalIgnoreCase)
    {
        "when", "where", "while", "if", "unless", "except", "which", "who", "whom", "whose",
        "because", "since", "although", "though", "whereas", "whether", "after", "before", "until", "once",
    };

    /// <summary>Words that open a subordinate clause past the segment subject, so the same words as the subject keep the main claim.</summary>
    private static readonly HashSet<string> MidSegmentOpeners = new(StringComparer.OrdinalIgnoreCase)
    {
        "that", "case", "cases",
    };

    /// <summary>Reports decision-decides-missing, decision-basis-missing and rule-name-invalid.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var decision in model.Records.Where(candidate =>
            candidate.Type == RecordTypes.Decision && !MarkChecks.UnderImplementationDecisions(model.Config, candidate.File)))
        {
            if (!decision.Ids(RecordFields.Decides).Any())
            {
                findings.Add(new Finding("decision-decides-missing", $"Decision '{decision.Id}' names nothing it decides.", decision.File, decision.Line, SubjectOf(decision)));
            }
        }

        foreach (var decision in model.Records.Where(candidate =>
            candidate.Type == RecordTypes.Decision && MarkChecks.UnderImplementationDecisions(model.Config, candidate.File)))
        {
            if (!decision.Ids(RecordFields.BasedOn).Any())
            {
                findings.Add(new Finding("decision-basis-missing", $"Decision '{decision.Id}' names no constraint or requirement in basedOn.", decision.File, decision.Line, SubjectOf(decision)));
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

            // Only the main claim counts: a negation inside a subordinate scope clause, such as
            // "cases this rule does not list", states scope, not the rule.
            if (StatesClaimNegation(statement) is { } word)
            {
                findings.Add(new Finding("rule-name-invalid", $"Rule '{rule.Id}' says '{word}' but its name keeps no restricting word.", rule.File, rule.Line, SubjectOf(rule)));
            }
        }
    }

    /// <summary>The first negation word the main claim states, or null when every negation sits in a subordinate scope clause.</summary>
    /// <param name="statement">The rule statement.</param>
    /// <returns>The negation word in lowercase, or null.</returns>
    static string? StatesClaimNegation(string statement)
    {
        foreach (Match match in Regex.Matches(statement, @"\b(not|no|never|cannot)\b", RegexOptions.IgnoreCase))
        {
            if (NegationStatesClaim(statement, match.Index))
            {
                return match.Value.ToLowerInvariant();
            }
        }

        return null;
    }

    /// <summary>Whether the negation at the index states the rule claim rather than a subordinate scope note.</summary>
    /// <param name="statement">The rule statement.</param>
    /// <param name="index">The index of the negation word.</param>
    /// <returns>True when no subordinate opener stands between the segment start and the negation.</returns>
    static bool NegationStatesClaim(string statement, int index)
    {
        var segmentStart = 0;

        for (var i = index - 1; i >= 0; i--)
        {
            if (statement[i] == ',' || statement[i] == ';' || statement[i] == ':')
            {
                segmentStart = i + 1;
                break;
            }
        }

        var before = statement.Substring(segmentStart, index - segmentStart);
        var words = Regex.Matches(before, @"[A-Za-z]+").Select(word => word.Value).ToList();

        if (words.Count == 0)
        {
            return true;
        }

        if (words.Any(SubordinateOpeners.Contains))
        {
            return false;
        }

        if (words.Skip(1).Any(MidSegmentOpeners.Contains))
        {
            return false;
        }

        var leading = before.Length - before.TrimStart().Length;

        foreach (Match self in Regex.Matches(before, @"\b(this|these)\s+rules?\b", RegexOptions.IgnoreCase))
        {
            if (self.Index > leading)
            {
                return false;
            }
        }

        return true;
    }
}
