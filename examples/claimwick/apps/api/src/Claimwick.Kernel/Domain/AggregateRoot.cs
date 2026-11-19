namespace Claimwick.Kernel.Domain;

/// <summary>The base of every aggregate root in Claimwick: its typed identity, the facts it raised, and their clearing.</summary>
public abstract class AggregateRoot<TId>
{
    private readonly List<IDomainEvent> pendingEvents = new();

    /// <summary>The aggregate identity.</summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>The version the store reads at commit. Domain code never reads or sets it.</summary>
    public long Version { get; private set; } = -1;

    /// <summary>The facts this aggregate raised since it was loaded or created.</summary>
    public IReadOnlyList<IDomainEvent> PendingEvents => pendingEvents;

    /// <summary>Records one fact this aggregate raised.</summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        pendingEvents.Add(domainEvent);
    }

    /// <summary>Clears the facts the collector staged.</summary>
    public void ClearEvents()
    {
        pendingEvents.Clear();
    }
}
