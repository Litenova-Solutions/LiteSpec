using LiteSpec.Authoring;

namespace LiteSpec.Rules;

/// <summary>
///     One retrieved provision with why it was retrieved. A departure the target
///     falls under replaces the covered provision's text in place.
/// </summary>
/// <param name="Entry">The provision or convention.</param>
/// <param name="Reasons">Why the entry was retrieved, in first-match order.</param>
/// <param name="Departure">The live departure replacing the entry, or null.</param>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal sealed record RuleResult(
    ProvisionEntry Entry,
    IReadOnlyList<string> Reasons,
    DepartureStore.Departure? Departure)
{
    /// <summary>Gets the printed one-line statement, or the departure replacement.</summary>
    public string Statement => Departure is null
        ? Entry.Statement
        : $"[departure {Departure.Id}{(Departure.ExpiresOn.Length > 0 ? $", expires {Departure.ExpiresOn}" : "")}] {Departure.Reason}";

    /// <summary>Gets the printed rationale, or the departure consequences.</summary>
    public string Rationale => Departure is null ? Entry.Rationale : Departure.Consequences;

    /// <summary>Gets the printed example, or empty for a departure.</summary>
    public string Example => Departure is null ? Entry.Example : "";

    /// <summary>Gets a value indicating whether a tool checks the provision and no departure replaces it, so it prints as one checked line.</summary>
    public bool IsChecked => Departure is null && Entry.IsChecked;

    /// <summary>Gets a value indicating whether the provision governs only through its layer, so it prints by identifier under the layer-wide heading.</summary>
    public bool IsLayerWide => Departure is null
        && !Entry.IsChecked
        && Entry.Source == ProvisionEntry.LiteSpecSource
        && (Entry.AppliesTo.Length == 0 || Scopes.ArtifactValues(Entry.AppliesTo).All(value => Scopes.IsLayerWide(value)))
        && Reasons.Count > 0
        && Reasons.All(reason => reason.StartsWith("layer ", StringComparison.Ordinal));
}
