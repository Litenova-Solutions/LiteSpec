namespace LiteSpec.Model;

/// <summary>One domain event class.</summary>
sealed record DomainEventInfo(string Class, string File, int Line, string? EventId);
