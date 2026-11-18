namespace Library.Lending.Application.ExtendLoan;

public sealed record ExtendLoanCommand(Guid LoanId, int Days);
