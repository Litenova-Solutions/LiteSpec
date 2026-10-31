namespace LiteSpec.Model.Diff;

/// <summary>
///     Compares two use case records: access and effects, and mirrors the refusals
///     and preconditions a change adds to or removes from the use case.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
/// <follows>litespec/core/release#review-behavior-changes-through-the-behavior-diff</follows>
static class UseCaseDiffer
{
    /// <summary>Compares a use case, adding detail lines.</summary>
    /// <param name="before">The base use case.</param>
    /// <param name="after">The head use case.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    public static void CompareUseCase(Record before, Record after, List<string> details)
    {
        var handled = new HashSet<string>(StringComparer.Ordinal) { RecordFields.Access, RecordFields.Effects };

        CompareAccess(before, after, details);
        CompareEffects(before, after, details);
        DiffValues.CompareFields(before, after, details, handled);
    }

    /// <summary>Compares a use case's access grant: tenant scope, grant, and auth kinds.</summary>
    /// <param name="before">The base use case.</param>
    /// <param name="after">The head use case.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    public static void CompareAccess(Record before, Record after, List<string> details)
    {
        var oldAccess = DiffEntries.AccessOf(before);
        var newAccess = DiffEntries.AccessOf(after);

        if (oldAccess is null || newAccess is null)
        {
            DiffValues.CompareValue("Access", DiffValues.Field(before, RecordFields.Access), DiffValues.Field(after, RecordFields.Access), details);
            return;
        }

        if (!string.Equals(oldAccess.Tenant, newAccess.Tenant, StringComparison.Ordinal))
        {
            details.Add($"Tenant scope changed from {oldAccess.Tenant ?? "none"} to {newAccess.Tenant ?? "none"}.");
        }

        if (!string.Equals(oldAccess.Grant, newAccess.Grant, StringComparison.Ordinal))
        {
            details.Add($"Grant changed: \"{newAccess.Grant ?? "none"}\" (was: \"{oldAccess.Grant ?? "none"}\").");
        }

        var oldAuth = oldAccess.Auth;
        var newAuth = newAccess.Auth;

        if (!oldAuth.SetEquals(newAuth))
        {
            var parts = newAuth.Except(oldAuth, StringComparer.Ordinal).OrderBy(kind => kind, StringComparer.Ordinal)
                    .Select(kind => $"+{kind}")
                .Concat(oldAuth.Except(newAuth, StringComparer.Ordinal).OrderBy(kind => kind, StringComparer.Ordinal)
                    .Select(kind => $"-{kind}"))
                .ToList();

            details.Add($"Auth changed: {string.Join(", ", parts)}.");
        }
    }

    /// <summary>Compares a use case's effects, keyed by verb and target.</summary>
    /// <param name="before">The base use case.</param>
    /// <param name="after">The head use case.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    public static void CompareEffects(Record before, Record after, List<string> details)
    {
        var oldEffects = DiffEntries.EffectEntries(before);
        var newEffects = DiffEntries.EffectEntries(after);

        if (oldEffects is null || newEffects is null)
        {
            DiffValues.CompareValue("Effects", DiffValues.Field(before, RecordFields.Effects), DiffValues.Field(after, RecordFields.Effects), details);
            return;
        }

        foreach (var key in oldEffects.Keys.Where(key => !newEffects.ContainsKey(key))
                     .OrderBy(key => key, StringComparer.Ordinal))
        {
            details.Add($"Effect removed: {oldEffects[key].Describe()}.");
        }

        foreach (var key in newEffects.Keys.Where(key => !oldEffects.ContainsKey(key))
                     .OrderBy(key => key, StringComparer.Ordinal))
        {
            details.Add($"Effect added: {newEffects[key].Describe()}.");
        }

        foreach (var key in oldEffects.Keys.Where(newEffects.ContainsKey)
                     .OrderBy(key => key, StringComparer.Ordinal))
        {
            var oldEffect = oldEffects[key];
            var newEffect = newEffects[key];

            if (!string.Equals(oldEffect.Timing, newEffect.Timing, StringComparison.Ordinal))
            {
                details.Add($"Effect {oldEffect.Edge()} timing changed from {oldEffect.Timing ?? "none"} to {newEffect.Timing ?? "none"}.");
            }
        }
    }

