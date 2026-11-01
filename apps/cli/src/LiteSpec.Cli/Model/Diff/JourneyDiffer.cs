namespace LiteSpec.Model.Diff;

/// <summary>Compares two journey records: the finish line and the steps.</summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
/// <follows>litespec/core/release#review-behavior-changes-through-the-behavior-diff</follows>
static class JourneyDiffer
{
    /// <summary>Compares a journey, adding detail lines.</summary>
    /// <param name="before">The base journey.</param>
    /// <param name="after">The head journey.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    public static void CompareJourney(Record before, Record after, List<string> details)
    {
        var handled = new HashSet<string>(StringComparer.Ordinal) { RecordFields.FinishedWhen, RecordFields.Steps };

        DiffValues.CompareValue("Finished when", DiffValues.Field(before, RecordFields.FinishedWhen), DiffValues.Field(after, RecordFields.FinishedWhen), details);
        CompareJourneySteps(before, after, details);
        DiffValues.CompareFields(before, after, details, handled);
    }

    /// <summary>Compares a journey's steps, keyed by step id.</summary>
    /// <param name="before">The base journey.</param>
    /// <param name="after">The head journey.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    public static void CompareJourneySteps(Record before, Record after, List<string> details)
    {
        var oldSteps = DiffEntries.JourneySteps(before);
        var newSteps = DiffEntries.JourneySteps(after);

        if (oldSteps is null || newSteps is null)
        {
            DiffValues.CompareValue("Steps", DiffValues.Field(before, RecordFields.Steps), DiffValues.Field(after, RecordFields.Steps), details);
            return;
        }

        foreach (var id in oldSteps.Keys.Where(id => !newSteps.ContainsKey(id))
                     .OrderBy(id => id, StringComparer.Ordinal))
        {
            details.Add($"Step {id} removed: \"{oldSteps[id].Statement}\".");
        }

        foreach (var id in newSteps.Keys.Where(id => !oldSteps.ContainsKey(id))
                     .OrderBy(id => id, StringComparer.Ordinal))
        {
            details.Add($"Step {id} added: \"{newSteps[id].Statement}\".");
        }

        foreach (var id in oldSteps.Keys.Where(newSteps.ContainsKey)
                     .OrderBy(id => id, StringComparer.Ordinal))
        {
            var oldStep = oldSteps[id];
            var newStep = newSteps[id];

            if (!string.Equals(oldStep.Statement, newStep.Statement, StringComparison.Ordinal))
            {
                details.Add($"Step {id} changed: \"{newStep.Statement}\" (was: \"{oldStep.Statement}\").");
            }

            if (!string.Equals(oldStep.Actor, newStep.Actor, StringComparison.Ordinal))
            {
                details.Add($"Step {id} actor changed from {oldStep.Actor ?? "none"} to {newStep.Actor ?? "none"}.");
            }

            if (!string.Equals(oldStep.Failure, newStep.Failure, StringComparison.Ordinal))
            {
                details.Add(oldStep.Failure is null
                    ? $"Step {id} gained a failure: \"{newStep.Failure}\"."
                    : newStep.Failure is null
                        ? $"Step {id} lost its failure."
                        : $"Step {id} failure changed: \"{newStep.Failure}\" (was: \"{oldStep.Failure}\").");
            }
        }
    }

    /// <summary>One product sentence per journey step, in step order.</summary>
    /// <param name="record">The journey record.</param>
    /// <returns>One line per step, holding the step id and its quoted statement, ordered by step id.</returns>
    public static List<string> StepsLines(Record record)
    {
        var steps = DiffEntries.JourneySteps(record);

        if (steps is null)
        {
            return [];
        }

        return steps
            .OrderBy(step => step.Key, StringComparer.Ordinal)
            .Select(step => $"Step {step.Key}: \"{step.Value.Statement}\".")
            .ToList();
    }
}
