namespace Library.Application.Lending.Loans.RenewLoan;

[Implements("use-case/lending.renew-loan")]
public sealed class RenewLoanCommandHandler
    : ICommandHandler<RenewLoanCommand, RenewLoanCommandResult>
{
    public Task<RenewLoanCommandResult> HandleAsync(RenewLoanCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(new RenewLoanCommandResult(command.LoanId));
    }
}
