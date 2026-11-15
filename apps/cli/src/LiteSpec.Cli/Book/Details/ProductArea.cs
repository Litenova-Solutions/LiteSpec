namespace LiteSpec.Cli.Book.Details;

/// <summary>One product area: a Behavior module with the features and flows its use cases, reactions, jobs and screens reach.</summary>
/// <param name="Id">The module identifier.</param>
/// <param name="Title">The module title.</param>
/// <param name="Features">The feature identifiers the module reaches, in identifier order.</param>
/// <param name="Flows">The flow identifiers the module reaches, in identifier order.</param>
sealed record ProductArea(string Id, string Title, IReadOnlyList<string> Features, IReadOnlyList<string> Flows);
