namespace Library.Lending.Application.Ports;

internal sealed class LoanGeneratedClient
{
    public Task FetchAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
