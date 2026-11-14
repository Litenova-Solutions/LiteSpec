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

    /// <summary>Gets the baseline verbs a permission name may open with, besides <see cref="Manage"/>.</summary>
    public static readonly HashSet<string> Baseline = new(StringComparer.Ordinal)
    {
        "read", "list", "search", "export", "download",
        "answer", "draft", "summarize", "translate",
        "create", "add", "remove", "change", "duplicate",
        "open", "close", "start", "complete",
        "publish", "withdraw", "pause", "resume",
        "suspend", "reinstate", "enable", "disable",
        "grant", "revoke", "request", "approve", "refuse",
        "issue", "void", "retire", "restore", "cancel", "expire",
        "record", "send", "post", "reverse", "retry", "verify",
    };
}
