using System.Text;

namespace LiteSpec.Model;

/// <summary>
///     The lookalike-term vocabulary: the spelling table and the token logic behind the
///     term-clash check, the names report, and the names page.
/// </summary>
/// <follows>litespec/core/authoring#review-each-term-for-its-lookalikes</follows>
static class TermLookalikes
{
    /// <summary>The British and American spellings that name one word, British first.</summary>
    public static readonly IReadOnlyList<(string British, string American)> Spellings =
    [
        ("behaviour", "behavior"),
        ("centre", "center"),
        ("colour", "color"),
        ("organise", "organize"),
        ("organisation", "organization"),
        ("licence", "license"),
        ("catalogue", "catalog"),
        ("cancelled", "canceled"),
        ("travelled", "traveled"),
        ("modelling", "modeling"),
    ];

    /// <summary>One token occurrence: the record holding it, or the code file and line.</summary>
    sealed record Occurrence(Record? Source, string File, int Line);

    /// <summary>
    ///     Finds every lookalike pair in use: a joined form beside its hyphenated split, a British
    ///     spelling beside its American spelling, or a singular beside its plural as two record
    ///     names in one home. A pair reports only when both forms occur, and never when the
    ///     project's lookalike ignore list names it.
    /// </summary>
    /// <param name="model">The built project model holding the records and code.</param>
    /// <returns>Every lookalike pair in use, ordered by first then second form.</returns>
    public static IReadOnlyList<LookalikePair> FindPairs(ProjectModel model)
    {
        var words = new Dictionary<string, Occurrence>(StringComparer.Ordinal);
        var splits = new Dictionary<string, Occurrence>(StringComparer.Ordinal);
        var names = new List<(string Name, string? Home, Occurrence At)>();

        foreach (var record in model.Records)
        {
            if (Identifiers.TrySplit(record.Id, out _, out var home, out var name))
            {
                var at = new Occurrence(record, record.File, record.Line);
                Collect(KebabWords(name), at, words, splits);
                names.Add((name.ToLowerInvariant(), home, at));
            }

            if (record.Type == RecordTypes.Term && record.Field(RecordFields.Term) is { Length: > 0 } term)
            {
                Collect(TermWords(term), new Occurrence(record, record.File, record.Line), words, splits);
            }
        }

        CollectCode(model, words, splits);

        var ignore = model.Config.LookalikeIgnore.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pairs = new List<LookalikePair>();
        pairs.AddRange(JoinedPairs(words, splits, model.Config));
        pairs.AddRange(SpellingPairs(words, model.Config));
        pairs.AddRange(PluralPairs(names, model.Config));
        pairs.Sort((left, right) =>
        {
            var first = string.Compare(left.First.Form, right.First.Form, StringComparison.Ordinal);
            return first != 0 ? first : string.Compare(left.Second.Form, right.Second.Form, StringComparison.Ordinal);
        });

        return pairs.Where(pair => !ignore.Contains($"{pair.First.Form}|{pair.Second.Form}")
            && !ignore.Contains($"{pair.Second.Form}|{pair.First.Form}")).ToList();
    }

    /// <summary>Collects the words and their consecutive pairs, keeping the first occurrence of each.</summary>
    /// <param name="found">The words found in one name or term.</param>
    /// <param name="at">The occurrence the words are collected at.</param>
    /// <param name="words">The first occurrence of each word, updated in place.</param>
    /// <param name="splits">The first occurrence of each consecutive word pair, updated in place.</param>
    static void Collect(
        List<string> found,
        Occurrence at,
        Dictionary<string, Occurrence> words,
        Dictionary<string, Occurrence> splits)
    {
        foreach (var word in found)
        {
            words.TryAdd(word, at);
        }

        for (var index = 0; index + 1 < found.Count; index++)
        {
            splits.TryAdd($"{found[index]}-{found[index + 1]}", at);
        }
    }

