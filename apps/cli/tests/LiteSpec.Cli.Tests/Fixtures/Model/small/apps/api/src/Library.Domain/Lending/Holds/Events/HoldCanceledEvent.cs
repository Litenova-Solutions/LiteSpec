namespace Library.Domain.Lending.Holds.Events;

/// <implements>event/hold.hold-canceled</implements>
public sealed record HoldCanceledEvent(Guid HoldId) : IDomainEvent;
