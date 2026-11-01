namespace LiteSpec.Model.Diff;

/// <summary>One parsed journey step: its statement, actor and failure.</summary>
/// <param name="Statement">The step statement.</param>
/// <param name="Actor">The actor carrying the step out, as written.</param>
/// <param name="Failure">What happens when the step fails, as written.</param>
sealed record JourneyStep(string Statement, string? Actor, string? Failure);
