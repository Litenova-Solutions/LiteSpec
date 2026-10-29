namespace Library.Infrastructure.Lending;

/// <follows>decision/0002-store-loans-as-documents</follows>
internal sealed class LoanDocumentStore
{
    public Task StoreAsync(Guid loanId, CancellationToken cancellationToken) => Task.CompletedTask;
}
