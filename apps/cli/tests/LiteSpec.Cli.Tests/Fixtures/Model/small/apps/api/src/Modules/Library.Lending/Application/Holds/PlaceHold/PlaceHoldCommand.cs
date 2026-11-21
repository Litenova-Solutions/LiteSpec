namespace Library.Lending.Application.Holds.PlaceHold;

public sealed record PlaceHoldCommand(Guid CopyId, Guid ReaderId);
