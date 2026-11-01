namespace Library.Application.Lending.Technical;

public sealed record LoanSummary(Guid LoanId)
{
    public bool IsOpen() => true;
}
