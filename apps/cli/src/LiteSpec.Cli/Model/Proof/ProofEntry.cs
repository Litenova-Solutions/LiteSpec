namespace LiteSpec.Cli.Model.Proof;

/// <summary>One scenario's recorded proof: the commit its last passing run was read at, that run's outcome, and the run.</summary>
/// <param name="Commit">The source commit the passing run was read at, empty when the consumer is not a git checkout.</param>
/// <param name="Outcome">The outcome at the last passing run, as the test run writes it.</param>
/// <param name="Run">The run identity that recorded the commit.</param>
sealed record ProofEntry(string Commit, string Outcome, string Run);
