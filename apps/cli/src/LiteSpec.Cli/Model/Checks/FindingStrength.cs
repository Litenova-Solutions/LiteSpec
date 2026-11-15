using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Checks;

/// <summary>
///     Grades each finding by the strength of the provisions its code is checked by. A code whose every governing provision
///     is advisory reports at level A and never blocks; a code on a required provision keeps its level, so it blocks unless
///     a waiver parks it. A code no provision names keeps its level.
/// </summary>
/// <follows>litespec/core/principles#state-the-strength-of-every-provision</follows>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
internal static class FindingStrength
{
    /// <summary>Returns the findings with the tier of each advisory-only code set to A.</summary>
    /// <param name="findings">The findings from every check, in sorted order.</param>
    /// <param name="provisions">The provision rows of the LiteSpec release the project reviews, with the Checked by evidence each one holds.</param>
    /// <returns>The findings, each graded by the strength of the provisions that govern its code.</returns>
    internal static List<Finding> Apply(IReadOnlyList<Finding> findings, IReadOnlyList<ProvisionRow> provisions)
    {
        var advisoryOnly = new Dictionary<string, bool>(StringComparer.Ordinal);

        foreach (var code in findings.Select(finding => finding.Code).Distinct(StringComparer.Ordinal))
        {
            var governing = provisions.Where(row => row.Evidence.Contains($"`{code}`", StringComparison.Ordinal)).ToList();
            advisoryOnly[code] = governing.Count > 0 && governing.All(row => row.Strength == "advisory");
        }

        return findings
            .Select(finding => advisoryOnly.TryGetValue(finding.Code, out var advisory) && advisory && finding.Tier is not "A"
                ? finding with { TierOverride = "A" }
                : finding)
            .ToList();
    }
}
