using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     The record types, page styles, required fields and link targets of the model.
/// </summary>
public static class RecordTypes
{
    /// <summary>Every written record type.</summary>
    public static readonly HashSet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        "product", "goal", "constraint", "research", "decision", "requirement", "problem",
        "feature", "topic", "actor", "role", "permission", "journey", "module",
        "aggregate", "rule", "use-case", "result", "scenario", "event", "integration-event",
        "reaction", "job", "notification", "setting",
        "entry-point", "screen", "external-system", "runbook", "deviation", "known-gap", "term",
    };

    /// <summary>
    ///     The record layer of each written type: intent, behavior or governance. A term
    ///     names its own layer in its layer field; this table gives it intent by default.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> LayerByType =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["product"] = "intent", ["goal"] = "intent", ["constraint"] = "intent",
            ["research"] = "intent", ["decision"] = "intent", ["requirement"] = "intent",
            ["problem"] = "intent", ["feature"] = "intent", ["topic"] = "intent",
            ["actor"] = "intent", ["journey"] = "intent", ["term"] = "intent",
            ["module"] = "behavior", ["aggregate"] = "behavior", ["rule"] = "behavior",
            ["use-case"] = "behavior", ["result"] = "behavior", ["scenario"] = "behavior",
            ["event"] = "behavior", ["integration-event"] = "behavior", ["reaction"] = "behavior",
            ["job"] = "behavior", ["notification"] = "behavior", ["setting"] = "behavior",
            ["role"] = "behavior", ["permission"] = "behavior", ["entry-point"] = "behavior",
            ["screen"] = "behavior", ["external-system"] = "behavior", ["runbook"] = "behavior",
            ["deviation"] = "governance", ["known-gap"] = "governance",
        };

    /// <summary>Page styles: templates a page may carry instead of a record.</summary>
    public static readonly HashSet<string> PageStyles = new HashSet<string>(StringComparer.Ordinal)
    {
        "permission-list", "entry-point-list", "setting-set", "retention-schedule",
        "rule-set", "index", "tutorial", "how-to", "reference", "command",
        "configuration", "glossary", "example-cast", "review-report",
    };

    /// <summary>Types whose identifier carries a home: type/home.name.</summary>
    public static readonly HashSet<string> WithHome = new HashSet<string>(StringComparer.Ordinal)
    {
        "requirement", "aggregate", "rule", "use-case", "result",
        "scenario", "event", "reaction", "job", "notification", "setting", "screen",
    };

    /// <summary>Types the small size requires.</summary>
    public static readonly IReadOnlyList<string> SmallRequired = new string[]
    {
        "product", "decision", "requirement", "actor", "role", "permission",
        "module", "aggregate", "rule", "use-case", "result", "event",
        "entry-point", "deviation", "known-gap", "term",
    };

    /// <summary>Types only the large size requires.</summary>
    public static readonly IReadOnlyList<string> LargeAdded = new string[]
    {
        "goal", "problem", "feature", "journey", "scenario",
    };

    /// <summary>The entry-point kinds that run in the background.</summary>
    public static readonly HashSet<string> BackgroundEntryKinds = new HashSet<string>(StringComparer.Ordinal)
    {
        "reaction", "job",
    };

    /// <summary>
    ///     Whether the field names a link, and which record types it may point at.
    ///     An empty set means any record type. A serves target of journey/&lt;name&gt;#&lt;step&gt;
    ///     resolves through its journey, and a calls target of
    ///     external-system/&lt;name&gt;#&lt;operation&gt; through its system.
    /// </summary>
    public static readonly Dictionary<string, HashSet<string>> LinkTargets =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["decides"] = new HashSet<string>(),
            ["cites"] = new HashSet<string> { "goal", "problem", "feature", "constraint", "research", "decision", "requirement" },
            ["basedOn"] = new HashSet<string> { "constraint", "research" },
            ["affects"] = new HashSet<string> { "actor" },
            ["addresses"] = new HashSet<string> { "actor", "permission" },
            ["solves"] = new HashSet<string> { "problem" },
            ["advances"] = new HashSet<string> { "goal" },
            ["serves"] = new HashSet<string> { "goal", "journey" },
            ["fulfills"] = new HashSet<string> { "requirement" },
            ["governs"] = new HashSet<string> { "aggregate", "use-case" },
            ["raises"] = new HashSet<string> { "event" },
            ["issues"] = new HashSet<string> { "use-case" },
            ["sends"] = new HashSet<string> { "notification" },
            ["publishes"] = new HashSet<string> { "integration-event" },
            ["calls"] = new HashSet<string> { "external-system" },
            ["on"] = new HashSet<string> { "event", "integration-event" },
            ["changes"] = new HashSet<string> { "aggregate" },
            ["reads"] = new HashSet<string> { "aggregate" },
            ["translates"] = new HashSet<string> { "event" },
            ["acceptedBy"] = new HashSet<string> { "decision", "requirement" },
            ["fallback"] = new HashSet<string> { "requirement" },
            ["releasedBy"] = new HashSet<string> { "use-case" },
            ["computedBy"] = new HashSet<string> { "rule" },
            ["enforcedBy"] = new HashSet<string> { "job" },
            ["effects"] = new HashSet<string> { "use-case", "notification", "integration-event", "external-system" },
            ["access"] = new HashSet<string> { "permission" },
            ["except"] = new HashSet<string> { "requirement" },
            ["dataClasses"] = new HashSet<string> { "job", "decision", "requirement" },
            ["allows"] = new HashSet<string> { "permission" },
            ["reachedThrough"] = new HashSet<string> { "entry-point" },
            ["dependsOn"] = new HashSet<string> { "module" },
            ["terms"] = new HashSet<string> { "term" },
            ["result"] = new HashSet<string> { "result" },
            ["actor"] = new HashSet<string> { "actor" },
            ["entryPoint"] = new HashSet<string> { "entry-point" },
            ["offers"] = new HashSet<string> { "use-case" },
            ["host"] = new HashSet<string> { "screen" },
            ["runbook"] = new HashSet<string> { "runbook" },
            ["readBy"] = new HashSet<string> { "use-case" },
            ["subject"] = new HashSet<string>(),
            ["values"] = new HashSet<string>(),
        };

    /// <summary>Fields that hold links but are read from nested objects.</summary>
    public static readonly HashSet<string> NestedLinkFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "holds", "steps",
    };

    /// <summary>Fields that hold identifiers but are not links.</summary>
    public static readonly HashSet<string> IdentityFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "type", "id", "title", "owner", "status", "decider", "decideBy",
        "replaces", "code",
    };
}
