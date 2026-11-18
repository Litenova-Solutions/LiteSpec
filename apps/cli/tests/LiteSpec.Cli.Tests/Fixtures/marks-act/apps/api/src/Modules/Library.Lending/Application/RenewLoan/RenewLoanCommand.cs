namespace Library.Lending.Application.RenewLoan;

public sealed record RenewLoanCommand(Guid LoanId);

public sealed record RenewLoanCommandResult(Guid LoanId);
