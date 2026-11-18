namespace Library.Lending.Application.Loans.BorrowCopy;

/// <implements>use-case/lending.borrow-copy</implements>
internal sealed class BorrowCopyValidator
{
    public Task ValidateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
