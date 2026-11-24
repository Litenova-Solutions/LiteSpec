using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteSpec.Model;

/// <summary>
///     One finding's decided outcome: whether it blocks, reports or is covered by a waiver.
/// </summary>
/// <param name="Outcome">What the host does with the finding.</param>
public sealed record Severity(Outcome Outcome)
{
    /// <summary>
    ///     Decides one finding's severity from its tier and the reader: reported for tier R, reported for tier S under an unqualified
    ///     reader, and blocking otherwise. Waived findings are set aside before, in Suppression.Partition.
    /// </summary>
    /// <param name="finding">The finding.</param>
    /// <param name="readerQualified">Whether the reader that produced the finding is qualified.</param>
    /// <returns>The decided severity.</returns>
    public static Severity Of(
        Finding finding,
        bool readerQualified)
    {
        if (string.Equals(finding.Tier, "R", StringComparison.Ordinal))
        {
            return new Severity(Outcome.Reported);
        }

        if (string.Equals(finding.Tier, "S", StringComparison.Ordinal) && !readerQualified)
        {
            return new Severity(Outcome.Reported);
        }

        return new Severity(Outcome.Blocking);
    }
}
