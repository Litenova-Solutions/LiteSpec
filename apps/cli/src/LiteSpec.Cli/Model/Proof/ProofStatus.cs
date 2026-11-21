namespace LiteSpec.Cli.Model.Proof;

/// <summary>One scenario's computed proof status: its recorded run commit against HEAD.</summary>
/// <param name="Commit">The full run commit the ledger recorded, empty when the run recorded none.</param>
/// <param name="LastPassedAt">The short run commit the book shows after "last passed at", empty when the run recorded none.</param>
/// <param name="Status">Whether the proof is fresh or stale.</param>
/// <param name="CommitsBehind">How many commits HEAD sits ahead of the run commit when stale with changed code, or 0 when fresh or unreadable.</param>
/// <param name="StaleReason">Why the proof is stale, or null when it is fresh.</param>
sealed record ProofStatus(string Commit, string LastPassedAt, string Status, int CommitsBehind, string? StaleReason);
