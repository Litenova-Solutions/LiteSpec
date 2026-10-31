namespace LiteSpec.Book;

/// <summary>One scenario's proof state from its proves links.</summary>
/// <param name="State">The proof state key.</param>
/// <param name="Links">The enriched proves links.</param>
/// <param name="Claimed">Whether a test claims the scenario without holding the claim.</param>
sealed record ScenarioProofResult(string State, IReadOnlyList<ProofLink> Links, bool Claimed);
