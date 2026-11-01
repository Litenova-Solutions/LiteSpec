namespace LiteSpec.Model;

/// <summary>One public method on an aggregate root with the domain events it raises.</summary>
sealed record AggregateMethod(string Aggregate, string Method, IReadOnlyList<string> Events, string File, int Line)
{
    /// <summary>Gets the use-case marks the method carries, in source order.</summary>
    public IReadOnlyList<string> UseCases { get; init; } = [];
}