    /// <summary>
    ///     Lists the refusals and preconditions a change adds to or removes from a use
    ///     case, mirroring the breaking reasons of their own entries.
    /// </summary>
    /// <param name="baseModel">The base model.</param>
    /// <param name="headModel">The head model.</param>
    /// <param name="useCase">The use case record.</param>
    /// <param name="addedIds">The ids the head adds.</param>
    /// <param name="removedIds">The ids the head removes.</param>
    /// <param name="byId">The change entries by record id.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    /// <param name="reasons">The breaking reasons a change adds to.</param>
    public static void EnrichUseCase(
        ProjectModel baseModel,
        ProjectModel headModel,
        Record useCase,
        HashSet<string> addedIds,
        HashSet<string> removedIds,
        Dictionary<string, IntentChange> byId,
        List<string> details,
        List<BreakingReason> reasons)
    {
        if (!Identifiers.TrySplit(useCase.Id, out _, out _, out var name))
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Mirror(string id)
        {
            if (byId.TryGetValue(id, out var entry))
            {
                foreach (var reason in entry.Reasons)
                {
                    if (seen.Add(reason.Code))
                    {
                        reasons.Add(reason);
                    }
                }
            }
        }

        foreach (var result in AddedResults(headModel, addedIds, name)
                     .OrderBy(record => record.Title, StringComparer.Ordinal))
        {
            details.Add(result.Field(RecordFields.Outcome) == "refusal"
                ? $"Added refusal: \"{result.Title}\" ({result.Field(RecordFields.Code) ?? "no code"})."
                : $"Added result: \"{result.Title}\".");
            Mirror(result.Id);
        }

        foreach (var result in RemovedResults(baseModel, removedIds, name)
                     .OrderBy(record => record.Title, StringComparer.Ordinal))
        {
            details.Add(result.Field(RecordFields.Outcome) == "refusal"
                ? $"Removed refusal: \"{result.Title}\" ({result.Field(RecordFields.Code) ?? "no code"})."
                : $"Removed result: \"{result.Title}\".");
            Mirror(result.Id);
        }

        foreach (var rule in AddedRules(headModel, addedIds, useCase.Id)
                     .OrderBy(record => record.Title, StringComparer.Ordinal))
        {
            details.Add($"Added precondition: \"{rule.Title}\".");
            Mirror(rule.Id);
        }

        foreach (var rule in RemovedRules(baseModel, removedIds, useCase.Id)
                     .OrderBy(record => record.Title, StringComparer.Ordinal))
        {
            details.Add($"Removed precondition: \"{rule.Title}\".");
            Mirror(rule.Id);
        }
    }

    /// <summary>The results a change adds under one use case home.</summary>
    /// <param name="headModel">The head model.</param>
    /// <param name="addedIds">The ids the head adds.</param>
    /// <param name="home">The use case home name.</param>
    /// <returns>The results a change adds under the use case home.</returns>
    public static IEnumerable<Record> AddedResults(ProjectModel headModel, HashSet<string> addedIds, string home) =>
        headModel.Records.Where(record =>
            record.Type == RecordTypes.Result
            && addedIds.Contains(record.Id)
            && Identifiers.TrySplit(record.Id, out _, out var resultHome, out _)
            && resultHome == home);

    /// <summary>The results a change removes under one use case home.</summary>
    /// <param name="baseModel">The base model.</param>
    /// <param name="removedIds">The ids the head removes.</param>
    /// <param name="home">The use case home name.</param>
    /// <returns>The results a change removes under the use case home.</returns>
    public static IEnumerable<Record> RemovedResults(ProjectModel baseModel, HashSet<string> removedIds, string home) =>
        baseModel.Records.Where(record =>
            record.Type == RecordTypes.Result
            && removedIds.Contains(record.Id)
            && Identifiers.TrySplit(record.Id, out _, out var resultHome, out _)
            && resultHome == home);

    /// <summary>The preconditions a change adds to one use case.</summary>
    /// <param name="headModel">The head model.</param>
    /// <param name="addedIds">The ids the head adds.</param>
    /// <param name="useCaseId">The use case identifier.</param>
    /// <returns>The preconditions a change adds to the use case.</returns>
    public static IEnumerable<Record> AddedRules(ProjectModel headModel, HashSet<string> addedIds, string useCaseId) =>
        headModel.Records.Where(record =>
            record.Type == RecordTypes.Rule
            && record.Field(RecordFields.Kind) == "precondition"
            && addedIds.Contains(record.Id)
            && record.Ids(RecordFields.Governs).Contains(useCaseId, StringComparer.Ordinal));

    /// <summary>The preconditions a change removes from one use case.</summary>
    /// <param name="baseModel">The base model.</param>
    /// <param name="removedIds">The ids the head removes.</param>
    /// <param name="useCaseId">The use case identifier.</param>
    /// <returns>The preconditions a change removes from the use case.</returns>
    public static IEnumerable<Record> RemovedRules(ProjectModel baseModel, HashSet<string> removedIds, string useCaseId) =>
        baseModel.Records.Where(record =>
            record.Type == RecordTypes.Rule
            && record.Field(RecordFields.Kind) == "precondition"
            && removedIds.Contains(record.Id)
            && record.Ids(RecordFields.Governs).Contains(useCaseId, StringComparer.Ordinal));
}
