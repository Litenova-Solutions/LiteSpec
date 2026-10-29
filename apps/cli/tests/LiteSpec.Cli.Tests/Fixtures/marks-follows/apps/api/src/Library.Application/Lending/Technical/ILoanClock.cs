namespace Library.Application.Lending.Technical;

public interface ILoanClock
{
    DateTimeOffset UtcNow();
}
