namespace LiteSpec.Model;

/// <summary>One scenario's recorded proof: its hashes at the last passing run, and that run.</summary>
/// <param name="Scenario">The scenario semantic hash at the last passing run.</param>
/// <param name="Test">The proving test files' hash at the last passing run.</param>
/// <param name="Run">The run identity that recorded the hashes.</param>
sealed record ProofEntry(string Scenario, string Test, string Run);
