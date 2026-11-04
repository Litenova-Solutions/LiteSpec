using LiteSpec.Model;

namespace LiteSpec.Rules;

/// <summary>
///     Retrieves the provisions that govern a target: artifact-scoped provisions match
///     the target artifact and the marked records, page-scope provisions match the
///     target layer, and conventions match their scope glob. A live departure whose
///     subject or paths cover the target replaces the provision its covers names.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal static class RuleRetriever
{
    /// <summary>Retrieves the provisions that govern the target, in group then identifier order.</summary>
    /// <param name="catalog">The applied provisions.</param>
    /// <param name="conventions">The project conventions.</param>
    /// <param name="departures">The live departures.</param>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="target">The rules target.</param>
    /// <returns>The retrieved provisions with their reasons.</returns>
    public static IReadOnlyList<RuleResult> Retrieve(
        ProvisionCatalog catalog,
        IReadOnlyList<ProvisionEntry> conventions,
        IReadOnlyList<DepartureStore.Departure> departures,
        ConsumerConfig config,
        PathContext target)
    {
        var candidates = target.GlobCandidates(config);
        var found = new Dictionary<string, (ProvisionEntry Entry, List<string> Reasons)>(StringComparer.Ordinal);

        foreach (var entry in catalog.Entries)
        {
            var reasons = ReasonsFor(entry, target);

            if (reasons.Count > 0)
            {
                found[entry.Id] = (entry, reasons);
            }
        }

        foreach (var convention in conventions)
        {
            if (convention.AppliesTo.Length > 0 && candidates.Any(candidate => Glob.Matches(convention.AppliesTo, candidate)))
            {
                found[convention.Id] = (convention, [$"path match ({convention.AppliesTo})"]);
            }
        }

        var results = new List<RuleResult>();

        foreach (var (entry, reasons) in found.Values)
        {
            var departure = departures.FirstOrDefault(candidate =>
                candidate.Covers == entry.Id && DepartureStore.Covers(candidate, target, candidates));

            results.Add(new RuleResult(entry, reasons, departure));
        }

        var groups = ProvisionEntry.GroupOrder.ToList();
        results.Sort((left, right) =>
        {
            var rank = groups.IndexOf(left.Entry.Group).CompareTo(groups.IndexOf(right.Entry.Group));
            return rank != 0 ? rank : string.CompareOrdinal(left.Entry.Id, right.Entry.Id);
        });

        return results;
    }

    /// <summary>Why the provision governs the target, or empty when it does not.</summary>
    /// <param name="entry">The provision.</param>
    /// <param name="target">The rules target.</param>
    /// <returns>The reasons in match order.</returns>
    static List<string> ReasonsFor(ProvisionEntry entry, PathContext target)
    {
        var reasons = new List<string>();

        if (target.Marked.Any(marked => string.Equals(marked, entry.Id, StringComparison.Ordinal)))
        {
            reasons.Add($"record {entry.Id}");
        }

        if (entry.AppliesTo.Length > 0)
        {
            if (target.Artifact.Length > 0 && entry.AppliesTo == target.Artifact)
            {
                reasons.Add(target.IsRecord ? $"record {target.Display}" : $"path match (artifact {target.Artifact})");
            }

            foreach (var marked in target.Marked)
            {
                if (PathContext.ArtifactOf(marked) is { Length: > 0 } artifact
                    && artifact == entry.AppliesTo
                    && !reasons.Any(reason => reason == $"record {marked}"))
                {
                    reasons.Add($"record {marked}");
                }
            }
        }
        else if (entry.Layer.Length > 0 && entry.Layer == target.Layer)
        {
            reasons.Add($"layer {target.Layer}");
        }

        return reasons;
    }
}
