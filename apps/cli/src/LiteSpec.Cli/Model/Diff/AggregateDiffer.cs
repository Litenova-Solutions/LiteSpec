using System.Text.Json;

namespace LiteSpec.Model.Diff;

/// <summary>
///     Compares two aggregate records: states, transitions, data, data classes,
///     saga steps, exits, and what drives it.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
/// <follows>litespec/core/release#review-behavior-changes-through-the-behavior-diff</follows>
static class AggregateDiffer
{
    /// <summary>Compares an aggregate, adding detail lines and breaking reasons.</summary>
    /// <param name="before">The base aggregate.</param>
    /// <param name="after">The head aggregate.</param>
    /// <param name="baseModel">The base model.</param>
    /// <param name="headModel">The head model.</param>
    /// <param name="approved">Whether the base aggregate stands approved.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    /// <param name="reasons">The breaking reasons a change adds to.</param>
    public static void CompareAggregate(
        Record before,
        Record after,
        ProjectModel baseModel,
        ProjectModel headModel,
        bool approved,
        List<string> details,
        List<BreakingReason> reasons)
    {
        var handled = new HashSet<string>(StringComparer.Ordinal) { RecordFields.States, RecordFields.Transitions, RecordFields.Data, RecordFields.DataClasses, RecordFields.Steps, RecordFields.Exits, RecordFields.DrivenBy };

        CompareStates(before, after, approved, details, reasons);
        CompareTransitions(before, after, baseModel, headModel, approved, details, reasons);
        CompareData(before, after, details);
        CompareDataClasses(before, after, details);
        CompareSagaSteps(before, after, details);
        CompareExits(before, after, details);
        CompareDrivenBy(before, after, details);
        DiffValues.CompareFields(before, after, details, handled);
    }

    /// <summary>The sentence naming the states an aggregate held.</summary>
    /// <param name="record">The aggregate record.</param>
    /// <returns>The sentence naming the states the aggregate held.</returns>
    public static string StatesLine(Record record)
    {
        var states = DiffEntries.StateNames(record);

        return states.Count == 0 ? "It held no states." : $"It held states: {string.Join(", ", states)}.";
    }

    /// <summary>Compares an aggregate's states, keyed by name.</summary>
    /// <param name="before">The base aggregate.</param>
    /// <param name="after">The head aggregate.</param>
    /// <param name="approved">Whether the base aggregate stands approved.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    /// <param name="reasons">The breaking reasons a change adds to.</param>
    public static void CompareStates(
        Record before, Record after, bool approved, List<string> details, List<BreakingReason> reasons)
    {
        var oldStates = DiffEntries.StateEntries(before);
        var newStates = DiffEntries.StateEntries(after);

        if (oldStates is null || newStates is null)
        {
            if (!DiffValues.JsonEquals(DiffValues.Field(before, RecordFields.States), DiffValues.Field(after, RecordFields.States)))
            {
                details.Add("States changed.");
            }

            return;
        }

        foreach (var name in oldStates.Keys.Where(name => !newStates.ContainsKey(name))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            details.Add($"State removed: {name}.");
        }

        foreach (var name in newStates.Keys.Where(name => !oldStates.ContainsKey(name))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            details.Add($"State added: {name}.");
        }

        foreach (var name in oldStates.Keys.Where(newStates.ContainsKey)
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            if (!string.Equals(oldStates[name], newStates[name], StringComparison.Ordinal))
            {
                details.Add($"State {name} meaning changed: \"{newStates[name]}\" (was: \"{oldStates[name]}\").");
            }
        }

        if (approved && oldStates.Keys.Any(name => !newStates.ContainsKey(name)))
        {
            reasons.Add(Differ.StateRemoved);
        }
    }

