namespace Library.Lending.Application.Loans.ExtendLoan;

public sealed record ExtendLoanCommand(Guid LoanId, int Days);
