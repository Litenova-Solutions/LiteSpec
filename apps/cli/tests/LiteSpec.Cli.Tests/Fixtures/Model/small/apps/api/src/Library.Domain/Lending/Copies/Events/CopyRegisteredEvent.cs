namespace Library.Domain.Lending.Copies.Events;

/// <implements>domain-event/copy.copy-registered</implements>
public sealed record CopyRegisteredEvent(Guid CopyId, string Shelf) : IDomainEvent;
