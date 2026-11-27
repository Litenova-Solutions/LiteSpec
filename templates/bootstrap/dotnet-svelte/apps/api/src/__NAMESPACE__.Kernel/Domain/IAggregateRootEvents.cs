namespace __NAMESPACE__.Kernel.Domain;

/// <summary>
/// The domain events an aggregate root holds, readable without its identifier
/// type. The outbox harvester receives each stored aggregate as an object and
/// reads and clears its events through this surface.
/// </summary>
public interface IAggregateRootEvents
{
    /// <summary>The domain events the last domain call raised, not yet staged.</summary>
    IReadOnlyCollection<IDomainEvent> StagedDomainEvents { get; }

    /// <summary>Removes and returns every staged domain event.</summary>
    IReadOnlyList<IDomainEvent> DrainDomainEvents();
}
