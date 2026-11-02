namespace LiteSpec.Model;

/// <summary>One review batch: a use case, a reaction, a job, a decision, or one module and type.</summary>
/// <param name="Selector">The batch selector.</param>
/// <param name="Kind">The batch kind: use-case, reaction, job, decision, record (one record carrying a risk class on its own), or module-type.</param>
/// <param name="RiskClasses">The risk classes any record in the batch carries, sorted, empty when none carries risk.</param>
/// <param name="Deciders">The distinct deciders, sorted.</param>
/// <param name="DecideBy">The earliest decide-by date, or null.</param>
/// <param name="Awaiting">The count of distinct drafts awaiting the batch.</param>
/// <param name="Records">The queued records, anchor first, then in authority order.</param>
sealed record ReviewBatch(
    string Selector,
    string Kind,
    IReadOnlyList<string> RiskClasses,
    IReadOnlyList<string> Deciders,
    string? DecideBy,
    int Awaiting,
    IReadOnlyList<ReviewEntry> Records)
{
    /// <summary>Gets a value indicating whether any record in the batch carries a risk class.</summary>
    public bool HasRisk => RiskClasses.Count > 0;
}
