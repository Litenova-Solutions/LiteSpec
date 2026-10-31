using Claimwick.Domain.Spending.Expenses.Events;

namespace Claimwick.Application.Spending.Expenses.OnExpenseSubmitted;

/// <implements>automation/spending.send-approver-reminders-on-expense-submitted</implements>
internal sealed class SendApproverRemindersOnExpenseSubmittedHandler(IEmailMessenger messenger)
    : IEventHandler<ExpenseSubmittedEvent>
{
    public async Task HandleAsync(ExpenseSubmittedEvent domainEvent, CancellationToken cancellationToken)
    {
        await messenger.SendAsync("approval-reminder-manager", domainEvent.ExpenseId, cancellationToken);
    }
}
