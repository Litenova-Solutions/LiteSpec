using LiteSpec.Cli.Model.Proof;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Diff;

/// <summary>
///     Finds the proving tests changed since the base without their scenario: a scenario whose base
///     ledger recorded a proof, whose own hash has not moved, while the hash of its proving test
///     methods has. Each becomes an advisory proof-hash-drift finding on the scenario record.
/// </summary>
/// <follows>litespec/core/agent#protect-proofs-from-implementation-edits</follows>
static class ProofChanges
{
    /// <summary>Finds every scenario whose proving test method changed since the base while the scenario did not.</summary>
    /// <param name="baseModel">The base model, whose ledger holds the proof hashes recorded at the base.</param>
    /// <param name="headModel">The head model, whose proving test methods are hashed as they now stand.</param>
    /// <returns>The proof-hash-drift findings, ordered by scenario id.</returns>
    public static IReadOnlyList<Finding> Find(ProjectModel baseModel, ProjectModel headModel)
    {
        var findings = new List<Finding>();

        foreach (var (id, recorded) in baseModel.Ledger.Entries.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            // A ledger entry from before method hashes, or a scenario the base or head no longer holds, has nothing to compare.
            if (recorded.Method.Length == 0
                || !baseModel.ById.TryGetValue(id, out var before)
                || !headModel.ById.TryGetValue(id, out var after)
                || before.Type != RecordTypes.Scenario
                || after.Type != RecordTypes.Scenario
                || before.Derived
                || after.Derived)
            {
                continue;
            }

            // A scenario that changed too is the change it proves, so only a scenario held still is reported.
            if (Checker.SemanticHash(before) != Checker.SemanticHash(after)
                || ProofLedger.MethodHashOf(headModel, id) == recorded.Method)
            {
                continue;
            }

            findings.Add(new Finding(
                "proof-hash-drift",
                $"Proving test of scenario '{id}' changed without the scenario.",
                after.File,
                after.Line,
                id));
        }

        return findings;
    }
}
