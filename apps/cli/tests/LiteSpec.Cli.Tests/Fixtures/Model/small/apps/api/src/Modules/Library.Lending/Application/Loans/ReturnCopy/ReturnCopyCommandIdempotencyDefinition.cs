namespace Library.Lending.Application.Loans.ReturnCopy;

internal sealed class ReturnCopyCommandIdempotencyDefinition : IIdempotencyDefinition<ReturnCopyCommand>
{
    public IdempotencyDeclaration Idempotency => new Exempt<IdempotencyDeclaration>("Each call returns its own copy, and returning an inactive loan is refused.");
}
