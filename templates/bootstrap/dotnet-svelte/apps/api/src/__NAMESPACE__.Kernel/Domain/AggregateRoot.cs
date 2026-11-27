using __NAMESPACE__.Kernel.Values;

namespace __NAMESPACE__.Kernel.Domain;

/// <summary>
/// The root of an aggregate. Carries the typed identifier and the domain events
/// the last domain call raised; the handler drains them after staging.
/// </summary>
/// <typeparam name="TId">The aggregate's identifier type.</typeparam>
public abstract class AggregateRoot<TId> : IAggregateRootEvents
    where TId : struct, ITypedId
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Creates a root with its identifier.</summary>
    protected AggregateRoot(TId id)
    {
        Id = id;
    }

    /// <summary>The aggregate identifier.</summary>
    public TId Id { get; private set; }

    /// <summary>The domain events the last domain call raised, not yet translated.</summary>
    public IReadOnlyCollection<IDomainEvent> StagedDomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Removes and returns every staged domain event. Called by the outbox harvester,
    /// which stages them into the same commit as the aggregate change.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DrainDomainEvents()
    {
        var drained = _domainEvents.ToList();
        _domainEvents.Clear();
        return drained;
    }

    /// <summary>Raises a domain event from a domain method that already validated.</summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
