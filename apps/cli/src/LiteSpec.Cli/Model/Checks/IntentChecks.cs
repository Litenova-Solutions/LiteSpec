using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingHelpers;

namespace LiteSpec.Cli.Model.Checks;

/// <summary>The Intent checks: a problem is an outside pain with evidence, an actor is a person or organization named from a journey, a problem or an access position, and a goal is moved by a journey or a quality requirement.</summary>
/// <follows>litespec/core/intent#admit-only-outside-pains-as-problems</follows>
/// <follows>litespec/core/intent#grade-problem-evidence-by-kind</follows>
/// <follows>litespec/core/intent#name-every-actor-from-a-journey-a-problem-or-an-access-position</follows>
/// <follows>litespec/core/intent#move-every-goal-with-a-journey-or-a-quality-requirement</follows>
/// <follows>litespec/core/intent#perform-each-journey-step-with-an-actor-or-a-system</follows>
/// <follows>litespec/core/intent#define-each-recurring-capitalized-phrase</follows>
internal static class IntentChecks
{
    /// <summary>The field recording whether an actor belongs to the team building and running the product.</summary>
    private const string BelongsToTeam = "belongsToTeam";

    /// <summary>The field listing the evidence entries of a problem.</summary>
    private const string Evidence = "evidence";

    /// <summary>The journey step field naming the system record performing the step when no person acts.</summary>
    private const string System = "system";

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

    /// <summary>Reports each problem breaking the admission or evidence rule.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckProblems(ProjectModel model, List<Finding> findings)
    {
        var problems = model.Records
            .Where(record => record.Type == RecordTypes.Problem && !record.Derived)
            .ToList();

        foreach (var problem in problems)
        {
            CheckPrimarySufferer(model, problem, findings);
            CheckEvidence(problem, findings);
        }
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

    /// <summary>Reports a problem whose evidence is only assumed, or absent.</summary>
    /// <param name="problem">The problem record under inspection.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckEvidence(Record problem, List<Finding> findings)
    {
        var strength = EvidenceStrengthOf(problem);

        if (strength is "assumed" or "none")
        {
            findings.Add(new Finding(
                "problem-evidence-missing",
                $"Problem '{problem.Id}' holds {strength} evidence and no observed or reported entry.",
                problem.File, problem.Line, SubjectOf(problem)));
        }
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
                     && !record.Derived))
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
                     && !record.Derived))
        {
            if (!moved.Contains(goal.Id))
            {
                findings.Add(new Finding(
                    "goal-mover-missing",
                    $"Goal '{goal.Id}' is moved by no journey and no quality requirement.",
                    goal.File, goal.Line, SubjectOf(goal))
                {
                    TierOverride = "G",
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
