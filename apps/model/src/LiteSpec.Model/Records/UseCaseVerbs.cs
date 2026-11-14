using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     The baseline use-case verbs, as the use-cases page's verb table lists them. A permission name opens with one of
///     these verbs or with <c>manage</c>, so the permission check reads this set and the page stays its written home.
/// </summary>
/// <follows>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</follows>
public static class UseCaseVerbs
{
    /// <summary>Gets the verb that opens a permission allowing every changing use case on what it names.</summary>
    public const string Manage = "manage";

    /// <summary>Gets the kind each baseline verb opens, as the verb table's Kind column states it: query, command, or either.</summary>
    public static readonly Dictionary<string, string> Kinds = new(StringComparer.Ordinal)
    {
        ["read"] = "query",
        ["list"] = "query",
        ["search"] = "query",
        ["export"] = "query",
        ["download"] = "query",
        ["answer"] = "either",
        ["draft"] = "either",
        ["summarize"] = "either",
        ["translate"] = "either",
        ["create"] = "command",
        ["add"] = "command",
        ["remove"] = "command",
        ["change"] = "command",
        ["duplicate"] = "command",
        ["open"] = "command",
        ["close"] = "command",
        ["start"] = "command",
        ["complete"] = "command",
        ["publish"] = "command",
        ["withdraw"] = "command",
        ["pause"] = "command",
        ["resume"] = "command",
        ["suspend"] = "command",
        ["reinstate"] = "command",
        ["enable"] = "command",
        ["disable"] = "command",
        ["grant"] = "command",
        ["revoke"] = "command",
        ["request"] = "command",
        ["approve"] = "command",
        ["refuse"] = "command",
        ["issue"] = "command",
        ["void"] = "command",
        ["retire"] = "command",
        ["restore"] = "command",
        ["cancel"] = "command",
        ["expire"] = "command",
        ["record"] = "command",
        ["send"] = "command",
        ["post"] = "command",
        ["reverse"] = "command",
        ["retry"] = "command",
        ["verify"] = "command",
    };

    /// <summary>Gets the baseline verbs a permission name may open with, besides <see cref="Manage"/>.</summary>
    public static readonly HashSet<string> Baseline = new(Kinds.Keys, StringComparer.Ordinal);
}
