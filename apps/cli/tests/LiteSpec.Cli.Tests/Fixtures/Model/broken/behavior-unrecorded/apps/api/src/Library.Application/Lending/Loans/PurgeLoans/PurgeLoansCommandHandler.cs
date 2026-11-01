namespace Library.Application.Lending.Loans.PurgeLoans;

/// <implements>use-case/lending.purge-loans</implements>
public sealed class PurgeLoansCommandHandler : ICommandHandler<PurgeLoansCommand>
{
    public Task Handle(PurgeLoansCommand command) => Task.CompletedTask;
}
