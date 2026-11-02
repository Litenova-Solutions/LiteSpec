using System.Text.Json;
using System.Text.RegularExpressions;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The Intent checks: a problem is an outside pain with cost, workaround and graded evidence, an actor is a person or organization named from a journey, a problem or an access position, and a goal is moved by a journey or a quality requirement.</summary>
/// <follows>litespec/core/intent#admit-only-outside-pains-as-problems</follows>
/// <follows>litespec/core/intent#state-the-cost-and-workaround-of-every-problem</follows>
/// <follows>litespec/core/intent#grade-problem-evidence-by-kind</follows>
/// <follows>litespec/core/intent#name-no-solution-in-a-problem-statement</follows>
/// <follows>litespec/core/intent#name-each-sibling-problem-or-merge-it</follows>
/// <follows>litespec/core/intent#name-every-actor-from-a-journey-a-problem-or-an-access-position</follows>
/// <follows>litespec/core/intent#move-every-goal-with-a-journey-or-a-quality-requirement</follows>
/// <follows>litespec/core/intent#perform-each-journey-step-with-an-actor-or-a-system</follows>
internal static class IntentChecks
{
    /// <summary>The field recording whether an actor belongs to the team building and running the product.</summary>
    private const string BelongsToTeam = "belongsToTeam";

    /// <summary>The field stating what a problem costs its sufferer today.</summary>
    private const string CostToday = "costToday";

    /// <summary>The field stating what a problem sufferer does instead today.</summary>
    private const string WorkaroundToday = "workaroundToday";

    /// <summary>The field listing the evidence entries of a problem.</summary>
    private const string Evidence = "evidence";

    /// <summary>The journey step field naming the system record performing the step when no person acts.</summary>
    private const string System = "system";

    /// <summary>Solution words a problem statement never names.</summary>
    private static readonly Regex SolutionWord = new(
        @"\b(build|add|provide|platform|feature|tool|app|dashboard)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Runs every Intent check.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckProblems(model, findings);
        CheckJourneySteps(model, findings);
        CheckActorsNamed(model, findings);
        CheckGoalsMoved(model, findings);
        CheckUndefinedPhrases(model, findings);
    }

    /// <summary>The computed evidence strength of a problem: the strongest evidence kind present, or none.</summary>
    /// <param name="record">The problem record whose evidence entries are graded.</param>
    /// <returns>Observed, reported or assumed when an entry carries that kind, else none.</returns>
    internal static string EvidenceStrengthOf(Record record)
    {
        var kinds = EvidenceKinds(record);

        if (kinds.Contains("observed"))
        {
            return "observed";
        }

        if (kinds.Contains("reported"))
        {
            return "reported";
        }

        return kinds.Contains("assumed") ? "assumed" : "none";
    }

    /// <summary>Reports each problem breaking the admission, cost, evidence, statement or sibling rule.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckProblems(ProjectModel model, List<Finding> findings)
    {
        var problems = model.Records
            .Where(record => record.Type == RecordTypes.Problem && !record.Derived && record.Status != RecordStatuses.Retired)
            .ToList();

        foreach (var problem in problems)
        {
            CheckPrimarySufferer(model, problem, findings);
            CheckCostAndWorkaround(problem, findings);
            CheckApprovalEvidence(problem, findings);
            CheckStatement(problem, findings);
        }

        CheckSiblingProblems(model, problems, findings);
    }

    /// <summary>Reports a problem whose primary sufferer is no person or organization outside the team.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="problem">The problem record under inspection.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPrimarySufferer(ProjectModel model, Record problem, List<Finding> findings)
    {
        var affects = problem.Ids(RecordFields.Affects);

        if (affects.Count == 0)
        {
            return;
        }

        if (!model.ById.TryGetValue(affects[0], out var sufferer) || sufferer.Type != RecordTypes.Actor)
        {
            return;
        }

        var kind = sufferer.Field(RecordFields.Kind);
        var onTeam = sufferer.Raw.ValueKind == JsonValueKind.Object
            && sufferer.Raw.TryGetProperty(BelongsToTeam, out var team)
            && team.ValueKind == JsonValueKind.True;

        if (kind is not ("person" or "organization") || onTeam)
        {
            findings.Add(new Finding(
                "problem-sufferer-invalid",
                $"Problem '{problem.Id}' names primary sufferer '{sufferer.Id}', which is no person or organization outside the team.",
                problem.File, problem.Line, SubjectOf(problem)));
        }
    }

