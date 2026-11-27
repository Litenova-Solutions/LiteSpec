namespace Library.Loans.Events;

/// <implements>domain-event/loan.loan-returned</implements>
public sealed record LoanReturnedEvent(Guid LoanId, DateTimeOffset ReturnedAt) : IDomainEvent;
