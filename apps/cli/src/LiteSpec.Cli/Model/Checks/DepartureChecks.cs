using LiteSpec.Authoring;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>
///     The departure checks: every covers item names a finding code the tools emit, a shared
///     provision or a shared page, and never a code the model needs in order to load; a departure
///     with an expiry date stops holding once the date passes.
/// </summary>
/// <follows>litespec/core/model#record-accepted-gaps-as-exceptions</follows>
internal static class DepartureChecks
{
    /// <summary>Reports departure-invalid for each covers item that cannot hold findings, and departure-expired for each departure past its expiry date.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var departures = model.Records.Where(record => record.Type == RecordTypes.Departure).ToList();

        if (departures.Count == 0)
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var standards = Standards();

        foreach (var departure in departures)
        {
            foreach (var cover in departure.Strings(RecordFields.Covers).Order(StringComparer.Ordinal))
            {
                if (Refusal(cover, standards) is { } reason)
                {
                    findings.Add(new Finding("departure-invalid", $"Departure '{departure.Id}' names '{cover}', {reason}.", departure.File, departure.Line, SubjectOf(departure)));
                }
            }

            if (departure.Field(RecordFields.ExpiresOn) is { } expiresOn
                && IsoDate(expiresOn, out var expires)
                && today > expires)
            {
                findings.Add(new Finding("departure-expired", $"Departure '{departure.Id}' stands past its expiresOn {expiresOn}.", departure.File, departure.Line, SubjectOf(departure)));
            }
        }
    }

    /// <summary>Why a covers item cannot hold findings, or null when it can.</summary>
    /// <param name="cover">The covers item.</param>
    /// <param name="standards">The shared provisions and pages, or null when no LiteSpec release is found.</param>
    /// <returns>The reason the item is refused, or null.</returns>
    private static string? Refusal(string cover, (HashSet<string> Provisions, HashSet<string> Pages)? standards)
    {
        if (FindingCodes.Load.Contains(cover))
        {
            return "which the model needs in order to load, so no departure covers it";
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