    /// <summary>Collects the words of every C# type name the code model holds.</summary>
    /// <param name="model">The built project model holding the code model.</param>
    /// <param name="words">The first occurrence of each word, updated in place.</param>
    /// <param name="splits">The first occurrence of each consecutive word pair, updated in place.</param>
    static void CollectCode(
        ProjectModel model,
        Dictionary<string, Occurrence> words,
        Dictionary<string, Occurrence> splits)
    {
        var records = new Dictionary<string, Record?>(StringComparer.Ordinal);

        foreach (var handler in model.Code.Handlers)
        {
            model.ById.TryGetValue(handler.UseCaseId, out var record);
            records.TryAdd(SimpleName(handler.Class), record);
        }

        foreach (var reaction in model.Code.Reactions)
        {
            model.ById.TryGetValue(reaction.RecordId ?? reaction.UseCaseId ?? "", out var record);
            records.TryAdd(SimpleName(reaction.Class), record);
        }

        foreach (var job in model.Code.Jobs)
        {
            model.ById.TryGetValue(job.RecordId ?? job.UseCaseId ?? "", out var record);
            records.TryAdd(SimpleName(job.Class), record);
        }

        foreach (var domainEvent in model.Code.Events)
        {
            model.ById.TryGetValue(domainEvent.EventId ?? "", out var record);
            records.TryAdd(SimpleName(domainEvent.Class), record);
        }

        foreach (var handler in model.Code.Handlers)
        {
            var at = At(records.GetValueOrDefault(SimpleName(handler.Class)), handler.File, handler.Line);
            Collect(TypeWords(handler.Class), at, words, splits);

            foreach (var thrown in handler.Thrown ?? [])
            {
                Collect(TypeWords(thrown.Type), at, words, splits);
            }

            foreach (var repository in handler.Repositories ?? [])
            {
                Collect(TypeWords(repository.Interface), at, words, splits);
            }
        }

        foreach (var reaction in model.Code.Reactions)
        {
            var at = At(records.GetValueOrDefault(SimpleName(reaction.Class)), reaction.File, reaction.Line);
            Collect(TypeWords(reaction.Class), at, words, splits);

            foreach (var thrown in reaction.Thrown ?? [])
            {
                Collect(TypeWords(thrown.Type), at, words, splits);
            }

            foreach (var repository in reaction.Repositories ?? [])
            {
                Collect(TypeWords(repository.Interface), at, words, splits);
            }
        }

        foreach (var job in model.Code.Jobs)
        {
            var at = At(records.GetValueOrDefault(SimpleName(job.Class)), job.File, job.Line);
            Collect(TypeWords(job.Class), at, words, splits);

            foreach (var thrown in job.Thrown ?? [])
            {
                Collect(TypeWords(thrown.Type), at, words, splits);
            }

            foreach (var repository in job.Repositories ?? [])
            {
                Collect(TypeWords(repository.Interface), at, words, splits);
            }
        }

        foreach (var domainEvent in model.Code.Events)
        {
            Collect(
                TypeWords(domainEvent.Class),
                At(records.GetValueOrDefault(SimpleName(domainEvent.Class)), domainEvent.File, domainEvent.Line),
                words,
                splits);
        }

        foreach (var literal in model.Code.Literals)
        {
            if (literal.Class is null)
            {
                continue;
            }

            Collect(TypeWords(literal.Class), At(records.GetValueOrDefault(literal.Class), literal.File, literal.Line), words, splits);
        }
    }

    /// <summary>An occurrence on the record when one holds the class, else on the code file.</summary>
    /// <param name="record">The record holding the class, or null when none holds it.</param>
    /// <param name="file">The code file the class occurs in.</param>
    /// <param name="line">The 1-based line the class occurs on.</param>
    /// <returns>The occurrence on the record when one holds it; otherwise the occurrence on the code file.</returns>
    static Occurrence At(Record? record, string file, int line) =>
        new(record, record?.File ?? file, record?.Line ?? line);

    /// <summary>The simple class name: the last dotted segment.</summary>
    /// <param name="qualified">The qualified type name to shorten.</param>
    /// <returns>The segment after the last dot, or the whole name when it holds no dot.</returns>
    static string SimpleName(string qualified) =>
        qualified[(qualified.LastIndexOf('.') + 1)..];

