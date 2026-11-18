namespace Library.Lending.Application.BorrowCopy;

internal sealed class BorrowCopyCommandIdempotencyDefinition : IIdempotencyDefinition<BorrowCopyCommand>
{
    public IdempotencyDeclaration Idempotency => IdempotencyDeclaration.NotIdempotent();
}
