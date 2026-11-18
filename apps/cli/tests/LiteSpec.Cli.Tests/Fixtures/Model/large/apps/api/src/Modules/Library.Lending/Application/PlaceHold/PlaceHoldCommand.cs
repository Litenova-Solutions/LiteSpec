namespace Library.Lending.Application.PlaceHold;

public sealed record PlaceHoldCommand(Guid CopyId, Guid ReaderId);
