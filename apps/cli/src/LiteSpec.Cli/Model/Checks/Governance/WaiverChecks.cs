using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.ProseText;

namespace LiteSpec.Cli.Model.Checks.Governance;

/// <summary>
///     The waiver checks: every waives item names a finding code the tools emit, a shared
///     provision or a shared page, and never a code the model needs in order to load; a waiver
///     with an expiry date stops holding once the date passes; two live waivers never cover
///     the same finding on the same subject or path.
/// </summary>
/// <follows>litespec/core/waivers#record-accepted-gaps-as-waivers</follows>
/// <follows>litespec/core/waivers#cover-each-finding-with-one-waiver</follows>
internal static class WaiverChecks
{
    /// <summary>Reports waiver-invalid for each waives item that cannot hold findings, waiver-expired for each waiver past its expiry date, and waiver-duplicate for each live waiver sharing a cover and a scope with an earlier live waiver.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var waivers = model.Records.Where(record => record.Type == RecordTypes.Waiver).ToList();

        if (waivers.Count == 0)
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var standards = Standards();

        foreach (var waiver in waivers)
        {
            foreach (var cover in waiver.Strings(RecordFields.Waives).Order(StringComparer.Ordinal))
            {
                if (Refusal(cover, standards) is { } reason)
                {
                    findings.Add(new Finding("waiver-invalid", $"Waiver '{waiver.Id}' names '{cover}', {reason}.", waiver.File, waiver.Line, SubjectOf(waiver)));
                }
            }

            if (waiver.Field(RecordFields.ExpiresOn) is { } expiresOn
                && IsoDate(expiresOn, out var expires)
                && today > expires)
            {
                findings.Add(new Finding("waiver-expired", $"Waiver '{waiver.Id}' stands past its expiresOn {expiresOn}.", waiver.File, waiver.Line, SubjectOf(waiver)));
            }
        }

        CheckOverlap(waivers, today, findings);
    }

    /// <summary>Reports waiver-duplicate for each live waiver sharing a waives item and an appliesTo record or path with an earlier live waiver.</summary>
    /// <param name="waivers">Every waiver record.</param>
    /// <param name="today">Today's date: a waiver past its expiry date no longer holds.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckOverlap(IReadOnlyList<Record> waivers, DateOnly today, List<Finding> findings)
    {
        var live = waivers
            .Where(waiver => Live(waiver, today))
            .OrderBy(waiver => waiver.Id, StringComparer.Ordinal)
            .ToList();

        for (var later = 1; later < live.Count; later++)
        {
            for (var earlier = 0; earlier < later; earlier++)
            {
                if (SharedCover(live[earlier], live[later]) is { } cover
                    && SharedScope(live[earlier], live[later]) is { } scope)
                {
                    findings.Add(new Finding("waiver-duplicate", $"Waiver '{live[later].Id}' overlaps waiver '{live[earlier].Id}': both cover '{cover}' on {scope}.", live[later].File, live[later].Line, SubjectOf(live[later])));
                }
            }
        }
    }

    /// <summary>Whether the waiver holds findings today: it has no expiry date, or its expiry date has not passed.</summary>
    /// <param name="waiver">The waiver record.</param>
    /// <param name="today">Today's date.</param>
    /// <returns>True when the waiver is live today.</returns>
    private static bool Live(Record waiver, DateOnly today) =>
        waiver.Field(RecordFields.ExpiresOn) is not { } expiresOn
        || (IsoDate(expiresOn, out var expires) && today <= expires);

    /// <summary>The first waives item both waivers name, in ordinal order, or null when they share none.</summary>
    /// <param name="first">The earlier live waiver.</param>
    /// <param name="second">The later live waiver.</param>
    /// <returns>The shared covers item, or null.</returns>
    private static string? SharedCover(Record first, Record second) =>
        first.Strings(RecordFields.Waives)
            .Intersect(second.Strings(RecordFields.Waives), StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .FirstOrDefault();

    /// <summary>The first scope both waivers share: an appliesTo record, else a path glob, or null when they share neither.</summary>
    /// <param name="first">The earlier live waiver.</param>
    /// <param name="second">The later live waiver.</param>
    /// <returns>The shared scope as finding text, or null.</returns>
    private static string? SharedScope(Record first, Record second)
    {
        var subject = first.Ids(RecordFields.AppliesTo)
            .Intersect(second.Ids(RecordFields.AppliesTo), StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .FirstOrDefault();

        if (subject is not null)
        {
            return $"'{subject}'";
        }

        var path = first.Ids(RecordFields.Paths)
            .Intersect(second.Ids(RecordFields.Paths), StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .FirstOrDefault();

        return path is null ? null : $"path '{path}'";
    }

    /// <summary>Why a waives item cannot hold findings, or null when it can.</summary>
    /// <param name="cover">The waives item.</param>
    /// <param name="standards">The shared provisions and pages, or null when no LiteSpec release is found.</param>
    /// <returns>The reason the item is refused, or null.</returns>
    private static string? Refusal(string cover, (HashSet<string> Provisions, HashSet<string> Pages)? standards)
    {
        if (FindingCodes.Load.Contains(cover))
        {
            return "which the model needs in order to load, so no waiver covers it";
        }

        if (FindingCodes.IsKnown(cover))
        {
            return null;
        }

        if (cover.StartsWith("litespec/", StringComparison.Ordinal))
        {
            if (standards is not { } known)
            {
                return null;
            }

            var shared = cover.Contains('#') ? known.Provisions.Contains(cover) : known.Pages.Contains(cover);

            if (shared)
            {
                return null;
            }

            return "which the installed release does not ship as a provision or page";
        }

        return "which no check emits, and which is no shared provision or page";
    }

    /// <summary>The shared provisions and pages of the installed release, or null when no release home is found.</summary>
    /// <returns>The provision identifiers and the page identifiers, or null.</returns>
    private static (HashSet<string> Provisions, HashSet<string> Pages)? Standards()
    {
        var home = LiteSpecHome.Root;

        if (home is null || !Directory.Exists(Path.Combine(home, "docs")))
        {
            return null;
        }

        var provisions = new HashSet<string>(StringComparer.Ordinal);
        var pages = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in Provisions.Collect(home))
        {
            provisions.Add(row.Id);
            pages.Add(row.Id.Split('#')[0]);
        }

        return (provisions, pages);
    }
}
