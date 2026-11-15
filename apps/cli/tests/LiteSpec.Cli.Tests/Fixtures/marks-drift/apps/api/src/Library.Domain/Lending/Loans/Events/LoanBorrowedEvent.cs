namespace Library.Domain.Lending.Loans.Events;

/// <implements>domain-event/loan.loan-borrowed</implements>
public sealed record LoanBorrowedEvent(Guid LoanId) : IDomainEvent
{
    public const string Classification = "domain-event/loan.placed";
}
