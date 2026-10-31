namespace LiteSpec.Book;

/// <summary>One review batch for the queue shard: the selector, kind, tier and queued record ids.</summary>
/// <param name="Selector">The batch selector.</param>
/// <param name="Kind">The batch kind: use-case, reaction, job, decision, record, or module-type.</param>
/// <param name="Tier">The batch tier: 1 when any record is tier 1, else 3.</param>
/// <param name="Deciders">The distinct deciders, sorted.</param>
/// <param name="DecideBy">The earliest decide-by date, or null.</param>
/// <param name="Awaiting">The count of distinct drafts awaiting the batch.</param>
/// <param name="Records">The queued record ids, anchor first, then in authority order.</param>
sealed record BookBatch(
    string Selector,
    string Kind,
    int Tier,
    IReadOnlyList<string> Deciders,
    string? DecideBy,
    int Awaiting,
    IReadOnlyList<string> Records);
