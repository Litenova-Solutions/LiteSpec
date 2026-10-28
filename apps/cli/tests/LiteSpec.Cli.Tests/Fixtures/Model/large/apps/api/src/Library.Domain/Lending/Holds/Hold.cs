using Library.Domain.Lending.Holds.Events;

namespace Library.Domain.Lending.Holds;

/// <implements>aggregate/lending.hold</implements>
public sealed class Hold : AggregateRoot<Guid>
{
    public Guid Id { get; init; }

    /// <implements>use-case/lending.place-hold</implements>
    public void Place()
    {
        RaiseDomainEvent(new HoldPlacedEvent(Id, Guid.Empty, Guid.Empty));
    }

    /// <implements>use-case/lending.cancel-hold</implements>
    public void Cancel()
    {
        RaiseDomainEvent(new HoldCanceledEvent(Id));
    }
}
