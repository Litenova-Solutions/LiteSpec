namespace Library.Domain.Lending.Loans.Events;

/// <implements>event/loan.loan-borrowed</implements>
public sealed record LoanBorrowedEvent(Guid LoanId) : IDomainEvent
{
    public const string Classification = "event/loan.placed";
}
