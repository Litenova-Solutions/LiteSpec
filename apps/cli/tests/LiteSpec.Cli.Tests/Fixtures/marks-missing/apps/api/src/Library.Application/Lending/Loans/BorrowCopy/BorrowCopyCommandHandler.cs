namespace Library.Application.Lending.Loans.BorrowCopy;

public sealed class BorrowCopyCommandHandler
    : ICommandHandler<BorrowCopyCommand, BorrowCopyCommandResult>
{
    public Task<BorrowCopyCommandResult> HandleAsync(BorrowCopyCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(new BorrowCopyCommandResult(Guid.Empty));
    }
}
