namespace LiteSpec.Model.Diff;

/// <summary>One security queue entry: a change the security reviewer approves.</summary>
/// <param name="Id">The record identifier.</param>
/// <param name="Title">The record title.</param>
/// <param name="Type">The record type.</param>
/// <param name="Detail">What changed, in one sentence.</param>
sealed record SecurityQueueEntry(string Id, string Title, string Type, string Detail);
