namespace Library.Lending.Application.Loans.BorrowCopy;

/// <follows>waiver/export-review-bypass</follows>
internal sealed class ExportStep
{
    public Task ExportAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
