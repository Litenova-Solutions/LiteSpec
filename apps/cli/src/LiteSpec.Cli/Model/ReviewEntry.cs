namespace LiteSpec.Model;

/// <summary>One record in the review queue, with its evidence.</summary>
/// <param name="Record">The queued record.</param>
/// <param name="Layer">The record layer: intent, behavior, or governance.</param>
/// <param name="Module">The batch module: the behavior module, intent, or governance.</param>
/// <param name="RiskClasses">The risk classes the record carries, empty when it carries none.</param>
/// <param name="Approval">The approval the project table gives the record, or null when it carries no risk class.</param>
/// <param name="EffectiveStatus">The builder's effective status.</param>
/// <param name="Reopened">Whether the approval hash no longer matches.</param>
/// <param name="FullReRead">Whether a reopened record needs a full re-read.</param>
/// <param name="Awaiting">The drafts whose effective status is draft because of this record.</param>
/// <param name="OpenQuestions">The count of open questions on the record.</param>
/// <param name="Scenarios">The scenario evidence.</param>
/// <param name="Findings">The count of open findings about the record.</param>
sealed record ReviewEntry(
    Record Record,
    string Layer,
    string Module,
    IReadOnlyList<string> RiskClasses,
    RecordApproval? Approval,
    string EffectiveStatus,
    bool Reopened,
    bool FullReRead,
    IReadOnlyList<string> Awaiting,
    int OpenQuestions,
    ReviewScenarios Scenarios,
    int Findings)
{
    /// <summary>Gets a value indicating whether the record carries any risk class.</summary>
    public bool HasRisk => RiskClasses.Count > 0;
}
