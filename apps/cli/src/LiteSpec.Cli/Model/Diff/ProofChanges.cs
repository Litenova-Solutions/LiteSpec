using LiteSpec.Cli.Model.Proof;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Diff;

/// <summary>
///     Finds the proving tests changed since the base without their scenario: a scenario both trees
///     hold, whose own hash has not moved, while the hash of its proving test methods has. Each
///     becomes an advisory proof-hash-drift finding on the scenario record.
/// </summary>
/// <follows>litespec/core/agent#protect-proofs-from-implementation-edits</follows>
static class ProofChanges
{
    /// <summary>Finds every scenario whose proving test method changed since the base while the scenario did not.</summary>
    /// <param name="baseModel">The base model, whose proving test methods are hashed as they stood at the base.</param>
    /// <param name="headModel">The head model, whose proving test methods are hashed as they now stand.</param>
    /// <returns>The proof-hash-drift findings, ordered by scenario id.</returns>
    public static IReadOnlyList<Finding> Find(ProjectModel baseModel, ProjectModel headModel)
    {
        var findings = new List<Finding>();

        var scenarios = baseModel.Records
            .Where(record => record.Type == RecordTypes.Scenario && !record.Derived)
            .Select(record => record.Id)
            .OrderBy(id => id, StringComparer.Ordinal);

        foreach (var id in scenarios)
        {
            var baseMethods = MethodHashOf(baseModel, id);

            // A scenario with no proving test at the base, or one the head no longer holds, has nothing to compare.
            if (baseMethods.Length == 0
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
                || MethodHashOf(headModel, id) == baseMethods)
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

    /// <summary>
    ///     The hash of a scenario's proving test methods: each method's hash in test name order, so an edit
    ///     to a proving method moves it and an edit elsewhere in its file does not. A scenario with no
    ///     confirmed proving test method hashes as empty.
    /// </summary>
    /// <param name="model">The built project model holding the proves links and tests.</param>
    /// <param name="scenario">The scenario id whose proving test methods are hashed.</param>
    /// <returns>The sixteen-hex-character hash, or empty when no test proves the scenario.</returns>
    static string MethodHashOf(ProjectModel model, string scenario)
    {
        var methods = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var link in model.Proves.Where(link => link.Claim == scenario && link.Confirmed))
        {
            foreach (var test in model.Tests.Where(test => test.Name == link.Scenario))
            {
                methods[test.Name] = test.MethodHash;
            }
        }

        if (methods.Count == 0)
        {
            return string.Empty;
        }

        var hasher = new Sha256();

        foreach (var (name, hash) in methods)
        {
            hasher.Append(System.Text.Encoding.UTF8.GetBytes(name + "\n" + hash + "\n"));
        }

        return Convert.ToHexString(hasher.Finish()).ToLowerInvariant()[..16];
    }
}
