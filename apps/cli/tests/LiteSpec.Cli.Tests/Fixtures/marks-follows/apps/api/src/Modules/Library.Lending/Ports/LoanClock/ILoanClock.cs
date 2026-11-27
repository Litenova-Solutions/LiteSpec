namespace Library.Lending.Ports.LoanClock;

public interface ILoanClock
{
    DateTimeOffset UtcNow();
}
