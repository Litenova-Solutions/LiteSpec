namespace Library.Domain.Lending.Loans.Events;

/// <implements>event/loan.loan-borrowed</implements>
public sealed record LoanBorrowedEvent(Guid LoanId, Guid CopyId, Guid ReaderId, DateOnly DueDate) : IDomainEvent;
