namespace LiteSpec.Cli.Model.Code;

/// <summary>One outbox decision the backend read found for a domain event type.</summary>
/// <param name="EventType">The simple name of the decided event type.</param>
/// <param name="File">The absolute composition file path.</param>
/// <param name="Line">The 1-based line of the decision call.</param>
/// <param name="Ignored">Whether the decision drops the event; false when it translates it.</param>
sealed record CodeOutboxDecision(string EventType, string File, int Line, bool Ignored);
