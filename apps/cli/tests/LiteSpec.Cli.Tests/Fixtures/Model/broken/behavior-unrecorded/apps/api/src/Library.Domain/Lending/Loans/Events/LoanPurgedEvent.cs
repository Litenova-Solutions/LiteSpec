namespace Library.Domain.Lending.Loans.Events;

/// <implements>event/loan.loan-purged</implements>
public sealed record LoanPurgedEvent(Guid LoanId) : IDomainEvent;