    /// <summary>Compares an aggregate's transitions, keyed by from, to, and use case.</summary>
    /// <param name="before">The base aggregate.</param>
    /// <param name="after">The head aggregate.</param>
    /// <param name="baseModel">The base model.</param>
    /// <param name="headModel">The head model.</param>
    /// <param name="approved">Whether the base aggregate stands approved.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    /// <param name="reasons">The breaking reasons a change adds to.</param>
    public static void CompareTransitions(
        Record before,
        Record after,
        ProjectModel baseModel,
        ProjectModel headModel,
        bool approved,
        List<string> details,
        List<BreakingReason> reasons)
    {
        var oldTransitions = DiffEntries.TransitionEntries(before, baseModel);
        var newTransitions = DiffEntries.TransitionEntries(after, headModel);

        if (oldTransitions is null || newTransitions is null)
        {
            if (!DiffValues.JsonEquals(DiffValues.Field(before, RecordFields.Transitions), DiffValues.Field(after, RecordFields.Transitions)))
            {
                details.Add("Transitions changed.");
            }

            return;
        }

        var removed = oldTransitions.Keys.Except(newTransitions.Keys)
            .OrderBy(key => key, StringComparer.Ordinal).ToList();
        var added = newTransitions.Keys.Except(oldTransitions.Keys)
            .OrderBy(key => key, StringComparer.Ordinal).ToList();

        foreach (var key in removed)
        {
            details.Add($"Transition removed: {oldTransitions[key]}.");
        }

        foreach (var key in added)
        {
            details.Add($"Transition added: {newTransitions[key]}.");
        }

        if (approved && removed.Count > 0)
        {
            reasons.Add(Differ.TransitionRemoved);
        }
    }

    /// <summary>Compares an aggregate's data, keyed by name.</summary>
    /// <param name="before">The base aggregate.</param>
    /// <param name="after">The head aggregate.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    public static void CompareData(Record before, Record after, List<string> details)
    {
        var oldData = DiffEntries.DataEntries(before);
        var newData = DiffEntries.DataEntries(after);

        if (oldData is null || newData is null)
        {
            if (!DiffValues.JsonEquals(DiffValues.Field(before, RecordFields.Data), DiffValues.Field(after, RecordFields.Data)))
            {
                details.Add("Data changed.");
            }

            return;
        }

        foreach (var name in oldData.Keys.Where(name => !newData.ContainsKey(name))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            details.Add($"Data removed: {name}.");
        }

        foreach (var name in newData.Keys.Where(name => !oldData.ContainsKey(name))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            details.Add($"Data added: {name}.");
        }

        foreach (var name in oldData.Keys.Where(newData.ContainsKey)
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            var (oldMeaning, oldClassification) = oldData[name];
            var (newMeaning, newClassification) = newData[name];

            if (!string.Equals(oldClassification, newClassification, StringComparison.Ordinal))
            {
                details.Add($"Data {name} classification changed from {oldClassification} to {newClassification}.");
            }

            if (!string.Equals(oldMeaning, newMeaning, StringComparison.Ordinal))
            {
                details.Add($"Data {name} meaning changed: \"{newMeaning}\" (was: \"{oldMeaning}\").");
            }
        }
    }

    /// <summary>Compares data classes, keyed by name.</summary>
    /// <param name="before">The base record.</param>
    /// <param name="after">The head record.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    public static void CompareDataClasses(Record before, Record after, List<string> details)
    {
        var oldClasses = DiffEntries.DataClassEntries(before);
        var newClasses = DiffEntries.DataClassEntries(after);

        if (oldClasses is null || newClasses is null)
        {
            DiffValues.CompareValue("Data classes", DiffValues.Field(before, RecordFields.DataClasses), DiffValues.Field(after, RecordFields.DataClasses), details);
            return;
        }

        foreach (var name in oldClasses.Keys.Where(name => !newClasses.ContainsKey(name))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            details.Add($"Data class removed: {name}.");
        }

        foreach (var name in newClasses.Keys.Where(name => !oldClasses.ContainsKey(name))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            details.Add($"Data class added: {name} ({newClasses[name].Classification ?? "no classification"}).");
        }

        foreach (var name in oldClasses.Keys.Where(newClasses.ContainsKey)
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            var oldClass = oldClasses[name];
            var newClass = newClasses[name];

            if (!string.Equals(oldClass.Classification, newClass.Classification, StringComparison.Ordinal))
            {
                details.Add($"Data class {name} classification changed from {oldClass.Classification ?? "none"} to {newClass.Classification ?? "none"}.");
            }

            if (!string.Equals(oldClass.Purpose, newClass.Purpose, StringComparison.Ordinal))
            {
                details.Add($"Data class {name} purpose changed: \"{newClass.Purpose ?? "none"}\" (was: \"{oldClass.Purpose ?? "none"}\").");
            }

            if (!string.Equals(oldClass.OnErasure, newClass.OnErasure, StringComparison.Ordinal))
            {
                details.Add($"Data class {name} erasure changed from {oldClass.OnErasure ?? "none"} to {newClass.OnErasure ?? "none"}.");
            }
        }
    }

