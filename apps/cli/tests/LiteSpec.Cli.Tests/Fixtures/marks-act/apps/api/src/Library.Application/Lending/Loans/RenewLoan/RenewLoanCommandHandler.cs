namespace Library.Application.Lending.Loans.RenewLoan;

/// <implements>use-case/lending.renew-loan</implements>
public sealed class RenewLoanCommandHandler
    : ICommandHandler<RenewLoanCommand, RenewLoanCommandResult>
{
    public Task<RenewLoanCommandResult> HandleAsync(RenewLoanCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(new RenewLoanCommandResult(command.LoanId));
    }
}
