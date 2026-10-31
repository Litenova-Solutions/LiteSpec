namespace LiteSpec.Model.Diff;

/// <summary>One named breaking reason: a stable code and its sentence.</summary>
/// <param name="Code">The stable reason code.</param>
/// <param name="Message">What the change breaks, in one sentence.</param>
sealed record BreakingReason(string Code, string Message);
