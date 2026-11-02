namespace LiteSpec.Book;

/// <summary>One review batch for the queue shard: the selector, kind, risk classes, approvers and queued record ids.</summary>
/// <param name="Selector">The batch selector.</param>
/// <param name="Kind">The batch kind: use-case, reaction, job, decision, record, or module-type.</param>
/// <param name="RiskClasses">The risk classes the queued records carry, in model order, empty when none carries risk.</param>
/// <param name="Approvers">The approvers the project approval table names for those risk classes, sorted.</param>
/// <param name="Deciders">The distinct deciders, sorted.</param>
/// <param name="DecideBy">The earliest decide-by date, or null.</param>
/// <param name="Awaiting">The count of distinct drafts awaiting the batch.</param>
/// <param name="Records">The queued record ids, anchor first, then in authority order.</param>
sealed record BookBatch(
    string Selector,
    string Kind,
    IReadOnlyList<string> RiskClasses,
    IReadOnlyList<string> Approvers,
    IReadOnlyList<string> Deciders,
    string? DecideBy,
    int Awaiting,
    IReadOnlyList<string> Records);
