namespace Library.Lending.Domain.Copies.Events;

/// <implements>domain-event/copy.copy-retired</implements>
public sealed record CopyRetiredEvent(Guid CopyId) : IDomainEvent;
