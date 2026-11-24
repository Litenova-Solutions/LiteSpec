using System;

namespace LiteSpec.Model;

/// <summary>
///     The outcome the records state for a domain event, derived one way for the outbox
///     decision check and the generated event-decision table, so the two cannot diverge.
/// </summary>
/// <follows>litespec/ext/outbox#decide-every-domain-event-outcome</follows>
public static class DomainEventOutcomes
{
    /// <summary>Derives the outcome the records state from the ignored flag and whether a consumer names the event.</summary>
    /// <param name="ignored">Whether the event record states `ignored: true`.</param>
    /// <param name="consumed">Whether a reaction or a translating module or integration event consumes the event.</param>
    /// <returns>
    ///     Ignored when the record states `ignored`, even beside a consumer, whose
    ///     contradiction the check reports as `domain-event-outcome-drift`; otherwise
    ///     Consumed when a consumer names the event, else Missing, which the check
    ///     reports as `domain-event-outcome-missing`.
    /// </returns>
    public static DomainEventOutcome FromRecord(bool ignored, bool consumed) =>
        ignored ? DomainEventOutcome.Ignored : consumed ? DomainEventOutcome.Consumed : DomainEventOutcome.Missing;

    /// <summary>The word the check and the records use for the outcome: ignored, consumed, or missing.</summary>
    /// <param name="outcome">The derived outcome.</param>
    /// <returns>The outcome word the generated table writes and the check compares against.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The outcome is not a named value.</exception>
    public static string Value(DomainEventOutcome outcome) => outcome switch
    {
        DomainEventOutcome.Ignored => "ignored",
        DomainEventOutcome.Consumed => "consumed",
        DomainEventOutcome.Missing => "missing",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "The domain event outcome is not a named value."),
    };
}
