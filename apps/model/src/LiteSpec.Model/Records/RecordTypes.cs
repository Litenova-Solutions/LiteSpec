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

    /// <summary>Gets the `entry-point-set` page style name.</summary>
    public const string EntryPointSet = "entry-point-set";

    /// <summary>Gets the `domain-event` record type name.</summary>
    public const string DomainEvent = "domain-event";

    /// <summary>Gets the `example-cast` record type name.</summary>
    public const string ExampleCast = "example-cast";

    /// <summary>Gets the `read-model` record type name.</summary>
    public const string ReadModel = "read-model";

    /// <summary>Gets the `vision` record type name.</summary>
    public const string Vision = "vision";

    /// <summary>Gets the `waiver` record type name.</summary>
    public const string Waiver = "waiver";

    /// <summary>Gets the `external-system` record type name.</summary>
    public const string ExternalSystem = "external-system";

    /// <summary>Gets the `feature` record type name.</summary>
    public const string Feature = "feature";

    /// <summary>Gets the `flow` record type name.</summary>
    public const string Flow = "flow";

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

    /// <summary>Gets the `module` record type name.</summary>
    public const string Module = "module";

    /// <summary>Gets the `notification` record type name.</summary>
    public const string Notification = "notification";

    /// <summary>Gets the `permission` record type name.</summary>
    public const string Permission = "permission";

    /// <summary>Gets the `permission-set` page style name.</summary>
    public const string PermissionSet = "permission-set";

    /// <summary>Gets the `problem` record type name.</summary>
    public const string Problem = "problem";

    /// <summary>Gets the `product` record type name.</summary>
    public const string Product = "product";

    /// <summary>Gets the `reaction` record type name.</summary>
    public const string Reaction = "reaction";

    /// <summary>Gets the `reference` record type name.</summary>
    public const string Reference = "reference";

    /// <summary>Gets the `refusal-set` page style name: one module's refusal codes.</summary>
    public const string RefusalSet = "refusal-set";

    /// <summary>Gets the `requirement` record type name.</summary>
    public const string Requirement = "requirement";

    /// <summary>Gets the `research` record type name.</summary>
    public const string Research = "research";

    /// <summary>Gets the `result` record type name.</summary>
    public const string Result = "result";

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

    /// <summary>Gets the `tutorial` record type name.</summary>
    public const string Tutorial = "tutorial";

    /// <summary>Gets the `use-case` record type name.</summary>
    public const string UseCase = "use-case";

    /// <summary>Every written record type.</summary>
    public static readonly HashSet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        "product", "goal", "vision", "constraint", "research", "decision", "requirement", "problem",
        "feature", "actor", "role", "permission", "flow", "module",
        "aggregate", "rule", "use-case", "result", "scenario", "domain-event", "integration-event",
        "reaction", "job", "notification", "setting", "read-model",
        "entry-point", "screen", "external-system", "runbook", "waiver", "term",
    };

    /// <summary>
    ///     The record layer of each written type: root for the product, which stands above
    ///     the record layers, and intent, behavior or governance for the rest. A term
    ///     names its own layer in its layer field; this table gives it intent by default.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> LayerByType =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["product"] = "root",
            ["goal"] = "intent", ["vision"] = "intent", ["constraint"] = "intent",
            ["research"] = "intent", ["decision"] = "intent", ["requirement"] = "intent",
            ["problem"] = "intent", ["feature"] = "intent",
            ["actor"] = "intent", ["flow"] = "intent", ["term"] = "intent",
            ["module"] = "behavior", ["aggregate"] = "behavior", ["rule"] = "behavior",
            ["use-case"] = "behavior", ["result"] = "behavior", ["scenario"] = "behavior",
            ["domain-event"] = "behavior", ["integration-event"] = "behavior", ["reaction"] = "behavior",
            ["job"] = "behavior", ["notification"] = "behavior", ["setting"] = "behavior", ["read-model"] = "behavior",
            ["role"] = "behavior", ["permission"] = "behavior", ["entry-point"] = "behavior",
            ["screen"] = "behavior", ["external-system"] = "behavior", ["runbook"] = "behavior",
            ["waiver"] = "governance",
        };

    /// <summary>Page styles: templates a page may carry instead of a record.</summary>
    public static readonly HashSet<string> PageStyles = new HashSet<string>(StringComparer.Ordinal)
    {
        "permission-set", "entry-point-set", "setting-set", "refusal-set",
        "rule-set", "index", "tutorial", "how-to", "reference", "command",
        "configuration", "glossary", "example-cast",
    };

    /// <summary>Container page styles: record files that hold entries without a record of their own.</summary>
    public static readonly HashSet<string> ContainerStyles = new HashSet<string>(StringComparer.Ordinal)
    {
        PermissionSet, EntryPointSet, SettingSet, RuleSet, RefusalSet,
    };

    /// <summary>Types whose identifier carries a home: type/home.name.</summary>
    public static readonly HashSet<string> WithHome = new HashSet<string>(StringComparer.Ordinal)
    {
        "requirement", "aggregate", "rule", "use-case", "result",
        "scenario", "domain-event", "reaction", "job", "notification", "setting", "screen", "read-model",
    };

    /// <summary>Types the small size requires.</summary>
    public static readonly IReadOnlyList<string> SmallRequired =
    [
        "product", "decision", "requirement", "actor", "role", "permission",
        "module", "aggregate", "rule", "use-case", "result", "domain-event",
        "entry-point", "term",
    ];

    /// <summary>Types only the large size requires.</summary>
    public static readonly IReadOnlyList<string> LargeAdded =
    [
        "goal", "problem", "feature", "flow", "scenario",
    ];

    /// <summary>
    ///     Whether the field names a link, and which record types it may point at.
    ///     An empty set means any record type. A serves target of flow/&lt;name&gt;#&lt;step&gt;
    ///     resolves through its flow, and a calls target of
    ///     external-system/&lt;name&gt;#&lt;operation&gt; through its system.
    ///     A flow's serves names one goal and its answers one problem or constraint; only serves moves a goal.
    /// </summary>
    public static readonly Dictionary<string, HashSet<string>> LinkTargets =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["decides"] = new HashSet<string>(),
            ["cites"] = new HashSet<string> { "goal", "vision", "problem", "feature", "constraint", "research", "decision", "requirement" },
            ["basedOn"] = new HashSet<string> { "constraint", "research", "vision", "decision", "requirement" },
            ["affects"] = new HashSet<string> { "actor" },
            ["audience"] = new HashSet<string> { "actor" },
            ["requires"] = new HashSet<string> { "actor", "permission" },
            ["advances"] = new HashSet<string> { "goal" },
            ["serves"] = new HashSet<string> { "goal", "flow" },
            ["answers"] = new HashSet<string> { "problem", "constraint" },
            ["solves"] = new HashSet<string> { "problem" },
            ["fulfills"] = new HashSet<string> { "requirement" },
            ["governs"] = new HashSet<string> { "aggregate", "use-case" },
            ["raises"] = new HashSet<string> { "domain-event" },
            ["runs"] = new HashSet<string> { "use-case" },
            ["sends"] = new HashSet<string> { "notification" },
            ["calls"] = new HashSet<string> { "external-system" },
            ["triggeredBy"] = new HashSet<string> { "domain-event", "integration-event" },
            ["compensates"] = new HashSet<string> { "use-case" },
            ["decidedBy"] = new HashSet<string> { "use-case", "aggregate" },
            ["closedBy"] = new HashSet<string>(),
            ["writes"] = new HashSet<string> { "aggregate" },
            ["keyedBy"] = new HashSet<string> { "aggregate" },
            ["from"] = new HashSet<string> { "integration-event" },
            ["erasedBy"] = new HashSet<string> { "integration-event" },
            ["removedBy"] = new HashSet<string> { "integration-event" },
            ["reads"] = new HashSet<string> { "aggregate", "setting", "read-model" },
            ["translates"] = new HashSet<string> { "domain-event" },
            ["through"] = new HashSet<string> { "external-system" },
            ["arrivesThrough"] = new HashSet<string> { "entry-point", "external-system" },
            ["deliveredThrough"] = new HashSet<string> { "external-system" },
            ["requiresConsent"] = new HashSet<string> { "rule" },
            ["movedBy"] = new HashSet<string> { "job" },
            ["fallback"] = new HashSet<string> { "requirement" },
            ["releasedBy"] = new HashSet<string> { "use-case" },
            ["computedBy"] = new HashSet<string> { "rule" },
            ["enforcedBy"] = new HashSet<string> { "job", "use-case" },
            ["effects"] = new HashSet<string> { "use-case", "notification", "external-system" },
            ["access"] = new HashSet<string> { "permission" },
            ["except"] = new HashSet<string> { "requirement" },
            ["dataClasses"] = new HashSet<string> { "job", "use-case", "decision", "requirement" },
            ["freshness"] = new HashSet<string>(),
            ["erasure"] = new HashSet<string> { "external-system", "integration-event" },
            ["allows"] = new HashSet<string> { "permission" },
            ["reachedThrough"] = new HashSet<string> { "entry-point" },
            ["dependsOn"] = new HashSet<string> { "module" },
            ["terms"] = new HashSet<string> { "term" },
            ["result"] = new HashSet<string> { "result" },
            ["actor"] = new HashSet<string> { "actor" },
            ["offers"] = new HashSet<string> { "use-case" },
            ["host"] = new HashSet<string> { "screen" },
            ["leadsTo"] = new HashSet<string> { "screen" },
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
        "rule", "use-case", "reaction", "job", "notification", "setting", "entry-point",
    ];

    /// <summary>
    ///     The record types that serve a flow step, in message order: the Written on
    ///     column of the serves row for flow steps, except the screen. The step check reads this table.
    /// </summary>
    public static readonly IReadOnlyList<string> StepServingTypes =
    [
        "use-case", "reaction", "job",
    ];

    /// <summary>
    ///     The record types that link up with serves or fulfills, in message order:
    ///     every fulfilling type except a rule and an entry point. The orphan check reads this table.
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
        "type", "id", "title", "code",
    };
}
