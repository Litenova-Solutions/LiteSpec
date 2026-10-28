using Library.Domain.Lending.Loans.Events;

namespace Library.Application.Lending.Loans.OnLoanBorrowed;

/// <implements>reaction/lending.send-due-notice-on-loan-borrowed</implements>
public sealed class SendDueNoticeOnLoanBorrowedHandler : IEventHandler<LoanBorrowedEvent>
{
    public Task HandleAsync(LoanBorrowedEvent fact, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
