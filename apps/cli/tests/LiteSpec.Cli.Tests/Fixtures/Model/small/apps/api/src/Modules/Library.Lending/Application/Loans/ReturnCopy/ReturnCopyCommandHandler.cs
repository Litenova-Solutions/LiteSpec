using Library.Lending.Domain.Loans;
using Library.Lending.Domain.Loans.Exceptions;

namespace Library.Lending.Application.Loans.ReturnCopy;

/// <implements>use-case/lending.return-copy</implements>
public sealed class ReturnCopyCommandHandler : ICommandHandler<ReturnCopyCommand>
{
    public Task Handle(ReturnCopyCommand command)
    {
        if (command.LoanId == Guid.Empty)
            throw new LoanNotActiveException();
        var loan = new Loan();
        loan.Return();
        return Task.CompletedTask;
    }
}
