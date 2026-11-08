namespace Library.Lending.BorrowCopy;

public sealed record BorrowCopyCommand(Guid CopyId, Guid ReaderId);
