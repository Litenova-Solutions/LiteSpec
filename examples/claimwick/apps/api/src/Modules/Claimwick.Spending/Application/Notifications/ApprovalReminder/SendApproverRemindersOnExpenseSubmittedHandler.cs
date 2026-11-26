using Claimwick.Spending.Domain.Expenses.Events;
using Claimwick.Spending.Ports.MailRelay;
using LiteBus.Events.Abstractions;

namespace Claimwick.Spending.Application.Notifications.ApprovalReminder;

/// <implements>reaction/spending.send-approver-reminders-on-expense-submitted</implements>
internal sealed class SendApproverRemindersOnExpenseSubmittedHandler : IEventHandler<ExpenseSubmittedDomainEvent>
{
    private readonly IMailRelay _messenger;

    public SendApproverRemindersOnExpenseSubmittedHandler(IMailRelay messenger)
    {
        _messenger = messenger;
    }

    public async Task HandleAsync(ExpenseSubmittedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await _messenger.SendAsync("approval-reminder-manager", domainEvent.ExpenseId.Value, cancellationToken);
    }
}
