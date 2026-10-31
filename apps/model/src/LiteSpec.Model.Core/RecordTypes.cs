using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     The record types, page styles, required fields and link targets of the model.
/// </summary>
public static class RecordTypes
{
    /// <summary>Gets the `actor` record type name.</summary>
    public const string Actor = "actor";

    /// <summary>Gets the `aggregate` record type name.</summary>
    public const string Aggregate = "aggregate";

    /// <summary>Gets the `command` record type name.</summary>
    public const string Command = "command";

    /// <summary>Gets the `configuration` record type name.</summary>
    public const string Configuration = "configuration";

    /// <summary>Gets the `constraint` record type name.</summary>
    public const string Constraint = "constraint";

    /// <summary>Gets the `decision` record type name.</summary>
    public const string Decision = "decision";

    /// <summary>Gets the `entry-point` record type name.</summary>
    public const string EntryPoint = "entry-point";

    /// <summary>Gets the `entry-point-list` record type name.</summary>
    public const string EntryPointList = "entry-point-list";

    /// <summary>Gets the `event` record type name.</summary>
    public const string Event = "event";

    /// <summary>Gets the `example-cast` record type name.</summary>
    public const string ExampleCast = "example-cast";

    /// <summary>Gets the `exception` record type name.</summary>
    public const string Exception = "exception";

    /// <summary>Gets the `external-system` record type name.</summary>
    public const string ExternalSystem = "external-system";

    /// <summary>Gets the `feature` record type name.</summary>
    public const string Feature = "feature";

    /// <summary>Gets the `glossary` record type name.</summary>
    public const string Glossary = "glossary";

    /// <summary>Gets the `goal` record type name.</summary>
    public const string Goal = "goal";

    /// <summary>Gets the `how-to` record type name.</summary>
    public const string HowTo = "how-to";

    /// <summary>Gets the `index` record type name.</summary>
    public const string Index = "index";

    /// <summary>Gets the `integration-event` record type name.</summary>
    public const string IntegrationEvent = "integration-event";

    /// <summary>Gets the `job` record type name.</summary>
    public const string Job = "job";

    /// <summary>Gets the `journey` record type name.</summary>
    public const string Journey = "journey";

    /// <summary>Gets the `module` record type name.</summary>
    public const string Module = "module";

    /// <summary>Gets the `notification` record type name.</summary>
    public const string Notification = "notification";

    /// <summary>Gets the `permission` record type name.</summary>
    public const string Permission = "permission";

    /// <summary>Gets the `permission-list` record type name.</summary>
    public const string PermissionList = "permission-list";

    /// <summary>Gets the `problem` record type name.</summary>
    public const string Problem = "problem";

    /// <summary>Gets the `product` record type name.</summary>
    public const string Product = "product";

    /// <summary>Gets the `reaction` record type name.</summary>
    public const string Reaction = "reaction";

    /// <summary>Gets the `reference` record type name.</summary>
    public const string Reference = "reference";

    /// <summary>Gets the `requirement` record type name.</summary>
    public const string Requirement = "requirement";

    /// <summary>Gets the `research` record type name.</summary>
    public const string Research = "research";

    /// <summary>Gets the `result` record type name.</summary>
    public const string Result = "result";

    /// <summary>Gets the `retention-schedule` record type name.</summary>
    public const string RetentionSchedule = "retention-schedule";

    /// <summary>Gets the `review-report` record type name.</summary>
    public const string ReviewReport = "review-report";

    /// <summary>Gets the `role` record type name.</summary>
    public const string Role = "role";

    /// <summary>Gets the `rule` record type name.</summary>
    public const string Rule = "rule";

    /// <summary>Gets the `rule-set` record type name.</summary>
    public const string RuleSet = "rule-set";

    /// <summary>Gets the `runbook` record type name.</summary>
    public const string Runbook = "runbook";

    /// <summary>Gets the `scenario` record type name.</summary>
    public const string Scenario = "scenario";

