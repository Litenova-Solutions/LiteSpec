namespace Library.Domain.Lending.Copies.Events;

/// <implements>event/copy.copy-retired</implements>
public sealed record CopyRetiredEvent(Guid CopyId) : IDomainEvent;
