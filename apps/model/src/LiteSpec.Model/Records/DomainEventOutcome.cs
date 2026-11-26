namespace LiteSpec.Model;

/// <summary>
///     The outcome the records state for a domain event: ignored, consumed, or missing.
/// </summary>
/// <follows>litespec/core/outbox#decide-every-domain-event-outcome</follows>
public enum DomainEventOutcome
{
    /// <summary>The event record states `ignored: true`: nothing consumes the event.</summary>
    Ignored = 0,

    /// <summary>A reaction or a translating module or integration event consumes the event.</summary>
    Consumed = 1,

    /// <summary>The records state neither: no consumer names the event and it states no `ignored`.</summary>
    Missing = 2,
}
