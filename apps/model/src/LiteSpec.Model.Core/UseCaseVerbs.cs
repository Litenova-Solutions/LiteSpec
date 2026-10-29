using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     The use-case verbs every project starts from, each with one meaning and one kind, and the synonyms
///     they retire. A project adds its domain verbs on top and retires more synonyms; it never redefines a
///     baseline verb.
/// </summary>
/// <follows>litespec/core/behavior#keep-one-kind-per-verb</follows>
/// <follows>litespec/core/behavior#start-every-use-case-with-a-declared-verb</follows>
public static class UseCaseVerbs
{
    /// <summary>The kind a verb names: a query reads, a command changes, and either serves both.</summary>
    public enum VerbKind
    {
        /// <summary>The verb opens only a query.</summary>
        Query,

        /// <summary>The verb opens only a command.</summary>
        Command,

        /// <summary>The verb opens a query or a command.</summary>
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

    /// <summary>
    ///     The words only a permission name opens with, each with the kind it grants. A permission groups the
    ///     use cases it allows, so `manage` names one that allows every changing use case on what it names.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, VerbKind> PermissionOnly =
        new Dictionary<string, VerbKind>(StringComparer.Ordinal)
        {
            ["manage"] = VerbKind.Command,
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
    /// <param name="name">The kebab-case name.</param>
    /// <param name="phrasal">The multi-word verbs counted as one opening.</param>
    /// <returns>The opening verb.</returns>
    public static string VerbOf(string name, IReadOnlyList<string> phrasal)
    {
        foreach (var verb in phrasal)
        {
            if (name == verb || name.StartsWith(verb + "-", StringComparison.Ordinal))
            {
                return verb;
            }
        }

        var dash = name.IndexOf('-');

        return dash < 0 ? name : name.Substring(0, dash);
    }

    /// <summary>The kind a verb names: the baseline kind, a project query verb, or either.</summary>
    /// <param name="verb">The opening verb.</param>
    /// <param name="queryVerbs">The project verbs that name a query.</param>
    /// <returns>The verb's kind.</returns>
    public static VerbKind KindOf(string verb, IReadOnlyList<string> queryVerbs) =>
        Baseline.TryGetValue(verb, out var kind) ? kind
        : Contains(queryVerbs, verb) ? VerbKind.Query
        : VerbKind.Either;

    /// <summary>Whether the baseline or the project declares the verb.</summary>
    /// <param name="verb">The opening verb.</param>
    /// <param name="verbs">The domain verbs the project adds to the baseline.</param>
    /// <param name="queryVerbs">The project verbs that name a query.</param>
    /// <param name="phrasal">The multi-word verbs counted as one opening.</param>
    /// <returns>Whether the verb is declared.</returns>
    public static bool IsDeclared(
        string verb,
        IReadOnlyList<string> verbs,
        IReadOnlyList<string> queryVerbs,
        IReadOnlyList<string> phrasal) =>
        Baseline.ContainsKey(verb)
        || Contains(verbs, verb)
        || Contains(queryVerbs, verb)
        || Contains(phrasal, verb);

    /// <summary>Whether a permission name may open with the word: a declared verb or a permission-only word.</summary>
    /// <param name="verb">The opening word.</param>
    /// <param name="verbs">The domain verbs the project adds to the baseline.</param>
    /// <param name="queryVerbs">The project verbs that name a query.</param>
    /// <param name="phrasal">The multi-word verbs counted as one opening.</param>
    /// <returns>Whether the word is declared for a permission.</returns>
    public static bool IsPermissionDeclared(
        string verb,
        IReadOnlyList<string> verbs,
        IReadOnlyList<string> queryVerbs,
        IReadOnlyList<string> phrasal) =>
        PermissionOnly.ContainsKey(verb) || IsDeclared(verb, verbs, queryVerbs, phrasal);

    /// <summary>The kind a permission's opening word grants: its permission-only kind, or its verb kind.</summary>
    /// <param name="verb">The opening word.</param>
    /// <param name="queryVerbs">The project verbs that name a query.</param>
    /// <returns>The word's kind.</returns>
    public static VerbKind PermissionKindOf(string verb, IReadOnlyList<string> queryVerbs) =>
        PermissionOnly.TryGetValue(verb, out var kind) ? kind : KindOf(verb, queryVerbs);

    private static bool Contains(IReadOnlyList<string> values, string verb)
    {
        foreach (var value in values)
        {
            if (string.Equals(value, verb, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
