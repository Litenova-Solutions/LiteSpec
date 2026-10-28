using System.Text;

namespace LiteSpec.Model;

/// <summary>
///     Where one lookalike form first occurs.
/// </summary>
/// <param name="Form">The form in lower kebab case.</param>
/// <param name="Seen">The first record id, or the relative path when no record holds it.</param>
/// <param name="Subject">The first record id, or null when no record holds the form.</param>
/// <param name="File">The absolute path of the first occurrence.</param>
/// <param name="Line">The 1-based line of the first occurrence.</param>
sealed record LookalikeHit(string Form, string Seen, string? Subject, string File, int Line);

/// <summary>
///     One lookalike pair: two forms of one concept, ordered so the first sorts before the second.
/// </summary>
/// <param name="Kind">The pair kind: hyphenation, spelling, or plurality.</param>
/// <param name="First">The first form and where it first occurs.</param>
/// <param name="Second">The second form and where it first occurs.</param>
sealed record LookalikePair(string Kind, LookalikeHit First, LookalikeHit Second);

/// <summary>
///     The lookalike-term vocabulary: the spelling table and the token logic behind the
///     term-lookalike check, the names report, and the names page.
/// </summary>
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

            if (record.Type == "term" && record.Field("term") is { Length: > 0 } term)
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

    /// <summary>The finding message for one pair, naming both forms and the first record of each.</summary>
    public static string MessageFor(LookalikePair pair) => pair.Kind switch
    {
        "spelling" => $"Terms '{pair.First.Form}' in '{pair.First.Seen}' and '{pair.Second.Form}' in '{pair.Second.Seen}' mix British and American spelling.",
        "plurality" => $"Terms '{pair.First.Form}' in '{pair.First.Seen}' and '{pair.Second.Form}' in '{pair.Second.Seen}' are the singular and plural of one noun.",
        _ => $"Terms '{pair.First.Form}' in '{pair.First.Seen}' and '{pair.Second.Form}' in '{pair.Second.Seen}' differ only in hyphenation.",
    };

    /// <summary>Collects the words and their consecutive pairs, keeping the first occurrence of each.</summary>
    static void Collect(
        IReadOnlyList<string> found,
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
    static Occurrence At(Record? record, string file, int line) =>
        new(record, record?.File ?? file, record?.Line ?? line);

    /// <summary>The simple class name: the last dotted segment.</summary>
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
    static IReadOnlyList<LookalikePair> JoinedPairs(
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
    static IReadOnlyList<LookalikePair> SpellingPairs(Dictionary<string, Occurrence> words, ConsumerConfig config)
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
    static IReadOnlyList<LookalikePair> PluralPairs(
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
    static LookalikePair Pair(
        string kind, string leftForm, Occurrence left, string rightForm, Occurrence right, ConsumerConfig config) =>
        string.Compare(leftForm, rightForm, StringComparison.Ordinal) <= 0
            ? new LookalikePair(kind, Hit(leftForm, left, config), Hit(rightForm, right, config))
            : new LookalikePair(kind, Hit(rightForm, right, config), Hit(leftForm, left, config));

    /// <summary>Where one form first occurs: its record id, or its relative path without one.</summary>
    static LookalikeHit Hit(string form, Occurrence at, ConsumerConfig config) =>
        new(form, at.Source?.Id ?? config.Relative(at.File), at.Source?.Id, at.File, at.Line);

    /// <summary>Whether one name is the plural of the other: a trailing s, es, or ies for y.</summary>
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
    static IReadOnlyList<string> KebabWords(string name) =>
        name.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.ToLowerInvariant())
            .Where(word => word.Length >= 2 && word.All(character => character is >= 'a' and <= 'z'))
            .ToList();

    /// <summary>The words of one glossary term: lowercase letter runs of two or more.</summary>
    static IReadOnlyList<string> TermWords(string term)
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
    static IReadOnlyList<string> TypeWords(string qualified)
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
