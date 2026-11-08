using System;
using System.Collections.Generic;
using System.Linq;

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
    ///     The finding codes about planned Intent: a requirement, step, goal or
    ///     constraint that no Behavior record reaches yet. They report locally and
    ///     block only in the pull-request check, so Intent can run ahead of Behavior.
    /// </summary>
    public static readonly IReadOnlyCollection<string> PlannedIntent = new HashSet<string>(StringComparer.Ordinal)
    {
        "intent-unreached", "requirement-fulfillment-missing", "step-use-case-missing", "goal-mover-missing", "constraint-unreached",
    };

    /// <summary>
    ///     Decides one finding's severity from its tier, the reader and where the
    ///     check runs: reported for tier A, reported for tier S under an unqualified
    ///     reader, reported for planned Intent outside continuous integration, and
    ///     blocking otherwise. Exceptions are set aside before, in Suppression.Partition.
    /// </summary>
    /// <param name="finding">The finding.</param>
    /// <param name="readerQualified">Whether the reader that produced the finding is qualified.</param>
    /// <param name="continuousIntegration">Whether the check runs in continuous integration, the pull-request check.</param>
    /// <returns>The decided severity.</returns>
    public static Severity Of(
        Finding finding,
        bool readerQualified,
        bool continuousIntegration)
    {
        if (string.Equals(finding.Tier, "A", StringComparison.Ordinal))
        {
            return new Severity(Outcome.Reported);
        }

        if (string.Equals(finding.Tier, "S", StringComparison.Ordinal) && !readerQualified)
        {
            return new Severity(Outcome.Reported);
        }

        if (!continuousIntegration && PlannedIntent.Contains(finding.Code))
        {
            return new Severity(Outcome.Reported);
        }

        return new Severity(Outcome.Blocking);
    }
}