    /// <summary>Reports a problem with no cost or no workaround, or one stating unknown with no open question.</summary>
    /// <param name="problem">The problem record under inspection.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckCostAndWorkaround(Record problem, List<Finding> findings)
    {
        var hasOpenQuestion = QuestionEntries(problem).Any(entry => !Answered(entry));

        if (MissingOrUnpriced(problem.Field(CostToday), hasOpenQuestion))
        {
            findings.Add(new Finding(
                "problem-cost-missing",
                $"Problem '{problem.Id}' states no cost today, or states unknown with no open question.",
                problem.File, problem.Line, SubjectOf(problem)));
        }

        if (MissingOrUnpriced(problem.Field(WorkaroundToday), hasOpenQuestion))
        {
            findings.Add(new Finding(
                "problem-workaround-missing",
                $"Problem '{problem.Id}' states no workaround today, or states unknown with no open question.",
                problem.File, problem.Line, SubjectOf(problem)));
        }
    }

    /// <summary>Whether a cost or workaround value is absent, or states unknown while no open question prices it.</summary>
    /// <param name="value">The cost or workaround text, or null when the field is missing.</param>
    /// <param name="hasOpenQuestion">Whether the record carries an open question.</param>
    /// <returns>True when the value is missing, or states unknown with no open question.</returns>
    private static bool MissingOrUnpriced(string? value, bool hasOpenQuestion)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.Trim().StartsWith("unknown", StringComparison.OrdinalIgnoreCase) && !hasOpenQuestion;
    }

    /// <summary>Reports an approved problem whose evidence is only assumed, or absent.</summary>
    /// <param name="problem">The problem record under inspection.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckApprovalEvidence(Record problem, List<Finding> findings)
    {
        if (problem.Status != RecordStatuses.Approved)
        {
            return;
        }

        var strength = EvidenceStrengthOf(problem);

        if (strength is "assumed" or "none")
        {
            findings.Add(new Finding(
                "problem-evidence-missing",
                $"Problem '{problem.Id}' is approved with {strength} evidence and no observed or reported entry.",
                problem.File, problem.Line, SubjectOf(problem)));
        }
    }

    /// <summary>Reports a problem statement naming a solution word.</summary>
    /// <param name="problem">The problem record under inspection.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckStatement(Record problem, List<Finding> findings)
    {
        var statement = problem.Field(RecordFields.Statement);

        if (statement is { Length: > 0 } && SolutionWord.Match(statement) is { Success: true } match)
        {
            findings.Add(new Finding(
                "problem-statement-invalid",
                $"Problem '{problem.Id}' names the solution word '{match.Value}' in its statement.",
                problem.File, problem.Line, SubjectOf(problem))
            {
                TierOverride = "A",
            });
        }
    }

    /// <summary>Reports two problems with the same primary sufferer cited from the same topic that name neither each other.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="problems">The problem records under inspection.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSiblingProblems(ProjectModel model, List<Record> problems, List<Finding> findings)
    {
        var topics = ProblemTopics(model);
        var bySufferer = problems
            .Where(problem => problem.Ids(RecordFields.Affects).Count > 0)
            .GroupBy(problem => problem.Ids(RecordFields.Affects)[0], StringComparer.Ordinal);

        foreach (var group in bySufferer)
        {
            var siblings = group.OrderBy(problem => problem.Id, StringComparer.Ordinal).ToList();

            for (var left = 0; left < siblings.Count; left++)
            {
                for (var right = left + 1; right < siblings.Count; right++)
                {
                    var first = siblings[left];
                    var second = siblings[right];
                    var shared = topics.GetValueOrDefault(first.Id, [])
                        .Intersect(topics.GetValueOrDefault(second.Id, []), StringComparer.Ordinal)
                        .ToList();

                    if (shared.Count == 0 || Names(first, second) || Names(second, first))
                    {
                        continue;
                    }

                    findings.Add(new Finding(
                        "problem-duplicate",
                        $"Problem '{second.Id}' shares primary sufferer '{group.Key}' and topic '{shared[0]}' with problem '{first.Id}' and names it nowhere.",
                        second.File, second.Line, SubjectOf(second))
                    {
                        TierOverride = "A",
                    });
                }
            }
        }
    }

    /// <summary>The topics citing each problem, from the requirements naming it in cites.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The topic names citing each problem id.</returns>
    private static Dictionary<string, HashSet<string>> ProblemTopics(ProjectModel model)
    {
        var topics = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var requirement in model.Records.Where(record => record.Type == RecordTypes.Requirement))
        {
            var parts = requirement.Id.Split('/', '.');

            if (parts.Length < 3)
            {
                continue;
            }

            foreach (var cited in requirement.Ids(RecordFields.Cites))
            {
                if (!cited.StartsWith("problem/", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!topics.TryGetValue(cited, out var names))
                {
                    names = new HashSet<string>(StringComparer.Ordinal);
                    topics[cited] = names;
                }

                names.Add(parts[1]);
            }
        }

        return topics;
    }

    /// <summary>Whether one problem names the other in its fields or its explanation.</summary>
    /// <param name="problem">The problem whose text is searched.</param>
    /// <param name="other">The problem whose id or title is searched for.</param>
    /// <returns>True when the problem names the other by id or by title.</returns>
    private static bool Names(Record problem, Record other)
    {
        var text = problem.Raw.GetRawText() + "\n" + (problem.Explanation ?? "");

        return text.Contains(other.Id, StringComparison.Ordinal)
            || (other.Title.Length > 0 && text.Contains(other.Title, StringComparison.Ordinal));
    }

    /// <summary>Reports each journey step naming both an actor and a system, or a system of the wrong type.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckJourneySteps(ProjectModel model, List<Finding> findings)
    {
        foreach (var journey in model.Records.Where(record => record.Type == RecordTypes.Journey && !record.Derived))
        {
            if (journey.Raw.ValueKind != JsonValueKind.Object
                || !journey.Raw.TryGetProperty(RecordFields.Steps, out var steps)
                || steps.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var step in steps.EnumerateArray())
            {
                if (step.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var hasActor = step.TryGetProperty(RecordFields.Actor, out var actor)
                    && actor.ValueKind == JsonValueKind.String;
                var hasSystem = step.TryGetProperty(System, out var system)
                    && system.ValueKind == JsonValueKind.String;

                if (hasActor && hasSystem)
                {
                    findings.Add(new Finding(
                        "field-invalid",
                        $"Journey '{journey.Id}' holds a step naming both an actor and a system.",
                        journey.File, journey.Line, SubjectOf(journey)));
                    continue;
                }

                if (hasSystem
                    && system.GetString() is { } systemId
                    && model.ById.TryGetValue(systemId, out var target)
                    && target.Type is not (RecordTypes.ExternalSystem or RecordTypes.Job or RecordTypes.Reaction))
                {
                    findings.Add(new Finding(
                        "field-invalid",
                        $"Journey '{journey.Id}' names system '{systemId}', which is a {target.Type}.",
                        journey.File, journey.Line, SubjectOf(journey)));
                }
            }
        }
    }

    /// <summary>Reports each actor named by no journey, problem or access position.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckActorsNamed(ProjectModel model, List<Finding> findings)
    {
        var named = new HashSet<string>(StringComparer.Ordinal);

        foreach (var journey in model.Records.Where(record => record.Type == RecordTypes.Journey))
        {
            foreach (var id in journey.Ids(RecordFields.Actor))
            {
                named.Add(id);
            }

            if (journey.Raw.ValueKind == JsonValueKind.Object
                && journey.Raw.TryGetProperty(RecordFields.Steps, out var steps)
                && steps.ValueKind == JsonValueKind.Array)
            {
                foreach (var step in steps.EnumerateArray())
                {
                    if (step.ValueKind == JsonValueKind.Object
                        && step.TryGetProperty(RecordFields.Actor, out var actor)
                        && actor.ValueKind == JsonValueKind.String
                        && actor.GetString() is { } actorId)
                    {
                        named.Add(actorId);
                    }
                }
            }
        }

        foreach (var problem in model.Records.Where(record => record.Type == RecordTypes.Problem))
        {
            foreach (var id in problem.Ids(RecordFields.Affects))
            {
                named.Add(id);
            }
        }

        foreach (var actor in model.Records.Where(record => record.Type == RecordTypes.Actor))
        {
            if (actor.Raw.ValueKind == JsonValueKind.Object
                && actor.Raw.TryGetProperty(RecordFields.Holds, out var holds)
                && holds.ValueKind == JsonValueKind.Array
                && holds.GetArrayLength() > 0)
            {
                named.Add(actor.Id);
            }
        }

        foreach (var actor in model.Records.Where(record => record.Type == RecordTypes.Actor
                     && !record.Derived && record.Status != RecordStatuses.Retired))
        {
            if (!named.Contains(actor.Id))
            {
                findings.Add(new Finding(
                    "actor-unused",
                    $"Actor '{actor.Id}' is named by no journey, problem or access position.",
                    actor.File, actor.Line, SubjectOf(actor))
                {
                    TierOverride = "A",
                });
            }
        }
    }

    /// <summary>Reports each goal moved by no journey and no quality requirement.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckGoalsMoved(ProjectModel model, List<Finding> findings)
    {
        var moved = new HashSet<string>(StringComparer.Ordinal);

        foreach (var journey in model.Records.Where(record => record.Type == RecordTypes.Journey))
        {
            foreach (var id in journey.Ids(RecordFields.Serves))
            {
                moved.Add(id);
            }
        }

        foreach (var requirement in model.Records.Where(record => record.Type == RecordTypes.Requirement
                     && record.Field(RecordFields.Kind) == "quality"))
        {
            foreach (var id in requirement.Ids(RecordFields.Cites))
            {
                moved.Add(id);
            }
        }

        foreach (var goal in model.Records.Where(record => record.Type == RecordTypes.Goal
                     && !record.Derived && record.Status != RecordStatuses.Retired))
        {
            if (!moved.Contains(goal.Id))
            {
                findings.Add(new Finding(
                    "goal-mover-missing",
                    $"Goal '{goal.Id}' is moved by no journey and no quality requirement.",
                    goal.File, goal.Line, SubjectOf(goal))
                {
                    TierOverride = "A",
                });
            }
        }
    }

    /// <summary>Reports each capitalized phrase of two to four words named in three or more records that no glossary term defines.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckUndefinedPhrases(ProjectModel model, List<Finding> findings)
    {
        var defined = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var titles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in model.Records)
        {
            if (record.Title.Length > 0)
            {
                titles.Add(record.Title);
            }

            if (record.Type == RecordTypes.Term && record.Title.Length > 0)
            {
                defined.Add(record.Title);
            }
        }

        var recordsOfPhrase = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var recordOfPhrase = new Dictionary<string, Record>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in model.Records.Where(record => !record.Derived).OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            var phrases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var text in TextsOf(record))
            {
                foreach (var phrase in PhrasesOf(text))
                {
                    phrases.Add(phrase);
                }
            }

            foreach (var phrase in phrases)
            {
                if (!recordsOfPhrase.TryGetValue(phrase, out var records))
                {
                    records = new HashSet<string>(StringComparer.Ordinal);
                    recordsOfPhrase[phrase] = records;
                    recordOfPhrase[phrase] = record;
                }

                records.Add(record.Id);
            }
        }

        foreach (var entry in recordsOfPhrase.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (entry.Value.Count < 3 || defined.Contains(entry.Key) || titles.Contains(entry.Key))
            {
                continue;
            }

            var record = recordOfPhrase[entry.Key];

            findings.Add(new Finding(
                "term-missing",
                $"Record '{record.Id}' names '{entry.Key}' in {entry.Value.Count} records, and no glossary term defines it.",
                record.File, record.Line, SubjectOf(record))
            {
                TierOverride = "A",
            });
        }
    }

    /// <summary>Every prose text of a record: its written string values and its explanation without headings.</summary>
    /// <param name="record">The record whose prose is read.</param>
    /// <returns>The prose texts of the record.</returns>
    private static List<string> TextsOf(Record record)
    {
        var texts = new List<string>();

        CollectStrings(record.Raw, texts);

        if (record.Explanation is { } explanation)
        {
            foreach (var line in explanation.Split('\n'))
            {
                if (!line.TrimStart().StartsWith('#'))
                {
                    texts.Add(line);
                }
            }
        }

        return texts;
    }

    /// <summary>Collects every string value of a JSON value, skipping code spans.</summary>
    /// <param name="value">The JSON value whose strings are collected.</param>
    /// <param name="texts">The list receiving each string value.</param>
    private static void CollectStrings(JsonElement value, List<string> texts)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                if (value.GetString() is { } text)
                {
                    texts.Add(BacktickSpan.Replace(text, " "));
                }

                break;

            case JsonValueKind.Array:
                foreach (var entry in value.EnumerateArray())
                {
                    CollectStrings(entry, texts);
                }

                break;

            case JsonValueKind.Object:
                foreach (var property in value.EnumerateObject())
                {
                    if (string.Equals(property.Name, "title", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    CollectStrings(property.Value, texts);
                }

                break;
        }
    }

    /// <summary>One word token inside prose.</summary>
    private static readonly Regex WordToken = new(@"[A-Za-z][a-zA-Z]*", RegexOptions.Compiled);

    /// <summary>The capitalized phrases of two to four words in one text, with a leading article dropped.</summary>
    /// <param name="text">The prose text to scan.</param>
    /// <returns>The capitalized phrases in the text.</returns>
    private static List<string> PhrasesOf(string text)
    {
        var phrases = new List<string>();
        var run = new List<string>();

        foreach (Match match in WordToken.Matches(text))
        {
            if (char.IsUpper(match.Value[0]))
            {
                run.Add(match.Value);

                if (run.Count == 4)
                {
                    AddPhrase(phrases, run);
                    run = [];
                }
            }
            else
            {
                AddPhrase(phrases, run);
                run = [];
            }
        }

        AddPhrase(phrases, run);

        return phrases;
    }

    /// <summary>Adds one run as a phrase when it holds two or more words after its article drops.</summary>
    /// <param name="phrases">The list receiving the phrase.</param>
    /// <param name="run">The capitalized words of one run.</param>
    private static void AddPhrase(List<string> phrases, List<string> run)
    {
        var words = run.SkipWhile(word => word is "The" or "A" or "An").ToList();

        if (words.Count >= 2)
        {
            phrases.Add(string.Join(" ", words));
        }
    }

    /// <summary>The evidence kinds a problem carries, one per evidence entry.</summary>
    /// <param name="record">The problem record whose evidence entries are read.</param>
    /// <returns>The kind of each evidence entry.</returns>
    private static HashSet<string> EvidenceKinds(Record record)
    {
        var kinds = new HashSet<string>(StringComparer.Ordinal);

        if (record.Raw.ValueKind == JsonValueKind.Object
            && record.Raw.TryGetProperty(Evidence, out var evidence)
            && evidence.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in evidence.EnumerateArray())
            {
                if (Str(entry, RecordFields.Kind) is { } kind)
                {
                    kinds.Add(kind);
                }
            }
        }

        return kinds;
    }
}