    /// <summary>Gets the `screen` record type name.</summary>
    public const string Screen = "screen";

    /// <summary>Gets the `setting` record type name.</summary>
    public const string Setting = "setting";

    /// <summary>Gets the `setting-set` record type name.</summary>
    public const string SettingSet = "setting-set";

    /// <summary>Gets the `term` record type name.</summary>
    public const string Term = "term";

    /// <summary>Gets the `topic` record type name.</summary>
    public const string Topic = "topic";

    /// <summary>Gets the `tutorial` record type name.</summary>
    public const string Tutorial = "tutorial";

    /// <summary>Gets the `use-case` record type name.</summary>
    public const string UseCase = "use-case";

    /// <summary>Every written record type.</summary>
    public static readonly HashSet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        "product", "goal", "constraint", "research", "decision", "requirement", "problem",
        "feature", "topic", "actor", "role", "permission", "journey", "module",
        "aggregate", "rule", "use-case", "result", "scenario", "event", "integration-event",
        "reaction", "job", "notification", "setting",
        "entry-point", "screen", "external-system", "runbook", "exception", "term",
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
            ["exception"] = "governance",
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
    public static readonly IReadOnlyList<string> SmallRequired =
    [
        "product", "decision", "requirement", "actor", "role", "permission",
        "module", "aggregate", "rule", "use-case", "result", "event",
        "entry-point", "exception", "term",
    ];

    /// <summary>Types only the large size requires.</summary>
    public static readonly IReadOnlyList<string> LargeAdded =
    [
        "goal", "problem", "feature", "journey", "scenario",
    ];

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
            ["runs"] = new HashSet<string> { "use-case" },
            ["sends"] = new HashSet<string> { "notification" },
            ["emits"] = new HashSet<string> { "integration-event" },
            ["calls"] = new HashSet<string> { "external-system" },
            ["triggeredBy"] = new HashSet<string> { "event", "integration-event" },
            ["changes"] = new HashSet<string> { "aggregate" },
            ["reads"] = new HashSet<string> { "aggregate", "setting" },
            ["translates"] = new HashSet<string> { "event" },
            ["acceptedBy"] = new HashSet<string> { "decision", "requirement" },
            ["fallback"] = new HashSet<string> { "requirement" },
            ["releasedBy"] = new HashSet<string> { "use-case" },
            ["computedBy"] = new HashSet<string> { "rule" },
            ["enforcedBy"] = new HashSet<string> { "reaction", "job" },
            ["effects"] = new HashSet<string> { "use-case", "notification", "integration-event", "external-system" },
            ["access"] = new HashSet<string> { "permission" },
            ["except"] = new HashSet<string> { "requirement" },
            ["dataClasses"] = new HashSet<string> { "reaction", "job", "decision", "requirement" },
            ["compensation"] = new HashSet<string> { "use-case" },
            ["freshness"] = new HashSet<string>(),
            ["erasure"] = new HashSet<string> { "external-system", "integration-event" },
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
            ["subject"] = new HashSet<string>(),
            ["values"] = new HashSet<string>(),
        };

    /// <summary>
    ///     The record types that fulfill a requirement, in message order: the Written on
    ///     column of the fulfills row of the Links table. The fulfillment check reads this table.
    /// </summary>
    public static readonly IReadOnlyList<string> FulfillingTypes =
    [
        "rule", "use-case", "reaction", "job", "notification", "setting",
    ];

    /// <summary>
    ///     The record types that serve a journey step, in message order: the Written on
    ///     column of the serves row for journey steps. The step check reads this table.
    /// </summary>
    public static readonly IReadOnlyList<string> StepServingTypes =
    [
        "use-case", "reaction", "job",
    ];

    /// <summary>
    ///     The record types that link up with serves or fulfills, in message order:
    ///     every fulfilling type except a rule. The orphan check reads this table.
    /// </summary>
    public static readonly IReadOnlyList<string> UpLinkedTypes =
    [
        "use-case", "reaction", "job", "notification", "setting",
    ];

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
