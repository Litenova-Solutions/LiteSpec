namespace LiteSpec.Cli.Book.Details;

/// <summary>The Intent no module reaches: the features and flows the book lists under Not yet fulfilled.</summary>
/// <param name="Features">The feature identifiers no module reaches, in identifier order.</param>
/// <param name="Flows">The flow identifiers no module reaches, in identifier order.</param>
sealed record UnfulfilledArea(IReadOnlyList<string> Features, IReadOnlyList<string> Flows);
