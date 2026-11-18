namespace Library.Lending.Application.BorrowCopy;

public sealed record BorrowCopyCommand(Guid CopyId, Guid ReaderId);
