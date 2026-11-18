namespace Library.Lending.Application.Loans.BorrowCopy;

public sealed record LoanSummary(Guid LoanId)
{
    public bool IsOpen() => true;
}
