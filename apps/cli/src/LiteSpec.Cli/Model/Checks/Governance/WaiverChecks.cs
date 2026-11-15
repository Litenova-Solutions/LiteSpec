using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.ProseText;

namespace LiteSpec.Cli.Model.Checks.Governance;

/// <summary>
///     The waiver checks: every covers item names a finding code the tools emit, a shared
///     provision or a shared page, and never a code the model needs in order to load; a waiver
///     with an expiry date stops holding once the date passes.
/// </summary>
/// <follows>litespec/core/waivers#record-accepted-gaps-as-waivers</follows>
internal static class WaiverChecks
{
    /// <summary>Reports waiver-invalid for each covers item that cannot hold findings, and waiver-expired for each waiver past its expiry date.</summary>
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
            foreach (var cover in waiver.Strings(RecordFields.Covers).Order(StringComparer.Ordinal))
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
    }

    /// <summary>Why a covers item cannot hold findings, or null when it can.</summary>
    /// <param name="cover">The covers item.</param>
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
