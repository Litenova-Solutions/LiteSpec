namespace Library.Application.Lending.Loans.BorrowCopy;

/// <implements>use-case/lending.borrow-copy</implements>
/// <implements>use-case/lending.register-copy</implements>
public sealed class BorrowCopyCommandHandler
    : ICommandHandler<BorrowCopyCommand, BorrowCopyCommandResult>
{
    public Task<BorrowCopyCommandResult> HandleAsync(BorrowCopyCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(new BorrowCopyCommandResult(Guid.Empty));
    }
}
