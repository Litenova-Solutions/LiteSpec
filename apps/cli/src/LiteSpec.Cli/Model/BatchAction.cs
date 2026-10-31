namespace LiteSpec.Model;

/// <summary>One record's approval in a batch: what changes and what it carries.</summary>
/// <param name="Record">The batch record.</param>
/// <param name="Action">The action: promoted, re-stamped, or held.</param>
/// <param name="TargetStatus">The status the record holds after the batch.</param>
/// <param name="Hash">The semantic hash at approval time.</param>
/// <param name="Tier">The approval tier.</param>
/// <param name="Scenarios">The scenario evidence.</param>
/// <param name="Findings">The count of open findings about the record.</param>
/// <param name="HeldGuards">The guards holding the record, empty unless a module and type batch held it.</param>
sealed record BatchAction(
    Record Record,
    string Action,
    string TargetStatus,
    string Hash,
    int Tier,
    ReviewScenarios Scenarios,
    int Findings,
    IReadOnlyList<string> HeldGuards);
