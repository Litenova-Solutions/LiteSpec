namespace LiteSpec.Model;

/// <summary>
///     The past tense of use-case verbs: the baseline table, the common irregulars, and the
///     derivation for project domain verbs. The verbs themselves live in <see cref="UseCaseVerbs" />.
/// </summary>
/// <follows>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</follows>
static class UseCaseVocabulary
{
    /// <summary>The past tense of every baseline verb, with the irregulars spelled out.</summary>
    public static readonly IReadOnlyDictionary<string, string> PastTense =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["read"] = "read",
            ["list"] = "listed",
            ["search"] = "searched",
            ["export"] = "exported",
            ["download"] = "downloaded",
            ["answer"] = "answered",
            ["draft"] = "drafted",
            ["summarize"] = "summarized",
            ["translate"] = "translated",
            ["create"] = "created",
            ["add"] = "added",
            ["remove"] = "removed",
            ["change"] = "changed",
            ["duplicate"] = "duplicated",
            ["open"] = "opened",
            ["close"] = "closed",
            ["start"] = "started",
            ["complete"] = "completed",
            ["publish"] = "published",
            ["withdraw"] = "withdrawn",
            ["pause"] = "paused",
            ["resume"] = "resumed",
            ["suspend"] = "suspended",
            ["reinstate"] = "reinstated",
            ["enable"] = "enabled",
            ["disable"] = "disabled",
            ["grant"] = "granted",
            ["revoke"] = "revoked",
            ["request"] = "requested",
            ["approve"] = "approved",
            ["refuse"] = "refused",
            ["issue"] = "issued",
            ["void"] = "voided",
            ["retire"] = "retired",
            ["restore"] = "restored",
            ["cancel"] = "cancelled",
            ["expire"] = "expired",
            ["record"] = "recorded",
            ["send"] = "sent",
            ["post"] = "posted",
            ["reverse"] = "reversed",
            ["retry"] = "retried",
            ["verify"] = "verified",
        };

    /// <summary>
    ///     Common English verbs whose past participle the regular rule misspells: the irregulars, and the
    ///     verbs that double their last consonant. Domain verbs a project adds often come from this list.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> CommonPastTense =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["take"] = "taken",
            ["write"] = "written",
            ["hold"] = "held",
            ["pay"] = "paid",
            ["buy"] = "bought",
            ["sell"] = "sold",
            ["give"] = "given",
            ["make"] = "made",
            ["keep"] = "kept",
            ["find"] = "found",
            ["bind"] = "bound",
            ["spend"] = "spent",
            ["lead"] = "led",
            ["get"] = "got",
            ["set"] = "set",
            ["put"] = "put",
            ["cut"] = "cut",
            ["run"] = "run",
            ["win"] = "won",
            ["bring"] = "brought",
            ["leave"] = "left",
            ["lose"] = "lost",
            ["meet"] = "met",
            ["begin"] = "begun",
            ["choose"] = "chosen",
            ["forget"] = "forgotten",
            ["freeze"] = "frozen",
            ["hide"] = "hidden",
            ["ride"] = "ridden",
            ["shake"] = "shaken",
            ["show"] = "shown",
            ["speak"] = "spoken",
            ["steal"] = "stolen",
            ["swear"] = "sworn",
            ["tear"] = "torn",
            ["wake"] = "woken",
            ["wear"] = "worn",
            ["teach"] = "taught",
            ["think"] = "thought",
            ["catch"] = "caught",
            ["feed"] = "fed",
            ["fight"] = "fought",
            ["split"] = "split",
            ["shut"] = "shut",
            ["submit"] = "submitted",
            ["admit"] = "admitted",
            ["commit"] = "committed",
            ["emit"] = "emitted",
            ["permit"] = "permitted",
            ["omit"] = "omitted",
            ["transfer"] = "transferred",
            ["refer"] = "referred",
            ["prefer"] = "preferred",
            ["occur"] = "occurred",
            ["control"] = "controlled",
            ["plan"] = "planned",
            ["stop"] = "stopped",
            ["tag"] = "tagged",
            ["ship"] = "shipped",
            ["drop"] = "dropped",
            ["flag"] = "flagged",
            ["map"] = "mapped",
            ["scan"] = "scanned",
            ["skip"] = "skipped",
            ["swap"] = "swapped",
            ["ban"] = "banned",
            ["bar"] = "barred",
            ["pin"] = "pinned",
            ["log"] = "logged",
            ["chat"] = "chatted",
            ["star"] = "starred",
            ["send"] = "sent",
            ["override"] = "overridden",
            ["withhold"] = "withheld",
            ["rebuild"] = "rebuilt",
            ["undo"] = "undone",
            ["rewrite"] = "rewritten",
        };

    /// <summary>
    ///     The past tense of a verb: the project's map first, then the baseline table,
    ///     else the regular form. A phrasal verb tenses its first word, so
    ///     <c>check-in</c> becomes <c>checked-in</c>.
    /// </summary>
    /// <param name="verb">The verb to tense, with any hyphenated phrasal tail.</param>
    /// <param name="project">The project past-tense map, consulted before the baseline tables.</param>
    /// <returns>The past tense of the verb.</returns>
    public static string PastTenseOf(string verb, IReadOnlyDictionary<string, string> project)
    {
        if (project.TryGetValue(verb, out var mapped) && mapped.Length > 0)
        {
            return mapped;
        }

        if (PastTense.TryGetValue(verb, out var baseline) && baseline.Length > 0)
        {
            return baseline;
        }

        if (CommonPastTense.TryGetValue(verb, out var common))
        {
            return common;
        }

        var dash = verb.IndexOf('-', StringComparison.Ordinal);

        if (dash > 0)
        {
            return PastTenseOf(verb[..dash], project) + verb[dash..];
        }

        // A prefixed verb tenses its base, so untag becomes untagged and resend becomes resent.
        foreach (var prefix in VerbPrefixes)
        {
            if (verb.StartsWith(prefix, StringComparison.Ordinal)
                && verb.Length > prefix.Length
                && (BasePastTense(verb[prefix.Length..]) ?? (prefix is "co" or "with" ? null : Doubled(verb[prefix.Length..]))) is { } basePast)
            {
                return prefix + basePast;
            }
        }

        if (verb.EndsWith('e') && verb.Length > 1)
        {
            return verb + "d";
        }

        if (verb.EndsWith('y') && verb.Length > 2 && !"aeiou".Contains(verb[^2]))
        {
            return verb[..^1] + "ied";
        }

        return Doubled(verb) ?? verb + "ed";
    }

    /// <summary>
    ///     The past tense of a one-syllable verb ending consonant-vowel-consonant, other than w, x or y,
    ///     which doubles its last consonant so shred becomes shredded; null for any other verb.
    /// </summary>
    /// <param name="verb">The verb to tense.</param>
    /// <returns>The doubled past form, or null when the verb does not double its last consonant.</returns>
    static string? Doubled(string verb) =>
        verb.Length >= 3
        && IsConsonant(verb[^1])
        && verb[^1] is not ('w' or 'x' or 'y')
        && IsVowel(verb[^2])
        && IsConsonant(verb[^3])
        && VowelGroups(verb) == 1
            ? verb + verb[^1] + "ed"
            : null;

    /// <summary>
    ///     The prefixes that carry a base verb's past tense, longest first so <c>under</c>
    ///     wins over <c>un</c> where both open the verb.
    /// </summary>
    static readonly string[] VerbPrefixes = ["over", "under", "with", "out", "mis", "pre", "un", "re", "co"];

    /// <summary>The past tense of a base verb from the common or baseline table, or null when regular.</summary>
    /// <param name="baseVerb">The base verb after its prefix is stripped.</param>
    /// <returns>The tabled past tense of the base verb, or null when no table holds it.</returns>
    static string? BasePastTense(string baseVerb)
    {
        if (CommonPastTense.TryGetValue(baseVerb, out var common))
        {
            return common;
        }

        if (PastTense.TryGetValue(baseVerb, out var baseline) && baseline.Length > 0)
        {
            return baseline;
        }

        return null;
    }

    /// <summary>Whether the character is one of the five vowels.</summary>
    /// <param name="character">The character to test.</param>
    /// <returns>True when the character is a, e, i, o or u.</returns>
    static bool IsVowel(char character) => character is 'a' or 'e' or 'i' or 'o' or 'u';

    /// <summary>Whether the character is an ASCII consonant.</summary>
    /// <param name="character">The character to test.</param>
    /// <returns>True when the character is a lowercase ASCII letter other than a vowel.</returns>
    static bool IsConsonant(char character) =>
        character is >= 'a' and <= 'z' && !IsVowel(character);

    /// <summary>The count of vowel groups in the verb, which stands in for its syllables.</summary>
    /// <param name="verb">The verb whose vowel groups are counted.</param>
    /// <returns>The number of runs of consecutive vowels in the verb.</returns>
    static int VowelGroups(string verb)
    {
        var groups = 0;
        var inVowel = false;

        foreach (var character in verb)
        {
            if (IsVowel(character))
            {
                if (!inVowel)
                {
                    groups++;
                }

                inVowel = true;
            }
            else
            {
                inVowel = false;
            }
        }

        return groups;
    }
}
