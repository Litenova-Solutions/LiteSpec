using Library.Lending.Domain.Loans.Events;

namespace Library.Lending.Application.Notifications.LoanDue;

/// <implements>reaction/lending.send-due-notice-on-loan-borrowed</implements>
public sealed class SendDueNoticeOnLoanBorrowedHandler(IMailMessenger messenger)
    : IEventHandler<LoanBorrowedEvent>
{
    public async Task Handle(LoanBorrowedEvent domainEvent)
    {
        await messenger.SendAsync("loan-due-reader", domainEvent.LoanId);
    }
}
