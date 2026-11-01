namespace Library.Domain.Lending.Holds.Events;

/// <implements>event/hold.hold-placed</implements>
public sealed record HoldPlacedEvent(Guid HoldId, Guid CopyId, Guid ReaderId) : IDomainEvent;
