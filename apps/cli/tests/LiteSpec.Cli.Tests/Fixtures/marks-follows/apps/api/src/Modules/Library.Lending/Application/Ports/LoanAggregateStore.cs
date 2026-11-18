namespace Library.Lending.Application.Ports;

/// <implements>aggregate/lending.loan</implements>
internal sealed class LoanAggregateStore
{
    public Task StoreAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
