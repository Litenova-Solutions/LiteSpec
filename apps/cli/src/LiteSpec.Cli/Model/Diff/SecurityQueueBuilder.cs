namespace LiteSpec.Model.Diff;

/// <summary>
///     The security queue of a diff: use cases widened to tenant scope cross, new
///     calls effects, and new personal or sensitive data classes, in record order.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
/// <follows>litespec/core/release#review-behavior-changes-through-the-behavior-diff</follows>
static class SecurityQueueBuilder
{
    /// <summary>Builds the security queue of a diff, in record order.</summary>
    /// <param name="baseModel">The base model.</param>
    /// <param name="headModel">The head model.</param>
    /// <param name="addedIds">The ids the head adds.</param>
    /// <param name="removedIds">The ids the head removes.</param>
    /// <returns>The security queue of the diff, in record order.</returns>
    public static List<SecurityQueueEntry> Build(
        ProjectModel baseModel, ProjectModel headModel, HashSet<string> addedIds, HashSet<string> removedIds)
    {
        var queue = new List<SecurityQueueEntry>();

        foreach (var record in headModel.Records
                     .OrderBy(left => left.Type, StringComparer.Ordinal)
                     .ThenBy(left => left.Id, StringComparer.Ordinal))
        {
            if (removedIds.Contains(record.Id))
            {
                continue;
            }

            var added = addedIds.Contains(record.Id);
            baseModel.ById.TryGetValue(record.Id, out var before);

            if (record.Type == RecordTypes.UseCase)
            {
                QueueTenantWidening(before, record, added, queue);
                QueueNewCalls(before, record, added, queue);
            }

            if (record.Type == RecordTypes.Aggregate)
            {
                QueueNewDataClasses(before, record, added, queue);
            }
        }

        return queue;
    }

    /// <summary>Queues a use case whose tenant scope is cross, newly or by widening.</summary>
    /// <param name="before">The base use case, or null when the head adds it.</param>
    /// <param name="after">The head use case.</param>
    /// <param name="added">Whether the head adds the use case.</param>
    /// <param name="queue">The queue an entry adds to.</param>
    public static void QueueTenantWidening(
        Record? before, Record after, bool added, List<SecurityQueueEntry> queue)
    {
        var tenant = DiffEntries.AccessOf(after)?.Tenant;

        if (!string.Equals(tenant, "cross", StringComparison.Ordinal))
        {
            return;
        }

        if (!added && before is not null
            && string.Equals(DiffEntries.AccessOf(before)?.Tenant, "cross", StringComparison.Ordinal))
        {
            return;
        }

        queue.Add(new SecurityQueueEntry(
            after.Id,
            after.Title,
            after.Type,
            added || before is null
                ? "Access tenant scope is cross."
                : $"Access tenant scope widened from {DiffEntries.AccessOf(before)?.Tenant ?? "none"} to cross."));
    }

    /// <summary>Queues a use case's new calls effects.</summary>
    /// <param name="before">The base use case, or null when the head adds it.</param>
    /// <param name="after">The head use case.</param>
    /// <param name="added">Whether the head adds the use case.</param>
    /// <param name="queue">The queue an entry adds to.</param>
    public static void QueueNewCalls(Record? before, Record after, bool added, List<SecurityQueueEntry> queue)
    {
        var newEffects = DiffEntries.EffectEntries(after);

        if (newEffects is null)
        {
            return;
        }

        var oldEffects = !added && before is not null ? DiffEntries.EffectEntries(before) : new Dictionary<string, EffectEntry>();

        if (oldEffects is null)
        {
            return;
        }

        foreach (var key in newEffects.Keys
                     .Where(key => !oldEffects.ContainsKey(key) && newEffects[key].Verb == "calls")
                     .OrderBy(key => key, StringComparer.Ordinal))
        {
            queue.Add(new SecurityQueueEntry(
                after.Id, after.Title, after.Type, $"New calls effect: {newEffects[key].Describe()}."));
        }
    }

    /// <summary>Queues an aggregate's new personal or sensitive data classes.</summary>
    /// <param name="before">The base aggregate, or null when the head adds it.</param>
    /// <param name="after">The head aggregate.</param>
    /// <param name="added">Whether the head adds the aggregate.</param>
    /// <param name="queue">The queue an entry adds to.</param>
    public static void QueueNewDataClasses(Record? before, Record after, bool added, List<SecurityQueueEntry> queue)
    {
        var newClasses = DiffEntries.DataClassEntries(after);

        if (newClasses is null)
        {
            return;
        }

        var oldClasses = !added && before is not null ? DiffEntries.DataClassEntries(before) : new Dictionary<string, DataClassValue>();

        if (oldClasses is null)
        {
            return;
        }

        foreach (var name in newClasses.Keys.OrderBy(left => left, StringComparer.Ordinal))
        {
            var classification = newClasses[name].Classification;

            if (classification is not ("personal" or "sensitive"))
            {
                continue;
            }

            if (!oldClasses.TryGetValue(name, out var oldClass))
            {
                queue.Add(new SecurityQueueEntry(
                    after.Id, after.Title, after.Type, $"New {classification} data class: {name}."));
            }
            else if (oldClass.Classification is not ("personal" or "sensitive"))
            {
                queue.Add(new SecurityQueueEntry(
                    after.Id,
                    after.Title,
                    after.Type,
                    $"Data class {name} is now {classification} (was: {oldClass.Classification ?? "none"})."));
            }
        }
    }
}
