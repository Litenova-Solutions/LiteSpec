using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Model.Screens;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>
///     The screen blueprint checks: the primary action names an offered use case, every screen
///     is led to or starts its entry point, no screen ends with nothing done, interface text
///     uses glossary terms rather than their aliases, every refusal a screen can reach carries
///     its sentence, and every written refusal carries its sentence in each language its entry
///     points declare beside the default.
/// </summary>
/// <follows>litespec/core/ui-ia#declare-each-screen-job-and-archetype</follows>
/// <follows>litespec/core/edges#write-the-sentence-a-person-reads-on-each-refusal</follows>
internal static class UiBlueprintChecks
{
    /// <summary>The field naming the screens a person reaches from a screen in one step.</summary>
    private const string LeadsToField = "leadsTo";

    /// <summary>The field carrying the sentence a person reads when a result reaches them.</summary>
    private const string SentenceField = "sentence";

    /// <summary>The field carrying the other spellings of a glossary term.</summary>
    private const string AliasesField = "aliases";

    /// <summary>Reports screen-primary-action-unknown, screen-unreachable, screen-dead-end, wording-alias, refusal-sentence-missing and sentence-language-missing.</summary>
    /// <param name="model">The loaded project model that holds every screen, result and glossary term.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckPrimaryActions(model, findings);
        CheckUnreachable(model, findings);
        CheckDeadEnd(model, findings);
        CheckWordingAliases(model, findings);
        CheckRefusalSentences(model, findings);
        CheckSentenceLanguages(model, findings);
    }

    /// <summary>Reports each screen no other screen leads to that is not its entry point's start.</summary>
    /// <param name="model">The loaded project model that holds every screen.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckUnreachable(ProjectModel model, List<Finding> findings)
    {
        var screens = ScreenRecords(model).OrderBy(screen => screen.Id, StringComparer.Ordinal).ToList();
        var ledTo = new HashSet<string>(screens.SelectMany(screen => screen.Ids(LeadsToField)), StringComparer.Ordinal);

        foreach (var screen in screens)
        {
            if (ledTo.Contains(screen.Id) || screen.Field(RecordFields.Host) is { Length: > 0 })
            {
                continue;
            }

            var point = FrontendRecords.EntryPointOf(screen);

            if (point is null || IsStart(screen))
            {
                continue;
            }

            findings.Add(new Finding(
                "screen-unreachable",
                $"Screen '{screen.Id}' is led to by no other screen and is not the start of entry point '{point}'.",
                screen.File, screen.Line, SubjectOf(screen)));
        }
    }

    /// <summary>Whether the screen starts its entry point: its address is the root or the sign-in route.</summary>
    /// <param name="screen">The screen record whose address is read.</param>
    /// <returns>True when the address is / or its last segment is sign-in; otherwise false.</returns>
    private static bool IsStart(Record screen)
    {
        if (screen.Field(RecordFields.Route) is not { Length: > 0 } address)
        {
            return false;
        }

        if (address.Trim() == "/")
        {
            return true;
        }

        var segments = address.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return segments.Length > 0 && segments[^1].Equals("sign-in", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Reports each screen that leads nowhere and offers no use case.</summary>
    /// <param name="model">The loaded project model that holds every screen.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckDeadEnd(ProjectModel model, List<Finding> findings)
    {
        foreach (var screen in ScreenRecords(model).OrderBy(screen => screen.Id, StringComparer.Ordinal))
        {
            if (screen.Ids(LeadsToField).Count == 0 && screen.Ids(RecordFields.Offers).Count == 0)
            {
                findings.Add(new Finding(
                    "screen-dead-end",
                    $"Screen '{screen.Id}' leads nowhere and offers no use case.",
                    screen.File, screen.Line, SubjectOf(screen)));
            }
        }
    }

    /// <summary>Reports each screen whose primary action names no use case the screen offers.</summary>
    /// <param name="model">The loaded project model that holds every screen.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPrimaryActions(ProjectModel model, List<Finding> findings)
    {
        foreach (var screen in ScreenRecords(model).OrderBy(screen => screen.Id, StringComparer.Ordinal))
        {
            if (screen.Field(RecordFields.PrimaryAction) is not { Length: > 0 } primary)
            {
                continue;
            }

            if (!screen.Ids(RecordFields.Offers).Contains(primary, StringComparer.Ordinal))
            {
                findings.Add(new Finding(
                    "screen-primary-action-unknown",
                    $"Screen '{screen.Id}' names '{primary}' as its primary action, which matches no use case it offers.",
                    screen.File, screen.Line, SubjectOf(screen)));
            }
        }
    }

    /// <summary>Reports each result sentence or glossary label using a glossary alias where no longer term covers the same words.</summary>
    /// <param name="model">The loaded project model that holds every result and glossary term.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckWordingAliases(ProjectModel model, List<Finding> findings)
    {
        var terms = model.Records.Where(record => record.Type == RecordTypes.Term).ToList();
        var titles = new HashSet<string>(
            terms.Select(term => term.Title).Where(title => title.Length > 0),
            StringComparer.OrdinalIgnoreCase);
        var aliases = new List<(string Alias, string Term, Regex Match)>();

        foreach (var term in terms.OrderBy(term => term.Id, StringComparer.Ordinal))
        {
            if (term.Title.Length == 0)
            {
                continue;
            }

            foreach (var alias in term.Strings(AliasesField))
            {
                if (alias.Trim().Length == 0 || titles.Contains(alias))
                {
                    continue;
                }

                aliases.Add((alias, term.Title, WordMatch(alias)));
            }
        }

        if (aliases.Count == 0)
        {
            return;
        }

        var shields = ShieldMatches(terms);

        CheckSentenceWording(model, aliases, shields, findings);
        CheckLabelWording(terms, aliases, shields, findings);
    }

    /// <summary>The whole-word matches of every glossary title and label: an alias match inside one of them reports nothing.</summary>
    /// <param name="terms">The glossary term records.</param>
    /// <returns>One whole-word match per distinct title and label, case-insensitive.</returns>
    private static List<Regex> ShieldMatches(List<Record> terms)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var shields = new List<Regex>();

        void Add(string phrase)
        {
            if (phrase.Trim().Length > 0 && seen.Add(phrase))
            {
                shields.Add(WordMatch(phrase));
            }
        }

        foreach (var term in terms.OrderBy(term => term.Id, StringComparer.Ordinal))
        {
            Add(term.Title);

            if (term.Raw.ValueKind != JsonValueKind.Object
                || !term.Raw.TryGetProperty(RecordFields.Labels, out var labels)
                || labels.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var label in labels.EnumerateObject())
            {
                if (label.Value.ValueKind == JsonValueKind.String && label.Value.GetString() is { } text)
                {
                    Add(text);
                }
            }
        }

        return shields;
    }

    /// <summary>A whole-word match for one glossary phrase, case-insensitive.</summary>
    /// <param name="phrase">The term, label or alias to match.</param>
    /// <returns>The whole-word match for the phrase.</returns>
    private static Regex WordMatch(string phrase) =>
        new(@"\b" + Regex.Escape(phrase) + @"\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Whether the alias matches the copy where no glossary term or label covers the same words.</summary>
    /// <param name="copy">The result sentence or glossary label under inspection.</param>
    /// <param name="alias">The whole-word match of the alias.</param>
    /// <param name="shields">The whole-word matches of every glossary title and label.</param>
    /// <returns>True when the alias matches at least once outside every term or label match; otherwise false.</returns>
    private static bool MatchesUncovered(string copy, Regex alias, List<Regex> shields)
    {
        foreach (Match hit in alias.Matches(copy))
        {
            var covered = false;

            foreach (var shield in shields)
            {
                foreach (Match cover in shield.Matches(copy))
                {
                    if (cover.Index <= hit.Index && hit.Index + hit.Length <= cover.Index + cover.Length)
                    {
                        covered = true;
                        break;
                    }
                }

                if (covered)
                {
                    break;
                }
            }

            if (!covered)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Reports each result sentence using a glossary alias where no longer term covers the same words.</summary>
    /// <param name="model">The loaded project model that holds every result.</param>
    /// <param name="aliases">The aliases with the term to use and the match for each.</param>
    /// <param name="shields">The whole-word matches of every glossary title and label.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSentenceWording(ProjectModel model, List<(string Alias, string Term, Regex Match)> aliases, List<Regex> shields, List<Finding> findings)
    {
        foreach (var result in model.Records
            .Where(record => record.Type == RecordTypes.Result && record.Field(SentenceField) is { Length: > 0 })
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            var sentence = result.Field(SentenceField)!;

            foreach (var (alias, term, match) in aliases)
            {
                if (MatchesUncovered(sentence, match, shields))
                {
                    findings.Add(new Finding(
                        "wording-alias",
                        $"Result '{result.Id}' carries '{alias}' in its sentence, an alias of glossary term '{term}'.",
                        result.File, result.Line, SubjectOf(result)));
                }
            }
        }
    }

    /// <summary>Reports each glossary label using a glossary alias where no longer term covers the same words.</summary>
    /// <param name="terms">The glossary term records.</param>
    /// <param name="aliases">The aliases with the term to use and the match for each.</param>
    /// <param name="shields">The whole-word matches of every glossary title and label.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckLabelWording(List<Record> terms, List<(string Alias, string Term, Regex Match)> aliases, List<Regex> shields, List<Finding> findings)
    {
        foreach (var term in terms.OrderBy(term => term.Id, StringComparer.Ordinal))
        {
            if (term.Raw.ValueKind != JsonValueKind.Object
                || !term.Raw.TryGetProperty(RecordFields.Labels, out var labels)
                || labels.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var label in labels.EnumerateObject())
            {
                if (label.Value.ValueKind != JsonValueKind.String || label.Value.GetString() is not { } text)
                {
                    continue;
                }

                foreach (var (alias, other, match) in aliases)
                {
                    if (MatchesUncovered(text, match, shields))
                    {
                        findings.Add(new Finding(
                            "wording-alias",
                            $"Glossary term '{term.Id}' carries '{alias}' in its '{label.Name}' label, an alias of glossary term '{other}'.",
                            term.File, term.Line, SubjectOf(term)));
                    }
                }
            }
        }
    }

    /// <summary>Reports each written refusal result of a use case a screen offers that carries no sentence.</summary>
    /// <param name="model">The loaded project model that holds every screen, use case and result.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRefusalSentences(ProjectModel model, List<Finding> findings)
    {
        var offered = new HashSet<string>(ScreenRecords(model).SelectMany(screen => screen.Ids(RecordFields.Offers)), StringComparer.Ordinal);

        foreach (var useCaseId in offered.Order(StringComparer.Ordinal))
        {
            if (!Identifiers.TrySplit(useCaseId, out _, out _, out var name) || name.Length == 0)
            {
                continue;
            }

            foreach (var result in model.RecordsByHome[name]
                .Where(result => result.Type == RecordTypes.Result && !result.Derived && result.Field(RecordFields.Outcome) == "refusal")
                .OrderBy(result => result.Id, StringComparer.Ordinal))
            {
                if (result.Field(SentenceField) is { Length: > 0 } sentence && sentence.Trim().Length > 0)
                {
                    continue;
                }

                findings.Add(new Finding(
                    "refusal-sentence-missing",
                    $"Result '{result.Id}' is a refusal of use case '{useCaseId}' a screen offers and carries no sentence.",
                    result.File, result.Line, SubjectOf(result)));
            }
        }
    }

    /// <summary>Reports each written refusal result missing a sentence for a language its use case's entry points declare beside the default, one finding per result naming every missing language.</summary>
    /// <param name="model">The loaded project model that holds every use case, entry point and result.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSentenceLanguages(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records
            .Where(record => record.Type == RecordTypes.UseCase && !record.Derived)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            if (!Identifiers.TrySplit(useCase.Id, out _, out _, out var name) || name.Length == 0)
            {
                continue;
            }

            var required = RequiredLanguages(model, useCase);

            if (required.Count == 0)
            {
                continue;
            }

            foreach (var result in model.RecordsByHome[name]
                .Where(result => result.Type == RecordTypes.Result && !result.Derived && result.Field(RecordFields.Outcome) == "refusal")
                .OrderBy(result => result.Id, StringComparer.Ordinal))
            {
                var present = SentencesOf(result);
                var missing = required.Keys
                    .Where(tag => !present.Contains(tag))
                    .Order(StringComparer.Ordinal)
                    .ToList();

                if (missing.Count == 0)
                {
                    continue;
                }

                findings.Add(new Finding(
                    "sentence-language-missing",
                    SentenceLanguageMessage(useCase, result, required, missing),
                    result.File, result.Line, SubjectOf(result)));
            }
        }
    }

    /// <summary>The message for a refusal result missing sentences: one missing language reads with its entry point, several join with each one's entry point.</summary>
    /// <param name="useCase">The use case record the result refuses.</param>
    /// <param name="result">The refusal result record missing sentences.</param>
    /// <param name="required">Each non-default language tag with the first entry point declaring it.</param>
    /// <param name="missing">The missing language tags in identifier order, holding at least one.</param>
    /// <returns>The finding message naming every missing language.</returns>
    private static string SentenceLanguageMessage(Record useCase, Record result, Dictionary<string, string> required, List<string> missing)
    {
        if (missing.Count == 1)
        {
            return $"Result '{result.Id}' is a refusal of use case '{useCase.Id}' reached through entry point '{required[missing[0]]}' and carries no '{missing[0]}' sentence.";
        }

        var parts = missing.Select(tag => $"'{tag}' through entry point '{required[tag]}'");

        return $"Result '{result.Id}' is a refusal of use case '{useCase.Id}' and carries no sentence in {string.Join(" or ", parts)}.";
    }

    /// <summary>The languages a use case's entry points declare beside each entry point's default, with one entry point naming each.</summary>
    /// <param name="model">The loaded project model that holds every entry point.</param>
    /// <param name="useCase">The use case record whose entry points are read.</param>
    /// <returns>Each non-default language tag with the first entry point declaring it.</returns>
    private static Dictionary<string, string> RequiredLanguages(ProjectModel model, Record useCase)
    {
        var required = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var pointId in useCase.Ids(RecordFields.ReachedThrough))
        {
            if (!model.ById.TryGetValue(pointId, out var point) || point.Type != RecordTypes.EntryPoint)
            {
                continue;
            }

            var languages = point.Strings(RecordFields.Languages);

            for (var index = 1; index < languages.Count; index++)
            {
                var tag = languages[index].Trim();

                if (tag.Length > 0)
                {
                    required.TryAdd(tag, pointId);
                }
            }
        }

        return required;
    }

    /// <summary>The language tags a result carries a non-empty sentence for.</summary>
    /// <param name="result">The result record whose sentences are read.</param>
    /// <returns>Each tag of sentences holding a non-empty sentence.</returns>
    private static HashSet<string> SentencesOf(Record result)
    {
        var present = new HashSet<string>(StringComparer.Ordinal);

        if (result.Raw.ValueKind != JsonValueKind.Object
            || !result.Raw.TryGetProperty(RecordFields.Sentences, out var sentences)
            || sentences.ValueKind != JsonValueKind.Object)
        {
            return present;
        }

        foreach (var entry in sentences.EnumerateObject())
        {
            if (entry.Value.ValueKind == JsonValueKind.String
                && entry.Value.GetString() is { Length: > 0 } text
                && text.Trim().Length > 0)
            {
                present.Add(entry.Name);
            }
        }

        return present;
    }
}
