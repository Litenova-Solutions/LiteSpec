namespace Library.Lending.Domain.Holds.Events;

/// <implements>domain-event/hold.hold-placed</implements>
public sealed record HoldPlacedEvent(Guid HoldId, Guid CopyId, Guid ReaderId) : IDomainEvent;
