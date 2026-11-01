namespace LiteSpec.Model;

/// <summary>One review batch: a use case, a reaction, a job, a decision, or one module and type.</summary>
/// <param name="Selector">The batch selector.</param>
/// <param name="Kind">The batch kind: use-case, reaction, job, decision, record (one tier 1 record on its own), or module-type.</param>
/// <param name="Tier">The batch tier: 1 when any record is tier 1, else 3.</param>
/// <param name="Deciders">The distinct deciders, sorted.</param>
/// <param name="DecideBy">The earliest decide-by date, or null.</param>
/// <param name="Awaiting">The count of distinct drafts awaiting the batch.</param>
/// <param name="Records">The queued records, anchor first, then in authority order.</param>
sealed record ReviewBatch(
    string Selector,
    string Kind,
    int Tier,
    IReadOnlyList<string> Deciders,
    string? DecideBy,
    int Awaiting,
    IReadOnlyList<ReviewEntry> Records);
