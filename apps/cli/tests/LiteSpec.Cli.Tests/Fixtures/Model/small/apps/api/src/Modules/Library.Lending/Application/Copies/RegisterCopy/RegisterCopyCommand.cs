namespace Library.Lending.Application.Copies.RegisterCopy;

public sealed record RegisterCopyCommand(Guid CopyId, string Shelf);
