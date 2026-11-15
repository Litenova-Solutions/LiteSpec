namespace Library.Domain.Lending.Loans.Events;

/// <implements>domain-event/loan.loan-extended</implements>
public sealed record LoanExtendedEvent(Guid LoanId, DateOnly NewDueDate) : IDomainEvent;
