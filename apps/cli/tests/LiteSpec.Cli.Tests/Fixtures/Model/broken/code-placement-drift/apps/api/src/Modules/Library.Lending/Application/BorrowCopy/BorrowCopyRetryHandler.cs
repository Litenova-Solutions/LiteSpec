using Library.Lending.Application.BorrowCopy;

namespace Library.Billing.BorrowCopy;

/// <implements>use-case/lending.borrow-copy</implements>
public sealed class BorrowCopyRetryHandler : ICommandHandler<BorrowCopyCommand>
{
    public Task Handle(BorrowCopyCommand command)
    {
        return Task.CompletedTask;
    }
}
