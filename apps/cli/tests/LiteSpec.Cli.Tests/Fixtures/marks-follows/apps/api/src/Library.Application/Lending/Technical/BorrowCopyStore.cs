namespace Library.Application.Lending.Technical;

/// <implements>use-case/lending.borrow-copy</implements>
/// <follows>decision/0002-store-loans-as-documents</follows>
internal sealed class BorrowCopyStore
{
    public Task StoreAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
