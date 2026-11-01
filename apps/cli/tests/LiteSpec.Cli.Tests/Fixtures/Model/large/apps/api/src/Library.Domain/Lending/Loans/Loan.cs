using Library.Domain.Lending.Loans.Events;

namespace Library.Domain.Lending.Loans;

/// <implements>aggregate/lending.loan</implements>
public sealed class Loan : AggregateRoot<Guid>
{
    public Guid Id { get; init; }

    /// <implements>use-case/lending.borrow-copy</implements>
    public void Borrow()
    {
        RaiseDomainEvent(new LoanBorrowedEvent(Id, Guid.Empty, Guid.Empty, new DateOnly(2026, 1, 1)));
    }

    /// <implements>use-case/lending.extend-loan</implements>
    public void Extend()
    {
        RaiseDomainEvent(new LoanExtendedEvent(Id, new DateOnly(2026, 2, 1)));
    }

    /// <implements>use-case/lending.return-copy</implements>
    public void Return()
    {
        RaiseDomainEvent(new LoanReturnedEvent(Id, DateTimeOffset.UtcNow));
    }
}
