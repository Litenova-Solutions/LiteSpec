namespace Library.Lending.Domain.Loans.Events;

/// <implements>domain-event/loan.loan-purged</implements>
public sealed record LoanPurgedEvent(Guid LoanId) : IDomainEvent;
