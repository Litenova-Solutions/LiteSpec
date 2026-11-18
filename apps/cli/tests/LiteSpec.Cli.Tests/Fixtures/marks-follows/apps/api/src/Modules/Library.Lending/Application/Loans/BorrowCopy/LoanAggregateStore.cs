namespace Library.Lending.Application.Loans.BorrowCopy;

/// <implements>aggregate/lending.loan</implements>
internal sealed class LoanAggregateStore
{
    public Task StoreAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
