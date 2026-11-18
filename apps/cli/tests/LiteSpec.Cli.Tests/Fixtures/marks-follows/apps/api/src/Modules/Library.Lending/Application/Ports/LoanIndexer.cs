namespace Library.Lending.Application.Ports;

/// <follows>decision/0099-no-such-decision</follows>
internal sealed class LoanIndexer
{
    public Task IndexAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
