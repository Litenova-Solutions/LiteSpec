using Claimwick.Spending.Domain.Expenses.Events;

namespace Claimwick.Spending.Application.Notifications.ApprovalReminder;

/// <implements>reaction/spending.send-approver-reminders-on-expense-submitted</implements>
internal sealed class SendApproverRemindersOnExpenseSubmittedHandler : IEventHandler<ExpenseSubmittedEvent>
{
    private readonly IEmailMessenger _messenger;

    public SendApproverRemindersOnExpenseSubmittedHandler(IEmailMessenger messenger)
    {
        _messenger = messenger;
    }

    public async Task HandleAsync(ExpenseSubmittedEvent domainEvent, CancellationToken cancellationToken)
    {
        await _messenger.SendAsync("approval-reminder-manager", domainEvent.ExpenseId, cancellationToken);
    }
}
