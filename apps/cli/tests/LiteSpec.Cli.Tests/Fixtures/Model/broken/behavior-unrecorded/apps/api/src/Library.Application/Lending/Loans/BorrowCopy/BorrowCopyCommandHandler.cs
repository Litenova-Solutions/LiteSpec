using Library.Domain.Lending.Loans;
using Library.Domain.Lending.Loans.Exceptions;

namespace Library.Application.Lending.Loans.BorrowCopy;

/// <implements>use-case/lending.borrow-copy</implements>
public sealed class BorrowCopyCommandHandler : ICommandHandler<BorrowCopyCommand>
{
    // A dotted literal outside a Code member declares no refusal code.
    private const string AuditAction = "lending.borrow-copy";

    public Task Handle(BorrowCopyCommand command)
    {
        if (command.CopyId == Guid.Empty)
            throw new CopyNotAvailableException();
        // A throw-site literal declares no refusal code; the ghost refusal is
        // declared by the LoanGhostRefusalException in this overlay instead.
        if (command.ReaderId == Guid.Empty)
            throw new InvalidOperationException("lending.ghost-refusal");
        var loan = new Loan();
        loan.Borrow();
        return Task.CompletedTask;
    }
}
