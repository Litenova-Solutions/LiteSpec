namespace Library.Application.Lending.Loans.RenewLoan;

public sealed record RenewLoanCommand(Guid LoanId);

public sealed record RenewLoanCommandResult(Guid LoanId);
