namespace Library.Lending.Application.Ports;

public sealed record LoanSummary(Guid LoanId)
{
    public bool IsOpen() => true;
}
