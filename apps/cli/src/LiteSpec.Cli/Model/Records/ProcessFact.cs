namespace LiteSpec.Cli.Model.Records;

/// <summary>One aggregate data class an external system processes.</summary>
/// <param name="Aggregate">The aggregate identifier the system holds data of.</param>
/// <param name="DataClass">The data class name within that aggregate.</param>
sealed record ProcessFact(string Aggregate, string DataClass);