    /// <summary>
    ///     The function words that open no compound: a hyphenated pair starting with one is two
    ///     words side by side, such as <c>as-signed</c> in <c>export-as-signed-rows</c>.
    /// </summary>
    static readonly HashSet<string> FunctionWords = new(StringComparer.Ordinal)
    {
        "a", "an", "as", "at", "by", "in", "of", "on", "or", "to", "with",
    };

    /// <summary>Every joined form beside the hyphenated split it also occurs with.</summary>
    /// <param name="words">The first occurrence of each word.</param>
    /// <param name="splits">The first occurrence of each hyphenated split.</param>
    /// <param name="config">The consumer configuration, for the relative path of a form no record holds.</param>
    /// <returns>The hyphenation pairs, in joined-form order.</returns>
    static List<LookalikePair> JoinedPairs(
        Dictionary<string, Occurrence> words, Dictionary<string, Occurrence> splits, ConsumerConfig config)
    {
        var pairs = new List<LookalikePair>();

        foreach (var joined in words.Keys.Order(StringComparer.Ordinal))
        {
            // A split needs two parts of at least two letters, so a shorter word pairs with nothing.
            if (joined.Length < 4)
            {
                continue;
            }

            for (var cut = 2; cut <= joined.Length - 2; cut++)
            {
                // A split opening with a function word is two words side by side, not one compound.
                if (FunctionWords.Contains(joined[..cut]))
                {
                    continue;
                }

                var split = $"{joined[..cut]}-{joined[cut..]}";

                if (splits.TryGetValue(split, out var seen))
                {
                    pairs.Add(Pair("hyphenation", joined, words[joined], split, seen, config));
                }
            }
        }

        return pairs;
    }

    /// <summary>Every British spelling beside the American spelling it also occurs with.</summary>
    /// <param name="words">The first occurrence of each word.</param>
    /// <param name="config">The consumer configuration, for the relative path of a form no record holds.</param>
    /// <returns>The spelling pairs, in spelling-table order.</returns>
    static List<LookalikePair> SpellingPairs(Dictionary<string, Occurrence> words, ConsumerConfig config)
    {
        var pairs = new List<LookalikePair>();

        foreach (var (british, american) in Spellings)
        {
            if (words.TryGetValue(british, out var britishSeen) && words.TryGetValue(american, out var americanSeen))
            {
                pairs.Add(Pair("spelling", british, britishSeen, american, americanSeen, config));
            }
        }

        return pairs;
    }

    /// <summary>Every singular beside its plural as two record names sharing one home.</summary>
    /// <param name="names">The record names with their homes and occurrences.</param>
    /// <param name="config">The consumer configuration, for the relative path of a form no record holds.</param>
    /// <returns>The plurality pairs, grouped by home in name order.</returns>
    static List<LookalikePair> PluralPairs(
        List<(string Name, string? Home, Occurrence At)> names, ConsumerConfig config)
    {
        var pairs = new List<LookalikePair>();

        foreach (var group in names.Where(entry => entry.Home is not null).GroupBy(entry => entry.Home!))
        {
            var distinct = group.GroupBy(entry => entry.Name, StringComparer.Ordinal)
                                .Select(own => own.First())
                                .OrderBy(entry => entry.Name, StringComparer.Ordinal)
                                .ToList();

            for (var left = 0; left < distinct.Count; left++)
            {
                for (var right = left + 1; right < distinct.Count; right++)
                {
                    if (IsPluralPair(distinct[left].Name, distinct[right].Name))
                    {
                        pairs.Add(Pair("plurality", distinct[left].Name, distinct[left].At, distinct[right].Name, distinct[right].At, config));
                    }
                }
            }
        }

        return pairs;
    }

