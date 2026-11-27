using LiteSpec.Standards;

namespace LiteSpec.Cli.Rules;

/// <summary>
///     One retrieved provision with why it was retrieved. A waiver the target
///     falls under replaces the covered provision's text in place.
/// </summary>
/// <param name="Entry">The provision or convention.</param>
/// <param name="Reasons">Why the entry was retrieved, in first-match order.</param>
/// <param name="Waiver">The live waiver replacing the entry, or null.</param>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal sealed record ProvisionMatch(
    ProvisionEntry Entry,
    IReadOnlyList<string> Reasons,
    WaiverFiles.Waiver? Waiver)
{
    /// <summary>Gets the printed one-line statement, or the waiver replacement.</summary>
    public string Statement => Waiver is null
        ? Entry.Statement
        : $"[waiver {Waiver.Id}{(Waiver.ExpiresOn.Length > 0 ? $", expires {Waiver.ExpiresOn}" : "")}] {Waiver.Reason}";

    /// <summary>Gets the printed rationale, or the waiver consequences.</summary>
    public string Rationale => Waiver is null ? Entry.Rationale : Waiver.Consequences;

    /// <summary>Gets the printed example, or empty for a waiver.</summary>
    public string Example => Waiver is null ? Entry.Example : "";

    /// <summary>Gets a value indicating whether a tool checks the provision and no waiver replaces it, so it prints as one checked line.</summary>
    public bool IsChecked => Waiver is null && Entry.IsChecked;

    /// <summary>Gets a value indicating whether the provision governs only through its concern, so it prints by identifier under the layer-wide heading.</summary>
    public bool IsConcernWide => Waiver is null
        && !Entry.IsChecked
        && Entry.Source == ProvisionEntry.LiteSpecSource
        && (Entry.AppliesTo.Length == 0 || Scopes.ArtifactValues(Entry.AppliesTo).All(value => Scopes.IsConcernWide(value)))
        && Reasons.Count > 0
        && Reasons.All(reason => reason.StartsWith("concern ", StringComparison.Ordinal));

    /// <summary>Gets a value indicating whether the provision matches only through marked records, so a code path collapses it into one line per record.</summary>
    public bool MatchesOnlyThroughMarkedRecord =>
        Reasons.Count > 0
        && Reasons.All(reason => reason.StartsWith("record ", StringComparison.Ordinal)
            && !Provisions.IsProvisionId(reason["record ".Length..]));

    /// <summary>Gets the marked record identifiers the match came through.</summary>
    public IReadOnlyList<string> RecordIds => Reasons
        .Where(reason => reason.StartsWith("record ", StringComparison.Ordinal))
        .Select(reason => reason["record ".Length..])
        .Where(id => !Provisions.IsProvisionId(id))
        .Distinct(StringComparer.Ordinal)
        .ToList();
}
