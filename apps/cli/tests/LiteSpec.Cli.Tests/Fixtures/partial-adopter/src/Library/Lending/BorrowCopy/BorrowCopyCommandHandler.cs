using Library.Loans;
using Library.Loans.Exceptions;

namespace Library.Lending.BorrowCopy;

/// <implements>use-case/lending.borrow-copy</implements>
public sealed class BorrowCopyCommandHandler : ICommandHandler<BorrowCopyCommand>
{
    public Task Handle(BorrowCopyCommand command)
    {
        if (command.CopyId == Guid.Empty)
            throw new CopyNotAvailableException();
        var loan = new Loan();
        loan.Borrow();
        return Task.CompletedTask;
    }
}
