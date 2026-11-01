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

    /// <summary>The finding is printed nowhere.</summary>
    Silent = 2,
}

/// <summary>
///     One finding's decided severity: whether it blocks, reports, or stays silent.
/// </summary>
/// <param name="Outcome">What the host does with the finding.</param>
/// <param name="Reason">Why a reported S finding does not block: draft, else null.</param>
public sealed record Severity(Outcome Outcome, string? Reason)
{
    /// <summary>
    ///     Decides one finding's severity from the record's own status: silent about
    ///     a retired record, reported for tier A, reported for tier S about a draft
    ///     record, reported for tier S under an unqualified reader, and blocking
    ///     otherwise. An inherited draft shows as a label in the book, never here.
    ///     Exceptions are set aside before, in Suppression.Partition.
    /// </summary>
    /// <param name="finding">The finding.</param>
    /// <param name="subjectOwnStatus">The own status of the finding's subject, or null when it names no record.</param>
    /// <param name="readerQualified">Whether the reader that produced the finding is qualified.</param>
    /// <returns>The decided severity.</returns>
    public static Severity Of(
        Finding finding,
        string? subjectOwnStatus,
        bool readerQualified)
    {
        if (string.Equals(subjectOwnStatus, "retired", StringComparison.Ordinal))
        {
            return new Severity(Outcome.Silent, null);
        }

        if (string.Equals(finding.Tier, "A", StringComparison.Ordinal))
        {
            return new Severity(Outcome.Reported, null);
        }

        if (string.Equals(finding.Tier, "S", StringComparison.Ordinal))
        {
            if (string.Equals(subjectOwnStatus, "draft", StringComparison.Ordinal))
            {
                return new Severity(Outcome.Reported, "draft");
            }

            if (!readerQualified)
            {
                return new Severity(Outcome.Reported, null);
            }
        }

        return new Severity(Outcome.Blocking, null);
    }
}
