namespace Library.Domain.Lending.Copies.Events;

/// <implements>event/copy.copy-registered</implements>
public sealed record CopyRegisteredEvent(Guid CopyId, string Shelf) : IDomainEvent;
