namespace Library.Application.Lending.Technical;

/// <implements>aggregate/lending.loan</implements>
internal sealed class LoanAggregateStore
{
    public Task StoreAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
