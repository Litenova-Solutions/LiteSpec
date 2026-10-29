namespace Library.Application.Lending.Technical;

internal sealed class LoanCache
{
    public Task ClearAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
