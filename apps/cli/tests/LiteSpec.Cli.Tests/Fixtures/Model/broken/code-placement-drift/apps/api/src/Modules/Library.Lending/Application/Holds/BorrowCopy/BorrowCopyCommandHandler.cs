using Library.Lending.Application.Loans.BorrowCopy;

namespace Library.Lending.Application.Holds.BorrowCopy;

/// <implements>use-case/lending.borrow-copy</implements>
public sealed class BorrowCopyCommandHandler : ICommandHandler<BorrowCopyCommand>
{
    public Task Handle(BorrowCopyCommand command)
    {
        return Task.CompletedTask;
    }
}
