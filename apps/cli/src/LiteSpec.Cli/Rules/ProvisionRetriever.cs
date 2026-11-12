using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Rules;

/// <summary>
///     Retrieves the provisions that govern a target: artifact-scoped provisions match
///     the target artifact and the marked records, a provision naming the artifact
///     <c>any</c> and page-scope provisions match the target concern, and conventions
///     match their scope glob or the scope vocabulary against the target. A live departure whose subject or paths cover the
///     target replaces the provision its covers names. Only provisions whose record
///     layer the project claims are retrieved, and a code path in a project that claims
///     neither Implementation nor Proof retrieves none.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal static class ProvisionRetriever
{
    /// <summary>Retrieves the provisions that govern the target, in group then identifier order.</summary>
    /// <param name="catalog">The applied provisions.</param>
    /// <param name="conventions">The project conventions.</param>
    /// <param name="departures">The live departures.</param>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="target">The rules target.</param>
    /// <returns>The retrieved provisions with their reasons.</returns>
    public static IReadOnlyList<ProvisionMatch> Retrieve(
        ProvisionCatalog catalog,
        IReadOnlyList<ProvisionEntry> conventions,
        IReadOnlyList<DepartureFiles.Departure> departures,
        ConsumerConfig config,
        PathContext target)
    {
        if (target.IsCodePath && config.Layers.Implementation is null && config.Layers.Proof is null)
        {
            return [];
        }

        var candidates = target.GlobCandidates(config);
        var found = new Dictionary<string, (ProvisionEntry Entry, List<string> Reasons)>(StringComparer.Ordinal);

        foreach (var entry in catalog.Entries)
        {
            if (!entry.Covered(config.Layers))
            {
                continue;
            }

            var reasons = ReasonsFor(entry, target);

            if (reasons.Count == 0)
            {
                continue;
            }

            if (entry.HasWhen && !ApplyWhen(entry, target, config, reasons))
            {
                continue;
            }

            found[entry.Id] = (entry, reasons);
        }

        foreach (var convention in conventions)
        {
            if (convention.AppliesTo.Length == 0)
            {
                continue;
            }

            if (ConventionFiles.IsScopeForm(convention.AppliesTo))
            {
                if (ConventionFiles.MatchesScope(convention.AppliesTo, target.Stack, target.Concern, target.Artifact, target.Marked))
                {
                    found[convention.Id] = (convention, [$"scope match ({convention.AppliesTo})"]);
                }
            }
            else if (candidates.Any(candidate => Glob.Matches(convention.AppliesTo, candidate)))
            {
                found[convention.Id] = (convention, [$"path match ({convention.AppliesTo})"]);
            }
        }

        var results = new List<ProvisionMatch>();

        foreach (var (entry, reasons) in found.Values)
        {
            var departure = departures.FirstOrDefault(candidate =>
                DepartureFiles.Names(candidate, entry) && DepartureFiles.Covers(candidate, target, candidates));

            results.Add(new ProvisionMatch(entry, reasons, departure));
        }

        var groups = ProvisionEntry.GroupOrder.ToList();
        results.Sort((left, right) =>
        {
            var rank = groups.IndexOf(left.Entry.Group).CompareTo(groups.IndexOf(right.Entry.Group));
            return rank != 0 ? rank : string.CompareOrdinal(left.Entry.Id, right.Entry.Id);
        });

        return results;
    }

    /// <summary>Whether the conditional provision is returned for the target, showing its condition on a record.</summary>
    /// <param name="entry">The conditional provision.</param>
    /// <param name="target">The rules target.</param>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="reasons">The reasons, with the condition added for a record target.</param>
    /// <returns>True when a record target names it, a code target marks it directly, or its condition holds for a marked record. A project condition holds for no record, and an audit-adoption condition holds for the named decision only.</returns>
    static bool ApplyWhen(ProvisionEntry entry, PathContext target, ConsumerConfig config, List<string> reasons)
    {
        if (!target.IsCodePath)
        {
            reasons.Add($"when {entry.When}");
            return true;
        }

        if (target.Marked.Any(marked => string.Equals(marked, entry.Id, StringComparison.Ordinal)))
        {
            return true;
        }

        var records = ProvisionWhen.LoadMarked(config, target);

        return records.Count > 0 && ProvisionWhen.Holds(entry.When, records, config);
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

        var artifacts = Scopes.ArtifactValues(entry.AppliesTo);

        if (artifacts.Count > 0 && artifacts.All(value => Scopes.IsConcernWide(value)))
        {
            if (entry.Concern.Length > 0 && entry.Concern == target.Concern)
            {
                reasons.Add($"concern {target.Concern}");
            }
        }
        else if (artifacts.Count > 0)
        {
            if (target.Artifact.Length > 0 && artifacts.Contains(target.Artifact, StringComparer.Ordinal))
            {
                reasons.Add(target.IsRecord ? $"record {target.Display}" : $"path match (artifact {target.Artifact})");
            }

            foreach (var marked in target.Marked)
            {
                if (PathContext.ArtifactOf(marked) is { Length: > 0 } artifact
                    && artifacts.Contains(artifact, StringComparer.Ordinal)
                    && !reasons.Any(reason => reason == $"record {marked}"))
                {
                    reasons.Add($"record {marked}");
                }
            }
        }
        else if (entry.Concern.Length > 0 && entry.Concern == target.Concern)
        {
            reasons.Add($"concern {target.Concern}");
        }

        return reasons;
    }
}
