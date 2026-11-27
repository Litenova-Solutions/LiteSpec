namespace __NAMESPACE__.Kernel.Domain;

/// <summary>
/// A fact an aggregate root raised about a change it made. The root keeps each raised
/// event until the outbox stages them into the commit with the change.
/// </summary>
public interface IDomainEvent;
