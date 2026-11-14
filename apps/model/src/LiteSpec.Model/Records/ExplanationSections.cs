using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteSpec.Model;

/// <summary>
///     The fixed H2 sections each record type's explanation file holds, in order. The model
///     checks, the consumer page checks and the maintainer's page contracts read this one table,
///     so a section name is written once.
/// </summary>
/// <follows>litespec/core/writing#give-each-explanation-file-its-fixed-sections</follows>
public static class ExplanationSections
{
    /// <summary>One fixed section: its H2 name and whether it may be left out.</summary>
    /// <param name="Name">The H2 heading text, without the marker.</param>
    /// <param name="Optional">Whether the section may be left out when it has nothing to say.</param>
    public sealed record Section(string Name, bool Optional);

    /// <summary>Gets the fixed sections of each record type and container page style, in order.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<Section>> ByType =
        new Dictionary<string, IReadOnlyList<Section>>(StringComparer.Ordinal)
        {
            // Intent
            [RecordTypes.Product] = Of(("Scope", false), ("Users", false), ("Out of Scope", false), ("Direction", false)),
            [RecordTypes.Actor] = Of(("Responsibilities", false), ("Access", false), ("Example", true)),
            [RecordTypes.Role] = Of(("Access", false), ("Limits", false)),
            [RecordTypes.Goal] = Of(("Target", false), ("Measurement", false)),
            [RecordTypes.Vision] = Of(("Direction", false), ("Effect on Current Design", false), ("Trigger", false)),
            [RecordTypes.Problem] = Of(("Affected Users", false), ("Failure Cases", false), ("Example", true), ("Current Workaround", false), ("Cost", false), ("Evidence", false), ("Resolution Criteria", false)),
            [RecordTypes.Constraint] = Of(("Source", false), ("Effect on the Product", false), ("Open Points", true)),
            [RecordTypes.Research] = Of(("Findings", false), ("Source Notes", true)),
            [RecordTypes.Decision] = Of(("Context", false), ("Options", false), ("Decision", false), ("Consequences", false), ("Evidence", true), ("Revisit Conditions", false)),
            [RecordTypes.Feature] = Of(("Capabilities", false), ("Success Criteria", false), ("Boundaries", false), ("Example", true)),
            [RecordTypes.Requirement] = Of(("Example", true)),
            [RecordTypes.Journey] = Of(("Preconditions", true), ("Example", false), ("Variations", true)),

            // Behavior
            [RecordTypes.Module] = Of(("Responsibilities", false), ("Boundaries", false), ("Example", true)),
            [RecordTypes.Aggregate] = Of(("Lifecycle", false), ("Data and Retention", false), ("Example", true), ("Limits", true)),
            [RecordTypes.UseCase] = Of(("Flow", false), ("Example", true), ("Edge Cases", true)),
            [RecordTypes.RuleSet] = Of(("Rationale", false), ("Example", true)),
            [RecordTypes.Reaction] = Of(("Trigger and Effect", false), ("Failure Handling", false), ("Example", true)),
            [RecordTypes.Job] = Of(("Schedule", false), ("Failure Handling", false), ("Example", true)),
            [RecordTypes.Notification] = Of(("Content", false), ("Delivery", false), ("Example", false)),
            [RecordTypes.IntegrationEvent] = Of(("Consumers", false), ("Compatibility", false), ("Example", false)),
            [RecordTypes.ExternalSystem] = Of(("Use", false), ("Data Shared", false), ("Failure Handling", false)),
            [RecordTypes.EntryPoint] = Of(("Audience", false), ("Authentication", false), ("Limits", true)),
            [RecordTypes.Screen] = Of(("Task", false), ("States", false), ("Example", false)),
            [RecordTypes.PermissionList] = Of(("Risk", false), ("Future Roles", true)),
            [RecordTypes.SettingSet] = Of(("Defaults", false), ("Changes", true)),
            [RecordTypes.RetentionSchedule] = Of(("Legal Basis", false), ("Conflicts", false), ("Deletion", false)),

            // Implementation and Governance
            [RecordTypes.Runbook] = Of(("Prerequisites", false), ("Steps", false), ("Verification", false), ("Recovery", false)),
            [RecordTypes.Departure] = Of(("Provision", false), ("Reason", false), ("Risk", false), ("Expiry", false)),
        };

    /// <summary>Gets the H2 names a record type's explanation must hold, in order, with the optional sections left out.</summary>
    /// <param name="type">The record type or container page style.</param>
    /// <returns>The required section names in table order, or an empty list when the type has no table.</returns>
    public static IReadOnlyList<string> RequiredNames(string type) =>
        ByType.TryGetValue(type, out var sections)
            ? sections.Where(section => !section.Optional).Select(section => section.Name).ToList()
            : [];

    /// <summary>Builds one type's section list from its name and optional pairs, in the order given.</summary>
    /// <param name="sections">The section names and whether each may be left out.</param>
    /// <returns>The sections in the order given.</returns>
    static List<Section> Of(params (string Name, bool Optional)[] sections) =>
        sections.Select(section => new Section(section.Name, section.Optional)).ToList();
}
