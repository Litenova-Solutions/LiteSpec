namespace Library.Domain.Lending.Copies.Events;

/// <implements>domain-event/copy.copy-retired</implements>
public sealed record CopyRetiredEvent(Guid CopyId) : IDomainEvent;
