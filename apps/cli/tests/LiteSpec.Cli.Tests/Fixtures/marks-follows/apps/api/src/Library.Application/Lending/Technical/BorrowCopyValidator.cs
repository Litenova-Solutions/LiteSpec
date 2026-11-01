namespace Library.Application.Lending.Technical;

/// <implements>use-case/lending.borrow-copy</implements>
internal sealed class BorrowCopyValidator
{
    public Task ValidateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
