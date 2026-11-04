using Library.Application.Lending.Loans.BorrowCopy;

namespace Library.Application.Lending.Holds.BorrowCopy;

/// <implements>use-case/lending.borrow-copy</implements>
public sealed class BorrowCopyCommandHandler : ICommandHandler<BorrowCopyCommand>
{
    public Task Handle(BorrowCopyCommand command)
    {
        return Task.CompletedTask;
    }
}
