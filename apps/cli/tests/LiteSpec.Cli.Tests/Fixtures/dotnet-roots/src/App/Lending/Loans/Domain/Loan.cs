namespace Library.Lending.Loans;

/// <implements>aggregate/lending.loan</implements>
public sealed class Loan
{
    public LoanState State { get; private set; } = new LoanActiveState();

    /// <implements>use-case/lending.borrow-copy</implements>
    public void Borrow()
    {
        State = new LoanBorrowedState();
    }
}

public abstract record LoanState;

public sealed record LoanActiveState : LoanState;

public sealed record LoanBorrowedState : LoanState;
