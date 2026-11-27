namespace Library.Lending.Application.Loans.BorrowCopy;

public sealed record BorrowCopyCommand(Guid CopyId, Guid ReaderId);

public sealed record BorrowCopyCommandResult(Guid LoanId);
