namespace LiteSpec.Model.Diff;

/// <summary>Compares two requirement records: statement, measure, target, and exceptions.</summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
/// <follows>litespec/core/release#review-behavior-changes-through-the-behavior-diff</follows>
static class RequirementDiffer
{
    /// <summary>Compares a requirement, adding detail lines and breaking reasons.</summary>
    /// <param name="before">The base requirement.</param>
    /// <param name="after">The head requirement.</param>
    /// <param name="approved">Whether the base requirement stands approved.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    /// <param name="reasons">The breaking reasons a change adds to.</param>
    public static void CompareRequirement(
        Record before, Record after, bool approved, List<string> details, List<BreakingReason> reasons)
    {
        var handled = new HashSet<string>(StringComparer.Ordinal) { RecordFields.Statement, RecordFields.Measure, RecordFields.DueBy, RecordFields.Except };

        if (DiffValues.CompareValue("Statement", DiffValues.Field(before, RecordFields.Statement), DiffValues.Field(after, RecordFields.Statement), details) && approved)
        {
            reasons.Add(Differ.RequirementNarrowed);
        }

        var measureChanged = DiffValues.CompareValue("Measure", DiffValues.Field(before, RecordFields.Measure), DiffValues.Field(after, RecordFields.Measure), details);
        var targetChanged = DiffValues.CompareValue("Due by", DiffValues.Field(before, RecordFields.DueBy), DiffValues.Field(after, RecordFields.DueBy), details);

        if ((measureChanged || targetChanged) && approved && before.Field(RecordFields.Kind) == "quality")
        {
            reasons.Add(Differ.QualityMeasureChanged);
        }

        CompareExcept(before, after, details);
        DiffValues.CompareFields(before, after, details, handled);
    }

    /// <summary>Compares a requirement's exceptions, keyed by condition.</summary>
    /// <param name="before">The base requirement.</param>
    /// <param name="after">The head requirement.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    public static void CompareExcept(Record before, Record after, List<string> details)
    {
        var oldExcept = DiffEntries.ExceptEntries(before);
        var newExcept = DiffEntries.ExceptEntries(after);

        if (oldExcept is null || newExcept is null)
        {
            DiffValues.CompareValue("Except", DiffValues.Field(before, RecordFields.Except), DiffValues.Field(after, RecordFields.Except), details);
            return;
        }

        foreach (var condition in oldExcept.Keys.Where(condition => !newExcept.ContainsKey(condition))
                     .OrderBy(condition => condition, StringComparer.Ordinal))
        {
            details.Add($"Exception removed: \"{condition}\".");
        }

        foreach (var condition in newExcept.Keys.Where(condition => !oldExcept.ContainsKey(condition))
                     .OrderBy(condition => condition, StringComparer.Ordinal))
        {
            details.Add($"Exception added: \"{condition}\", falling back to {newExcept[condition]}.");
        }

        foreach (var condition in oldExcept.Keys.Where(newExcept.ContainsKey)
                     .OrderBy(condition => condition, StringComparer.Ordinal))
        {
            if (!string.Equals(oldExcept[condition], newExcept[condition], StringComparison.Ordinal))
            {
                details.Add($"Exception \"{condition}\" now falls back to {newExcept[condition]} (was: {oldExcept[condition]}).");
            }
        }
    }
}