    /// <summary>Compares an aggregate's saga steps, keyed by step id.</summary>
    /// <param name="before">The base aggregate.</param>
    /// <param name="after">The head aggregate.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    public static void CompareSagaSteps(Record before, Record after, List<string> details)
    {
        var oldSteps = DiffEntries.SagaStepEntries(before);
        var newSteps = DiffEntries.SagaStepEntries(after);

        if (oldSteps is null || newSteps is null)
        {
            if (!DiffValues.JsonEquals(DiffValues.Field(before, RecordFields.Steps), DiffValues.Field(after, RecordFields.Steps)))
            {
                details.Add("Steps changed.");
            }

            return;
        }

        foreach (var id in oldSteps.Keys.Where(id => !newSteps.ContainsKey(id))
                     .OrderBy(id => id, StringComparer.Ordinal))
        {
            details.Add($"Step removed: {id}.");
        }

        foreach (var id in newSteps.Keys.Where(id => !oldSteps.ContainsKey(id))
                     .OrderBy(id => id, StringComparer.Ordinal))
        {
            var runs = DiffEntries.StringOf(newSteps[id], "runs");

            details.Add(runs is null ? $"Step added: {id}." : $"Step added: {id} running {runs}.");
        }

        foreach (var id in oldSteps.Keys.Where(newSteps.ContainsKey)
                     .OrderBy(id => id, StringComparer.Ordinal))
        {
            var oldStep = oldSteps[id];
            var newStep = newSteps[id];
            var oldRuns = DiffEntries.StringOf(oldStep, "runs");
            var newRuns = DiffEntries.StringOf(newStep, "runs");

            if (!string.Equals(oldRuns, newRuns, StringComparison.Ordinal))
            {
                details.Add($"Step {id} now runs {newRuns ?? "nothing"} (was: {oldRuns ?? "nothing"}).");
            }

            var oldGuard = DiffEntries.StringOf(oldStep, "guard");
            var newGuard = DiffEntries.StringOf(newStep, "guard");

            if (!string.Equals(oldGuard, newGuard, StringComparison.Ordinal))
            {
                details.Add(oldGuard is null
                    ? $"Step {id} gained a guard: \"{newGuard}\"."
                    : newGuard is null
                        ? $"Step {id} lost its guard."
                        : $"Step {id} guard changed: \"{newGuard}\" (was: \"{oldGuard}\").");
            }

            var oldHas = oldStep.TryGetProperty("compensation", out var oldCompensation);
            var newHas = newStep.TryGetProperty("compensation", out var newCompensation);

            if (!DiffValues.JsonEquals(
                    oldHas ? oldCompensation : (JsonElement?)null,
                    newHas ? newCompensation : (JsonElement?)null))
            {
                details.Add($"Step {id} compensation changed.");
            }
        }
    }

    /// <summary>Compares an aggregate's exits, by name.</summary>
    /// <param name="before">The base aggregate.</param>
    /// <param name="after">The head aggregate.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    public static void CompareExits(Record before, Record after, List<string> details)
    {
        var oldExits = DiffEntries.ExitNames(before);
        var newExits = DiffEntries.ExitNames(after);

        if (oldExits is null || newExits is null)
        {
            if (!DiffValues.JsonEquals(DiffValues.Field(before, RecordFields.Exits), DiffValues.Field(after, RecordFields.Exits)))
            {
                details.Add("Exits changed.");
            }

            return;
        }

        foreach (var name in oldExits.Where(name => !newExits.Contains(name, StringComparer.Ordinal))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            details.Add($"Exit removed: {name}.");
        }

        foreach (var name in newExits.Where(name => !oldExits.Contains(name, StringComparer.Ordinal))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            details.Add($"Exit added: {name}.");
        }
    }

    /// <summary>Compares what drives an aggregate: its job, reaction, or person.</summary>
    /// <param name="before">The base aggregate.</param>
    /// <param name="after">The head aggregate.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    public static void CompareDrivenBy(Record before, Record after, List<string> details)
    {
        var oldDriver = DiffValues.Field(before, RecordFields.DrivenBy);
        var newDriver = DiffValues.Field(after, RecordFields.DrivenBy);

        if (oldDriver is null || newDriver is null)
        {
            DiffValues.CompareValue("Driven by", oldDriver, newDriver, details);
            return;
        }

        if (DiffValues.JsonEquals(oldDriver, newDriver))
        {
            return;
        }

        var oldNames = DiffEntries.DriverNames(oldDriver.Value);
        var newNames = DiffEntries.DriverNames(newDriver.Value);

        details.Add(oldNames.Count == 1 && newNames.Count == 1
            ? $"Driven by changed from {oldNames[0]} to {newNames[0]}."
            : "Driven by changed.");
    }
}
