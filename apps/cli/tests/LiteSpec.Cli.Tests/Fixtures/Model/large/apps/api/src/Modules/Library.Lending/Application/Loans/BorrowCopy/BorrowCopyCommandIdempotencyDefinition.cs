namespace Library.Lending.Application.Loans.BorrowCopy;

internal sealed class BorrowCopyCommandIdempotencyDefinition : IIdempotencyDefinition<BorrowCopyCommand>
{
    public IdempotencyDeclaration Idempotency => new Exempt<IdempotencyDeclaration>("Each call lends its own copy, and lending an unavailable copy is refused.");
}
