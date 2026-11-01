namespace LiteSpec.Model.Diff;

/// <summary>Compares two rule records: the kind, and whether a kind change weakens the rule.</summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
/// <follows>litespec/core/release#review-behavior-changes-through-the-behavior-diff</follows>
static class RuleDiffer
{
    /// <summary>Compares a rule, adding detail lines and breaking reasons.</summary>
    /// <param name="before">The base rule.</param>
    /// <param name="after">The head rule.</param>
    /// <param name="approved">Whether the base rule stands approved.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    /// <param name="reasons">The breaking reasons a change adds to.</param>
    public static void CompareRule(
        Record before, Record after, bool approved, List<string> details, List<BreakingReason> reasons)
    {
        var handled = new HashSet<string>(StringComparer.Ordinal) { RecordFields.Kind };
        var oldKind = before.Field(RecordFields.Kind);
        var newKind = after.Field(RecordFields.Kind);

        if (!string.Equals(oldKind, newKind, StringComparison.Ordinal))
        {
            details.Add($"Rule kind changed from {oldKind ?? "none"} to {newKind ?? "none"}.");

            if (approved && Weakened(oldKind, newKind))
            {
                reasons.Add(Differ.RuleWeakened);
            }
        }

        DiffValues.CompareFields(before, after, details, handled);
    }

    /// <summary>
    ///     Whether the kind change weakens the rule: an invariant always holds, so any
    ///     move away from it narrows what it promises, as does a move from a
    ///     precondition to a calculation or a cross-aggregate rule.
    /// </summary>
    /// <param name="oldKind">The base kind, as written.</param>
    /// <param name="newKind">The head kind, as written.</param>
    /// <returns>True when the kind change weakens the rule.</returns>
    public static bool Weakened(string? oldKind, string? newKind) =>
        oldKind switch
        {
            "invariant" => newKind != "invariant",
            "precondition" => newKind is "calculation" or "cross-aggregate",
            _ => false,
        };
}
