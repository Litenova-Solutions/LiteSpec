namespace LiteSpec.Cli.Model.Checks;

/// <summary>One aggregate transition: the states it leaves and enters, and the job or use case that moves it, when it names one.</summary>
internal sealed record AggregateTransition(string From, string To, string? MovedBy);
