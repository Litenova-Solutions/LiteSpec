namespace LiteSpec.Cli.Model.Proof;

/// <summary>One scenario's recorded proof: its hashes at the last passing run, that run, and its source commit.</summary>
/// <param name="Scenario">The scenario semantic hash at the last passing run.</param>
/// <param name="Test">The proving test files' hash at the last passing run.</param>
/// <param name="Run">The run identity that recorded the hashes.</param>
/// <param name="Method">The proving test methods' hash at the last passing run, empty when the ledger predates it.</param>
/// <param name="Marks">The hash of the files carrying the mark of the proved behavior at the last passing run, empty when the ledger predates it.</param>
/// <param name="Commit">The source commit the run was read at, empty when the consumer is not a git checkout.</param>
sealed record ProofEntry(string Scenario, string Test, string Run, string Method = "", string Marks = "", string Commit = "");
