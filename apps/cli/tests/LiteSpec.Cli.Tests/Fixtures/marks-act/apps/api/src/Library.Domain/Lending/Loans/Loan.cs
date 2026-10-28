namespace Library.Domain.Lending.Loans;

/// <implements>aggregate/lending.loan</implements>
public sealed class Loan : AggregateRoot<LoanId>
{
    public Guid Id { get; init; }

    /// <implements>use-case/lending.borrow-copy</implements>
    /// <implements>use-case/lending.renew-loan</implements>
    public void Borrow(Guid readerId, DateOnly dueDate)
    {
        RaiseDomainEvent(new LoanBorrowedEvent(Id, readerId, dueDate));
    }
}
