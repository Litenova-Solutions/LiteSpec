namespace Library.Lending.Application.Loans.RenewLoan;

/// <implements>use-case/lending.renew-loan</implements>
public sealed class RenewLoanCommandHandler : ICommandHandler<RenewLoanCommand>
{
    public Task Handle(RenewLoanCommand command)
    {
        return Task.CompletedTask;
    }
}
