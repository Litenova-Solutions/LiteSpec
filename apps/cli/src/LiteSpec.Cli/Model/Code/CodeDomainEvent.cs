namespace LiteSpec.Cli.Model.Code;

/// <summary>One domain event class.</summary>
sealed record CodeDomainEvent(string Class, string File, int Line, string? EventId);
