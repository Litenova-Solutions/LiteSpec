using Library.Lending.Domain.Loans.Events;

namespace Library.Lending.Application.Notifications.LoanDue;

/// <implements>reaction/lending.send-due-notice-on-loan-borrowed</implements>
public sealed class SendDueNoticeOnLoanBorrowedHandler(IMailMessenger messenger) : IEventHandler<LoanBorrowedEvent>
{
    public Task HandleAsync(LoanBorrowedEvent fact, CancellationToken cancellationToken)
    {
        return messenger.SendAsync("loan-due-reader", cancellationToken);
    }
}
