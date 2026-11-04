using System;

namespace LiteSpec.Model;

/// <summary>
///     What a check host does with a finding once exceptions are set aside.
/// </summary>
public enum Outcome
{
    /// <summary>The finding is printed and fails the check.</summary>
    Blocking = 0,

    /// <summary>The finding is printed and the check passes.</summary>
    Reported = 1,
}

/// <summary>
///     One finding's decided severity: whether it blocks or reports.
/// </summary>
/// <param name="Outcome">What the host does with the finding.</param>
public sealed record Severity(Outcome Outcome)
{
    /// <summary>
    ///     Decides one finding's severity from its tier and the reader:
    ///     reported for tier A, reported for tier S under an unqualified
    ///     reader, and blocking otherwise. Exceptions are set aside before,
    ///     in Suppression.Partition.
    /// </summary>
    /// <param name="finding">The finding.</param>
    /// <param name="readerQualified">Whether the reader that produced the finding is qualified.</param>
    /// <returns>The decided severity.</returns>
    public static Severity Of(
        Finding finding,
        bool readerQualified)
    {
        if (string.Equals(finding.Tier, "A", StringComparison.Ordinal))
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
