namespace Library.Lending.Application.Notifications.LoanDue;

/// <summary>Watches for overdue loans.</summary>
public sealed class FlagLoanOnLoanOverdueHandler : IEventHandler<LoanOverdueEvent>
{
    public Task Handle(LoanOverdueEvent domainEvent) => Task.CompletedTask;
}
