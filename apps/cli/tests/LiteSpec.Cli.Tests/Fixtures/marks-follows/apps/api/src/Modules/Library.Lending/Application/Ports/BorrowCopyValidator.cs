namespace Library.Lending.Application.Ports;

/// <implements>use-case/lending.borrow-copy</implements>
internal sealed class BorrowCopyValidator
{
    public Task ValidateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
