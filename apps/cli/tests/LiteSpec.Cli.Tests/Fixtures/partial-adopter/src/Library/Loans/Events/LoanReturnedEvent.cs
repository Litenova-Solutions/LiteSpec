namespace Library.Loans.Events;

/// <implements>event/loan.loan-returned</implements>
public sealed record LoanReturnedEvent(Guid LoanId, DateTimeOffset ReturnedAt) : IDomainEvent;
