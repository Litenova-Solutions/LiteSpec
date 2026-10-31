namespace LiteSpec.Model;

/// <summary>One batch plan: the ordered approvals, the left-out records, the guard failures, and the sample.</summary>
/// <param name="Batch">The queued batch.</param>
/// <param name="Ordered">Every batch record in approval order, held records included.</param>
/// <param name="LeftOut">The tier 1 records a module and type batch leaves out.</param>
/// <param name="Failures">The guard failures, each naming its record and guard.</param>
/// <param name="Sample">The sampled tier 3 ids.</param>
sealed record BatchPlan(
    ReviewBatch Batch,
    IReadOnlyList<BatchAction> Ordered,
    IReadOnlyList<Record> LeftOut,
    IReadOnlyList<(string Id, string Guard)> Failures,
    IReadOnlyList<string> Sample);
