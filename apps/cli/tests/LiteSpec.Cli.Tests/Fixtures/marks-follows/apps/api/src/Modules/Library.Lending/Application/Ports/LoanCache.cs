namespace Library.Lending.Application.Ports;

internal sealed class LoanCache
{
    public Task ClearAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
