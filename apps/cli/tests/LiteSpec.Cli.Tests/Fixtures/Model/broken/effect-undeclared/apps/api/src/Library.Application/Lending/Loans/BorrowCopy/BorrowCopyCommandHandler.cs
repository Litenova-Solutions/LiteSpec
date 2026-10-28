using Library.Domain.Lending.Loans;
using Library.Domain.Lending.Loans.Exceptions;

namespace Library.Application.Lending.Loans.BorrowCopy;

/// <implements>use-case/lending.borrow-copy</implements>
public sealed class BorrowCopyCommandHandler(ICommandMediator mediator) : ICommandHandler<BorrowCopyCommand>
{
    // A dotted literal outside a Code member declares no refusal code.
    private const string AuditAction = "lending.borrow-copy";

    public async Task Handle(BorrowCopyCommand command)
    {
        if (command.CopyId == Guid.Empty)
            throw new CopyNotAvailableException();
        var loan = new Loan();
        loan.Borrow();
        await mediator.SendAsync(new PurgeLoansCommand());
    }
}
