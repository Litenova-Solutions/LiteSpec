using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.ProseText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Intent;

/// <summary>The Intent checks: a problem is an outside pain with evidence, an actor is a person, organization or system named from a flow, a problem, an access position or a represented external system, and a goal is moved by a flow or a quality requirement.</summary>
/// <follows>litespec/core/intent#admit-only-outside-pains-as-problems</follows>
/// <follows>litespec/core/intent#grade-problem-evidence-by-kind</follows>
/// <follows>litespec/core/intent#name-every-actor-from-a-flow-a-problem-or-an-access-position</follows>
/// <follows>litespec/core/intent#move-every-goal-with-a-flow-or-a-quality-requirement</follows>
/// <follows>litespec/core/intent#define-each-recurring-capitalized-phrase</follows>
internal static class IntentChecks
{
    /// <summary>The field recording whether an actor belongs to the team building and running the product.</summary>
    private const string BelongsToTeam = "belongsToTeam";

    /// <summary>The field listing the evidence entries of a problem.</summary>
    private const string Evidence = "evidence";

    /// <summary>Runs every Intent check.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckProblems(model, findings);
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

    /// <summary>Reports each actor named by no flow, problem, access position or external system.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckActorsNamed(ProjectModel model, List<Finding> findings)
    {
        var named = new HashSet<string>(StringComparer.Ordinal);

        foreach (var flow in model.Records.Where(record => record.Type == RecordTypes.Flow))
        {
            foreach (var id in flow.Ids(RecordFields.Actor))
            {
                named.Add(id);
            }

            if (flow.Raw.ValueKind == JsonValueKind.Object
                && flow.Raw.TryGetProperty(RecordFields.Steps, out var steps)
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

        // A system actor stands for the external system that represents it, so the
        // representing system names it the way a flow names a person.
        foreach (var system in model.Records.Where(record => record.Type == RecordTypes.ExternalSystem))
        {
            foreach (var id in system.Ids(RecordFields.Represents))
            {
                named.Add(id);
            }
        }

        foreach (var actor in model.Records.Where(record => record.Type == RecordTypes.Actor
                     && !record.Derived))
        {
            if (!named.Contains(actor.Id))
            {
                findings.Add(new Finding(
                    "actor-unused",
                    $"Actor '{actor.Id}' is named by no flow, problem, access position or external system.",
                    actor.File, actor.Line, SubjectOf(actor)));
            }
        }
    }

    /// <summary>Reports each goal moved by no flow and no quality requirement.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckGoalsMoved(ProjectModel model, List<Finding> findings)
    {
        var moved = new HashSet<string>(StringComparer.Ordinal);

        foreach (var flow in model.Records.Where(record => record.Type == RecordTypes.Flow))
        {
            foreach (var id in flow.Ids(RecordFields.Serves))
            {
                moved.Add(id);
            }
        }

        foreach (var requirement in model.Records.Where(record => record.Type == RecordTypes.Requirement
                     && record.Field(RecordFields.Kind) == "quality"))
        {
            foreach (var id in requirement.Ids(RecordFields.BasedOn))
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
                    $"Goal '{goal.Id}' is moved by no flow and no quality requirement.",
                    goal.File, goal.Line, SubjectOf(goal)));
            }
        }
    }

    /// <summary>Reports each capitalized phrase of two to four words named in three or more records that no glossary term defines, skipping sentence openers, months, currency codes, acronym pairs, cast names and phrases built from reaction titles.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckUndefinedPhrases(ProjectModel model, List<Finding> findings)
    {
        var defined = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var titles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reactionTitlePhrases = ReactionTitlePhrasesOf(model);

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

        // The cast names people, businesses and products that examples reuse; they are cast, not terms.
        foreach (var cast in model.Records.Where(record => RecordTypes.CastTypes.Contains(record.Type)))
        {
            foreach (var text in TextsOf(cast))
            {
                foreach (var phrase in PhrasesOf(text))
                {
                    defined.Add(phrase);
                }
            }
        }

        var castNames = CastNamesOf(model);

        var recordsOfPhrase = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var recordOfPhrase = new Dictionary<string, Record>(StringComparer.OrdinalIgnoreCase);

        // Research, constraints and decisions quote the outside world by its own names (laws,
        // vendors, places), which are not the product's vocabulary, so only product records count.
        foreach (var record in model.Records.Where(record => !record.Derived && !OutsideSourceTypes.Contains(record.Type)).OrderBy(record => record.Id, StringComparer.Ordinal))
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
            if (entry.Value.Count < 3 || defined.Contains(entry.Key) || titles.Contains(entry.Key)
                || reactionTitlePhrases.Contains(entry.Key) || ContainsCastName(entry.Key, castNames))
            {
                continue;
            }

            var record = recordOfPhrase[entry.Key];

            findings.Add(new Finding(
                "term-missing",
                $"Record '{record.Id}' names '{entry.Key}' in {entry.Value.Count} records, and no glossary term defines it.",
                record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Every two-to-four-word phrase built from the words of a reaction title.</summary>
    /// <param name="model">The loaded project model that holds every reaction record.</param>
    /// <returns>The phrases that a reaction title states, so a term check cannot mistake them for undefined vocabulary.</returns>
    private static HashSet<string> ReactionTitlePhrasesOf(ProjectModel model)
    {
        var phrases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var reaction in model.Records.Where(record => record.Type == RecordTypes.Reaction))
        {
            var words = WordToken.Matches(reaction.Title).Select(match => match.Value).ToList();

            for (var at = 0; at < words.Count; at++)
            {
                for (var length = 2; length <= 4 && at + length <= words.Count; length++)
                {
                    phrases.Add(string.Join(" ", words.Skip(at).Take(length)));
                }
            }
        }

        return phrases;
    }

    /// <summary>Every prose text of a record: its written string values.</summary>
    /// <param name="record">The record whose prose is read.</param>
    /// <returns>The prose texts of the record.</returns>
    private static List<string> TextsOf(Record record)
    {
        var texts = new List<string>();

        CollectStrings(record.Raw, texts);

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

    /// <summary>Every explanation section name of every record type, which reads as a heading, not a term.</summary>
    private static readonly HashSet<string> SectionNames = new(
        ExplanationSections.ByType.Values.SelectMany(sections => sections.Select(section => section.Name)),
        StringComparer.OrdinalIgnoreCase);

    /// <summary>Sentence openers and function words that never open a term.</summary>
    private static readonly HashSet<string> SentenceOpeners = new(
        [
            "When", "On", "If", "Otherwise", "Its", "Then", "After", "Before", "Once",
            "Every", "Each", "This", "That", "These", "Those", "A", "An", "The",
            "And", "Or", "But", "So", "For", "With", "From", "To", "In", "At", "By",
        ],
        StringComparer.OrdinalIgnoreCase);

    /// <summary>The record types that quote outside sources by name, which term-missing does not read.</summary>
    private static readonly HashSet<string> OutsideSourceTypes = new(StringComparer.Ordinal)
    {
        RecordTypes.Research, RecordTypes.Constraint, RecordTypes.Decision,
    };

    /// <summary>Month names, which date a sentence rather than name a term.</summary>
    private static readonly HashSet<string> MonthNames = new(
        [
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December",
        ],
        StringComparer.OrdinalIgnoreCase);

    /// <summary>ISO 4217 currency codes, which state a currency rather than name a term.</summary>
    private static readonly HashSet<string> CurrencyCodes = new(
        [
            "AED", "AFN", "ALL", "AMD", "ANG", "AOA", "ARS", "AUD", "AWG", "AZN",
            "BAM", "BBD", "BDT", "BGN", "BHD", "BIF", "BMD", "BND", "BOB", "BOV",
            "BRL", "BSD", "BTN", "BWP", "BYN", "BZD", "CAD", "CDF", "CHE", "CHF",
            "CHW", "CLF", "CLP", "CNY", "COP", "COU", "CRC", "CUC", "CUP", "CVE",
            "CZK", "DJF", "DKK", "DOP", "DZD", "EGP", "ERN", "ETB", "EUR", "FJD",
            "FKP", "GBP", "GEL", "GGP", "GHS", "GIP", "GMD", "GNF", "GTQ", "GYD",
            "HKD", "HNL", "HTG", "HUF", "IDR", "ILS", "IMP", "INR", "IQD", "IRR",
            "ISK", "JEP", "JMD", "JOD", "JPY", "KES", "KGS", "KHR", "KMF", "KPW",
            "KRW", "KWD", "KYD", "KZT", "LAK", "LBP", "LKR", "LRD", "LSL", "LYD",
            "MAD", "MDL", "MGA", "MKD", "MMK", "MNT", "MOP", "MRU", "MUR", "MVR",
            "MWK", "MXN", "MXV", "MYR", "MZN", "NAD", "NGN", "NIO", "NOK", "NPR",
            "NZD", "OMR", "PAB", "PEN", "PGK", "PHP", "PKR", "PLN", "PYG", "QAR",
            "RON", "RSD", "RUB", "RWF", "SAR", "SBD", "SCR", "SDG", "SEK", "SGD",
            "SHP", "SLE", "SOS", "SRD", "SSP", "STN", "SVC", "SYP", "SZL", "THB",
            "TJS", "TMT", "TND", "TOP", "TRY", "TTD", "TWD", "TZS", "UAH", "UGX",
            "USD", "USN", "UYI", "UYU", "UZS", "VED", "VES", "VND", "VUV", "WST",
            "XAF", "XAG", "XAU", "XBA", "XBB", "XBC", "XBD", "XCD", "XDR", "XOF",
            "XPD", "XPF", "XPT", "XSU", "XUA", "YER", "ZAR", "ZMW", "ZWG",
        ],
        StringComparer.Ordinal);

    /// <summary>The capitalized phrases of two to four words in one text, with a leading article dropped. A phrase never spans a sentence stop, a colon, a semicolon, a pipe or a line break, and a word touching a digit or a slash joins no phrase.</summary>
    /// <param name="text">The prose text to scan.</param>
    /// <returns>The capitalized phrases in the text.</returns>
    private static List<string> PhrasesOf(string text)
    {
        var phrases = new List<string>();
        var run = new List<string>();
        var end = 0;

        foreach (Match match in WordToken.Matches(text))
        {
            if (BreaksRun(text[end..match.Index]))
            {
                AddPhrase(phrases, run);
                run = [];
            }

            end = match.Index + match.Length;

            if (JoinsNothing(text, match.Index, match.Length))
            {
                AddPhrase(phrases, run);
                run = [];
                continue;
            }

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

    /// <summary>Whether the gap between two words ends the run: it holds a sentence stop, a colon, a semicolon, a pipe or a line break.</summary>
    /// <param name="gap">The text between the end of one word and the start of the next.</param>
    /// <returns>True when the gap ends the run.</returns>
    private static bool BreaksRun(string gap) =>
        gap.IndexOfAny(['.', '?', '!', ':', ';', '|', '\n', '\r']) >= 0;

    /// <summary>Whether the word joins no phrase: it touches a digit or a slash, so it is a code or a path, not prose.</summary>
    /// <param name="text">The prose text holding the word.</param>
    /// <param name="start">The word's start offset in the text.</param>
    /// <param name="length">The word's length.</param>
    /// <returns>True when the word touches a digit or a slash.</returns>
    private static bool JoinsNothing(string text, int start, int length)
    {
        if (start > 0 && (char.IsDigit(text[start - 1]) || text[start - 1] == '/'))
        {
            return true;
        }

        var after = start + length;

        return after < text.Length && (char.IsDigit(text[after]) || text[after] == '/');
    }

    /// <summary>Adds one run as a phrase when it holds two or more words after its article drops, unless it opens with a sentence opener, holds a month, a currency code or an acronym pair, or names an explanation section.</summary>
    /// <param name="phrases">The list receiving the phrase.</param>
    /// <param name="run">The capitalized words of one run.</param>
    private static void AddPhrase(List<string> phrases, List<string> run)
    {
        var words = run.SkipWhile(word => word is "The" or "A" or "An").ToList();

        if (words.Count < 2)
        {
            return;
        }

        if (SentenceOpeners.Contains(words[0]))
        {
            return;
        }

        if (words.Any(word => MonthNames.Contains(word) || CurrencyCodes.Contains(word)))
        {
            return;
        }

        if (HasAcronym(words))
        {
            return;
        }

        var phrase = string.Join(" ", words);

        if (!SectionNames.Contains(phrase))
        {
            phrases.Add(phrase);
        }
    }

    /// <summary>Whether the phrase holds an all-caps acronym, which names an outside standard, law or code rather than a product term.</summary>
    /// <param name="words">The capitalized words of one phrase.</param>
    /// <returns>True when a word is an all-caps acronym.</returns>
    private static bool HasAcronym(List<string> words)
    {
        foreach (var word in words)
        {
            if (word.Length >= 2 && word.All(char.IsUpper))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Every name the project's cast lists: each entry's name or title.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The cast names, without placeholders.</returns>
    private static HashSet<string> CastNamesOf(ProjectModel model)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in model.Records.Where(record => RecordTypes.CastTypes.Contains(record.Type)))
        {
            var name = entry.Field(RecordFields.Name) ?? entry.Title;

            if (name.Length > 0)
            {
                names.Add(name);
            }
        }

        names.RemoveWhere(name => name.Length == 0 || name.Contains('<') || name.Contains('>'));

        return names;
    }

    /// <summary>Whether the phrase contains a cast name as one word or as contiguous words.</summary>
    /// <param name="phrase">The candidate phrase.</param>
    /// <param name="castNames">The cast names.</param>
    /// <returns>True when a cast name sits inside the phrase.</returns>
    private static bool ContainsCastName(string phrase, HashSet<string> castNames)
    {
        if (castNames.Count == 0)
        {
            return false;
        }

        var words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var name in castNames)
        {
            var nameWords = name.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (nameWords.Length == 0 || nameWords.Length > words.Length)
            {
                continue;
            }

            for (var start = 0; start <= words.Length - nameWords.Length; start++)
            {
                var matches = true;

                for (var offset = 0; offset < nameWords.Length; offset++)
                {
                    if (!words[start + offset].Equals(nameWords[offset], StringComparison.OrdinalIgnoreCase))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    return true;
                }
            }
        }

        return false;
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
