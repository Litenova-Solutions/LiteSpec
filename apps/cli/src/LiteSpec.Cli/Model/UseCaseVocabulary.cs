namespace LiteSpec.Model;

/// <summary>
///     The use-case verbs every project starts from, each with one meaning and one kind, and the synonyms
///     they retire. A project adds its domain verbs on top and retires more synonyms; it never redefines a
///     baseline verb.
/// </summary>
static class UseCaseVocabulary
{
    /// <summary>The kind a verb names: a query reads, a command changes, and either serves both.</summary>
    public enum VerbKind
    {
        Query,
        Command,
        Either,
    }

    /// <summary>The baseline verbs with the kind each one names.</summary>
    public static readonly IReadOnlyDictionary<string, VerbKind> Baseline =
        new Dictionary<string, VerbKind>(StringComparer.Ordinal)
        {
            // Reading.
            ["read"] = VerbKind.Query,
            ["list"] = VerbKind.Query,
            ["search"] = VerbKind.Query,
            ["export"] = VerbKind.Query,
            ["download"] = VerbKind.Query,

            // Text a model writes for a person to review.
            ["answer"] = VerbKind.Either,
            ["draft"] = VerbKind.Either,
            ["summarize"] = VerbKind.Either,
            ["translate"] = VerbKind.Either,

            // Records and their parts.
            ["create"] = VerbKind.Command,
            ["add"] = VerbKind.Command,
            ["remove"] = VerbKind.Command,
            ["change"] = VerbKind.Command,
            ["duplicate"] = VerbKind.Command,

            // Periods, work, visibility, stops and switches.
            ["open"] = VerbKind.Command,
            ["close"] = VerbKind.Command,
            ["start"] = VerbKind.Command,
            ["complete"] = VerbKind.Command,
            ["publish"] = VerbKind.Command,
            ["withdraw"] = VerbKind.Command,
            ["pause"] = VerbKind.Command,
            ["resume"] = VerbKind.Command,
            ["suspend"] = VerbKind.Command,
            ["reinstate"] = VerbKind.Command,
            ["enable"] = VerbKind.Command,
            ["disable"] = VerbKind.Command,

            // Access, decisions and documents.
            ["grant"] = VerbKind.Command,
            ["revoke"] = VerbKind.Command,
            ["request"] = VerbKind.Command,
            ["approve"] = VerbKind.Command,
            ["refuse"] = VerbKind.Command,
            ["issue"] = VerbKind.Command,
            ["void"] = VerbKind.Command,

            // A record's life, intentions and time.
            ["retire"] = VerbKind.Command,
            ["restore"] = VerbKind.Command,
            ["cancel"] = VerbKind.Command,
            ["expire"] = VerbKind.Command,

            // Outside facts, messages and the books.
            ["record"] = VerbKind.Command,
            ["send"] = VerbKind.Command,
            ["post"] = VerbKind.Command,
            ["reverse"] = VerbKind.Command,
            ["retry"] = VerbKind.Command,
            ["verify"] = VerbKind.Command,
        };

    /// <summary>The synonyms the baseline retires, each with the verb that replaces it.</summary>
    public static readonly IReadOnlyDictionary<string, string> Retired =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["get"] = "read",
            ["fetch"] = "read",
            ["lookup"] = "read",
            ["count"] = "read",
            ["browse"] = "list",
            ["find"] = "search",
            ["update"] = "change",
            ["edit"] = "change",
            ["modify"] = "change",
            ["revise"] = "change",
            ["set"] = "change",
            ["adjust"] = "change",
            ["configure"] = "change",
            ["rename"] = "change",
            ["define"] = "create",
            ["compose"] = "create",
            ["save"] = "create",
            ["new"] = "create",
            ["delete"] = "remove",
            ["notify"] = "send",
            ["remind"] = "send",
            ["email"] = "send",
            ["suggest"] = "answer",
            ["ask"] = "answer",
            ["mark"] = "record",
            ["note"] = "record",
            ["log"] = "record",
            ["end"] = "close",
            ["finish"] = "complete",
            ["abandon"] = "cancel",
            ["lapse"] = "expire",
            ["clone"] = "duplicate",
            ["copy"] = "duplicate",
            ["replay"] = "retry",
            ["recheck"] = "verify",
            ["archive"] = "retire",
            ["reject"] = "refuse",
            ["deny"] = "refuse",
            ["unpublish"] = "withdraw",
            ["hide"] = "withdraw",
        };

    /// <summary>The first word of a name, or the phrasal verb it opens with.</summary>
    public static string VerbOf(string name, IReadOnlyList<string> phrasal)
    {
        foreach (var verb in phrasal)
        {
            if (name == verb || name.StartsWith(verb + "-", StringComparison.Ordinal))
            {
                return verb;
            }
        }

        var dash = name.IndexOf('-', StringComparison.Ordinal);

        return dash < 0 ? name : name[..dash];
    }
}
