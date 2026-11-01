namespace LiteSpec.Model.Diff;

/// <summary>Compares two decision records: the choice and the options.</summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
/// <follows>litespec/core/release#review-behavior-changes-through-the-behavior-diff</follows>
static class DecisionDiffer
{
    /// <summary>Compares a decision, adding detail lines.</summary>
    /// <param name="before">The base decision.</param>
    /// <param name="after">The head decision.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    public static void CompareDecision(Record before, Record after, List<string> details)
    {
        var handled = new HashSet<string>(StringComparer.Ordinal) { RecordFields.Choice, RecordFields.Options };

        DiffValues.CompareValue("Choice", DiffValues.Field(before, RecordFields.Choice), DiffValues.Field(after, RecordFields.Choice), details);
        CompareDecisionOptions(before, after, details);
        DiffValues.CompareFields(before, after, details, handled);
    }

    /// <summary>Compares a decision's options, by name.</summary>
    /// <param name="before">The base decision.</param>
    /// <param name="after">The head decision.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    public static void CompareDecisionOptions(Record before, Record after, List<string> details)
    {
        var oldOptions = DiffEntries.OptionNames(before);
        var newOptions = DiffEntries.OptionNames(after);

        if (oldOptions is null || newOptions is null)
        {
            DiffValues.CompareValue("Options", DiffValues.Field(before, RecordFields.Options), DiffValues.Field(after, RecordFields.Options), details);
            return;
        }

        foreach (var name in oldOptions.Where(name => !newOptions.Contains(name, StringComparer.Ordinal))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            details.Add($"Option removed: \"{name}\".");
        }

        foreach (var name in newOptions.Where(name => !oldOptions.Contains(name, StringComparer.Ordinal))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            details.Add($"Option added: \"{name}\".");
        }
    }
}
