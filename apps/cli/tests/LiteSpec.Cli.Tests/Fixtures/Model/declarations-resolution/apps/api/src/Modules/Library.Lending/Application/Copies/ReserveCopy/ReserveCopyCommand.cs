namespace Library.Lending.Application.Copies.ReserveCopy;

public sealed class ReserveCopyCommand
{
    public required CopyId Copy { get; init; }
    public required Guid ReaderId { get; init; }
}
