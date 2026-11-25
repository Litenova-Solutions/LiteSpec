using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteSpec.Model;

/// <summary>
///     The fixed H2 sections each record type's explanation file holds, in order. Each section
///     names the record fields it explains or elaborates; the model checks, the consumer page
///     checks and the maintainer's page contracts read this one table, so a section name is
///     written once. A section with an empty field list is computed or an illustration, and the
///     unbacked-section check skips it: Capabilities and Consumers restate computed facts, and
///     Example only illustrates.
/// </summary>
/// <follows>litespec/core/writing#give-each-explanation-file-its-fixed-sections</follows>
/// <follows>litespec/core/model#keep-the-whole-specification-in-the-records</follows>
public static class ExplanationSections
{
    /// <summary>One fixed section: its H2 name, whether it may be left out, and the record fields it explains.</summary>
    /// <param name="Name">The H2 heading text, without the marker.</param>
    /// <param name="Optional">Whether the section may be left out when it has nothing to say.</param>
    /// <param name="Fields">The record fields the section explains. Empty for computed or illustration sections, which the unbacked check skips.</param>
    public sealed record Section(string Name, bool Optional, IReadOnlyList<string> Fields);

    /// <summary>Gets the fixed sections of each record type and container page style, in order.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<Section>> ByType =
        new Dictionary<string, IReadOnlyList<Section>>(StringComparer.Ordinal)
        {
            // Intent
            [RecordTypes.Product] = Of(("Scope", false, new[] { "boundary" }), ("Users", false, new[] { "users" }), ("Out of Scope", false, new[] { "excludes" })),
            [RecordTypes.Actor] = Of(("Responsibilities", false, new[] { "responsibilities" }), ("Knowledge", false, new[] { "knowledge" }), ("Example", true, [])),
            [RecordTypes.Role] = Of(("Access", false, new[] { "scope" }), ("Excludes", false, new[] { "excludes" })),
            [RecordTypes.Goal] = Of(("Target", false, new[] { "target" }), ("Measurement", false, new[] { "measure", "observations" })),
            [RecordTypes.Direction] = Of(),
            [RecordTypes.Problem] = Of(("Affected Users", false, new[] { "affects" }), ("Failure Cases", false, new[] { "failureCases" }), ("Example", true, []), ("Current Workaround", false, new[] { "workaround" }), ("Cost", false, new[] { "cost" }), ("Evidence", false, new[] { "evidence" }), ("Resolution Criteria", false, new[] { "resolutionCriteria" })),
            [RecordTypes.Constraint] = Of(("Scope", false, new[] { "coverage" }), ("Rules", false, new[] { "obligations" }), ("Effect on the Product", false, new[] { "productEffect" }), ("Open Points", true, new[] { "openPoints" })),
            [RecordTypes.Research] = Of(("Findings", false, new[] { "findings" }), ("Open Questions", true, new[] { "openQuestions" }), ("Source Notes", true, new[] { "sources" })),
            [RecordTypes.Decision] = Of(("Context", false, new[] { "context" }), ("Options", false, new[] { "options" }), ("Decision", false, new[] { "choice" }), ("Consequences", false, new[] { "consequences" }), ("Evidence", true, new[] { "basedOn" }), ("Revisit Conditions", false, new[] { "reopenedWhen" })),
            [RecordTypes.Feature] = Of(("Capabilities", false, []), ("Success Criteria", false, new[] { "successCriteria" }), ("Boundaries", false, new[] { "excludes" }), ("Example", true, [])),
            [RecordTypes.Requirement] = Of(("Example", true, [])),
            [RecordTypes.Flow] = Of(("Example", false, []), ("Branches", true, new[] { "branches" })),

            // Behavior
            [RecordTypes.Module] = Of(("Covers", false, new[] { "covers" }), ("Boundaries", false, new[] { "excludes" }), ("Example", true, [])),
            [RecordTypes.KnowledgeArea] = Of(("Variants", false, new[] { "variants" }), ("Example", true, [])),
            [RecordTypes.Aggregate] = Of(("Lifecycle", false, new[] { "states", "transitions" }), ("Data and Retention", false, new[] { "data", "dataClasses" }), ("Example", true, []), ("Excludes", true, new[] { "excludes" })),
            [RecordTypes.UseCase] = Of(("Flow", false, new[] { "steps" }), ("Example", true, []), ("Edge Cases", true, new[] { "edgeCases" })),
            [RecordTypes.Rule] = Of(("Rationale", false, new[] { "rationale", "basedOn", "cites" }), ("Example", true, []), ("Limits", true, [])),
            [RecordTypes.DomainEvent] = Of(("Example", true, [])),
            [RecordTypes.Reaction] = Of(("Trigger and Effect", false, new[] { "triggeredBy", "effects", "outcomes" }), ("Failure Handling", false, new[] { "onFailure" }), ("Example", true, [])),
            [RecordTypes.Job] = Of(("Invocation", false, new[] { "kind", "schedule", "runsWhen" }), ("Failure Handling", false, new[] { "onFailure" }), ("Example", true, [])),
            [RecordTypes.Notification] = Of(("Content", false, new[] { "template", "parameters", "locale" }), ("Delivery", false, new[] { "channel", "deliveredThrough", "frequency" }), ("Example", false, [])),
            [RecordTypes.ModuleEvent] = Of(("Consumers", false, []), ("Compatibility", false, new[] { "compatibility" }), ("Example", true, [])),
            [RecordTypes.IntegrationEvent] = Of(("Consumers", false, []), ("Compatibility", false, new[] { "compatibility" }), ("Example", false, [])),
            [RecordTypes.ReadModel] = Of(("Sources", false, new[] { "fields", "keyedBy" }), ("Freshness", false, new[] { "freshness" }), ("Example", true, [])),
            [RecordTypes.ReferenceData] = Of(("Values", false, new[] { "unit", "rows", "row", "columns", "choices", "valueMeanings", "singleton" }), ("Example", true, [])),
            [RecordTypes.ExternalSystem] = Of(("Use", false, new[] { "summary" }), ("Data Shared", false, new[] { "processes", "erasure" }), ("Failure Handling", false, new[] { "operations" })),
            [RecordTypes.EntryPoint] = Of(("Audience", false, new[] { "audience" }), ("Authentication", false, new[] { "authentication", "session", "reauthenticationTriggers" }), ("Excludes", true, new[] { "excludes" })),
            [RecordTypes.Screen] = Of(("Task", false, new[] { "task" }), ("States", false, new[] { "displayStates" }), ("Example", false, [])),
            [RecordTypes.PermissionSet] = Of(("Risk", false, new[] { "risk" }), ("Boundaries", true, new[] { "excludes" })),
            [RecordTypes.Setting] = Of(("Defaults", false, new[] { "default", "range", "unit", "choices", "valueMeanings" }), ("Changes", true, new[] { "setBy" })),

            // Implementation and Governance
            [RecordTypes.Runbook] = Of(("Prerequisites", false, new[] { "prerequisites" }), ("Steps", false, new[] { "procedure" }), ("Verification", false, new[] { "verification" }), ("Recovery", false, new[] { "recovery" })),
            [RecordTypes.Waiver] = Of(("Provision", false, new[] { "waives" }), ("Reason", false, new[] { "reason" }), ("Risk", false, new[] { "consequences" }), ("Expiry", false, new[] { "expiresOn", "exitWhen" })),
        };

    /// <summary>Gets the H2 names a record type's explanation must hold, in order, with the optional sections left out.</summary>
    /// <param name="type">The record type or container page style.</param>
    /// <returns>The required section names in table order, or an empty list when the type has no table.</returns>
    public static IReadOnlyList<string> RequiredNames(string type) =>
        ByType.TryGetValue(type, out var sections)
            ? sections.Where(section => !section.Optional).Select(section => section.Name).ToList()
            : [];

    /// <summary>Builds one type's section list from its name, optional and fields, in the order given.</summary>
    /// <param name="sections">The section names, whether each may be left out, and the fields it explains.</param>
    /// <returns>The sections in the order given.</returns>
    static List<Section> Of(params (string Name, bool Optional, string[] Fields)[] sections) =>
        sections.Select(section => new Section(section.Name, section.Optional, section.Fields)).ToList();
}
