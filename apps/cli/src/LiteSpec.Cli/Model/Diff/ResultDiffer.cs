namespace LiteSpec.Model.Diff;

/// <summary>Compares two result records: the outcome and the meaning.</summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
/// <follows>litespec/core/release#review-behavior-changes-through-the-behavior-diff</follows>
static class ResultDiffer
{
    /// <summary>Compares a result, adding detail lines and breaking reasons.</summary>
    /// <param name="before">The base result.</param>
    /// <param name="after">The head result.</param>
    /// <param name="approved">Whether the base result stands approved.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    /// <param name="reasons">The breaking reasons a change adds to.</param>
    public static void CompareResult(
        Record before, Record after, bool approved, List<string> details, List<BreakingReason> reasons)
    {
        var handled = new HashSet<string>(StringComparer.Ordinal) { RecordFields.Outcome, RecordFields.Meaning };
        var oldOutcome = before.Field(RecordFields.Outcome);
        var newOutcome = after.Field(RecordFields.Outcome);

        if (!string.Equals(oldOutcome, newOutcome, StringComparison.Ordinal))
        {
            details.Add($"Outcome changed from {oldOutcome ?? "none"} to {newOutcome ?? "none"}.");

            if (approved)
            {
                reasons.Add(Differ.ResultOutcomeChanged);
            }
        }

        if (DiffValues.CompareValue("Meaning", DiffValues.Field(before, RecordFields.Meaning), DiffValues.Field(after, RecordFields.Meaning), details)
            && approved
            && oldOutcome == "refusal"
            && newOutcome == "refusal")
        {
            reasons.Add(Differ.RefusalMeaningChanged);
        }

        DiffValues.CompareFields(before, after, details, handled);
    }
}
