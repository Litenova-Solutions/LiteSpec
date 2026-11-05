using Claimwick.Spending.Expenses.Domain.Events;

namespace Claimwick.Spending.UseCases.OnExpenseSubmitted;

/// <implements>reaction/spending.send-approver-reminders-on-expense-submitted</implements>
internal sealed class SendApproverRemindersOnExpenseSubmittedHandler(IEmailMessenger messenger)
    : IEventHandler<ExpenseSubmittedEvent>
{
    public async Task HandleAsync(ExpenseSubmittedEvent domainEvent, CancellationToken cancellationToken)
    {
        await messenger.SendAsync("approval-reminder-manager", domainEvent.ExpenseId, cancellationToken);
    }
}
