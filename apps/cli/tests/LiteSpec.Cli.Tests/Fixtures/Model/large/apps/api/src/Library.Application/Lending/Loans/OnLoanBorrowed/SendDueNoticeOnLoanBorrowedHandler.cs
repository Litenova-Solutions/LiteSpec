using Library.Domain.Lending.Loans.Events;

namespace Library.Application.Lending.Loans.OnLoanBorrowed;

/// <implements>automation/lending.send-due-notice-on-loan-borrowed</implements>
public sealed class SendDueNoticeOnLoanBorrowedHandler(IMailMessenger messenger)
    : IEventHandler<LoanBorrowedEvent>
{
    public async Task Handle(LoanBorrowedEvent domainEvent)
    {
        await messenger.SendAsync("loan-due-reader", domainEvent.LoanId);
    }
}
