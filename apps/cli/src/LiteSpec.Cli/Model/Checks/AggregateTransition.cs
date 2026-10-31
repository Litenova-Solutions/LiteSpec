namespace LiteSpec.Model.Checks;

/// <summary>One aggregate transition: the states it leaves and enters, and the use case behind it.</summary>
internal sealed record AggregateTransition(string From, string To, string? By);