    /// <summary>One pair with its forms ordered, each with where it first occurs.</summary>
    /// <param name="kind">The pair kind: hyphenation, spelling, or plurality.</param>
    /// <param name="leftForm">The first candidate form.</param>
    /// <param name="left">The first occurrence of the left form.</param>
    /// <param name="rightForm">The second candidate form.</param>
    /// <param name="right">The first occurrence of the right form.</param>
    /// <param name="config">The consumer configuration, for the relative path of a form no record holds.</param>
    /// <returns>The pair with the ordinally smaller form first.</returns>
    static LookalikePair Pair(
        string kind, string leftForm, Occurrence left, string rightForm, Occurrence right, ConsumerConfig config) =>
        string.Compare(leftForm, rightForm, StringComparison.Ordinal) <= 0
            ? new LookalikePair(kind, Hit(leftForm, left, config), Hit(rightForm, right, config))
            : new LookalikePair(kind, Hit(rightForm, right, config), Hit(leftForm, left, config));

    /// <summary>Where one form first occurs: its record id, or its relative path without one.</summary>
    /// <param name="form">The lookalike form.</param>
    /// <param name="at">The first occurrence of the form.</param>
    /// <param name="config">The consumer configuration, for the relative path when no record holds the form.</param>
    /// <returns>The hit naming the form and its first occurrence.</returns>
    static LookalikeHit Hit(string form, Occurrence at, ConsumerConfig config) =>
        new(form, at.Source?.Id ?? config.Relative(at.File), at.Source?.Id, at.File, at.Line);

    /// <summary>Whether one name is the plural of the other: a trailing s, es, or ies for y.</summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns>True when the longer name is the plural of the shorter by a trailing s, es, or ies for y.</returns>
    static bool IsPluralPair(string left, string right)
    {
        var (shorter, longer) = left.Length <= right.Length ? (left, right) : (right, left);

        if (shorter.Length < 2 || longer.Length - shorter.Length is < 1 or > 2)
        {
            return false;
        }

        if (longer == shorter + "s" && !shorter.EndsWith('s'))
        {
            return true;
        }

        if (longer == shorter + "es")
        {
            return true;
        }

        return shorter.EndsWith('y')
            && shorter.Length > 2
            && "aeiou".IndexOf(shorter[^2]) < 0
            && longer == shorter[..^1] + "ies";
    }

    /// <summary>The words of one kebab-case name: lowercase letter runs of two or more.</summary>
    /// <param name="name">The kebab-case name to split.</param>
    /// <returns>The lowercase words of at least two letters in the name.</returns>
    static List<string> KebabWords(string name) =>
        name.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.ToLowerInvariant())
            .Where(word => word.Length >= 2 && word.All(character => character is >= 'a' and <= 'z'))
            .ToList();

    /// <summary>The words of one glossary term: lowercase letter runs of two or more.</summary>
    /// <param name="term">The glossary term to split.</param>
    /// <returns>The lowercase letter runs of at least two letters in the term.</returns>
    static List<string> TermWords(string term)
    {
        var words = new List<string>();
        var current = new StringBuilder();

        foreach (var character in term.ToLowerInvariant())
        {
            if (character is >= 'a' and <= 'z')
            {
                current.Append(character);
                continue;
            }

            if (current.Length >= 2)
            {
                words.Add(current.ToString());
            }

            current.Clear();
        }

        if (current.Length >= 2)
        {
            words.Add(current.ToString());
        }

        return words;
    }

    /// <summary>The words of one C# type name: each dotted segment split before every capital.</summary>
    /// <param name="qualified">The qualified type name to split.</param>
    /// <returns>The lowercase words of at least two letters in the type name.</returns>
    static List<string> TypeWords(string qualified)
    {
        var words = new List<string>();

        foreach (var segment in qualified.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var current = new StringBuilder();

            foreach (var character in segment)
            {
                if (char.IsUpper(character) && current.Length > 0)
                {
                    words.Add(current.ToString());
                    current.Clear();
                }

                if (char.IsAsciiLetterOrDigit(character))
                {
                    current.Append(char.ToLowerInvariant(character));
                }
            }

            if (current.Length > 0)
            {
                words.Add(current.ToString());
            }
        }

        return words.Where(word => word.Length >= 2 && word.All(character => character is >= 'a' and <= 'z')).ToList();
    }
}
