using Library.Application.Lending.Loans.BorrowCopy;

namespace Library.Application.Billing.Loans.BorrowCopy;

/// <implements>use-case/lending.borrow-copy</implements>
public sealed class BorrowCopyRetryHandler : ICommandHandler<BorrowCopyCommand>
{
    public Task Handle(BorrowCopyCommand command)
    {
        return Task.CompletedTask;
    }
}
