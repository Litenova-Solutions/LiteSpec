namespace Library.Lending.Application.Loans.RenewLoan;

public sealed record RenewLoanCommand(Guid LoanId, Guid ReaderId);
