namespace Library.Lending.Application.Ports;

public interface ILoanClock
{
    DateTimeOffset UtcNow();
}
