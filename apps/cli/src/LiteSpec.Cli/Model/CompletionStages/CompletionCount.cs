namespace LiteSpec.Cli.Model.CompletionStages;

/// <summary>One labelled completion count: how many of a set are done.</summary>
/// <param name="Done">How many in the set are done.</param>
/// <param name="Of">How many the set holds.</param>
sealed record CompletionCount(int Done, int Of);
