namespace LiteSpec.Model;

/// <summary>One record in the review queue, with its evidence.</summary>
/// <param name="Record">The queued record.</param>
/// <param name="Layer">The record layer: intent, behavior, or governance.</param>
/// <param name="Module">The batch module: the behavior module, intent, or governance.</param>
/// <param name="Tier">The approval tier: 1 needs a named person, 3 is sampled.</param>
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
    int Tier,
    string EffectiveStatus,
    bool Reopened,
    bool FullReRead,
    IReadOnlyList<string> Awaiting,
    int OpenQuestions,
    ReviewScenarios Scenarios,
    int Findings);
