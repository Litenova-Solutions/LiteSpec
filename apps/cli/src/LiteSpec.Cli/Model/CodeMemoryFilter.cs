namespace LiteSpec.Model;

/// <summary>One LINQ filter a query handler applies to the result of a repository list call.</summary>
sealed record CodeMemoryFilter(string File, int Line);
