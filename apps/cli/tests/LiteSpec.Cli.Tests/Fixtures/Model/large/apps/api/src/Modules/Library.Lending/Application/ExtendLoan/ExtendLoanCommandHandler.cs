using Library.Lending.Domain.Loans;
using Library.Lending.Domain.Loans.Exceptions;

namespace Library.Lending.Application.ExtendLoan;

/// <implements>use-case/lending.extend-loan</implements>
public sealed class ExtendLoanCommandHandler : ICommandHandler<ExtendLoanCommand>
{
    public Task Handle(ExtendLoanCommand command)
    {
        if (command.Days <= 0)
            throw new LoanExtensionRefusedException();
        var loan = new Loan();
        loan.Extend();
        return Task.CompletedTask;
    }
}
