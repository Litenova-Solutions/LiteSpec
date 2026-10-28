namespace Library.Domain.Lending.Loans.Events;

/// <implements>event/loan.loan-extended</implements>
public sealed record LoanExtendedEvent(Guid LoanId, DateOnly NewDueDate) : IDomainEvent;
