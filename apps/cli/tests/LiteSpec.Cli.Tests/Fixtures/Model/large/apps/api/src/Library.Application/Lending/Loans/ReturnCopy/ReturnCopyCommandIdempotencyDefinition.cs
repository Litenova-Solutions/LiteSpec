namespace Library.Application.Lending.Loans.ReturnCopy;

internal sealed class ReturnCopyCommandIdempotencyDefinition : IIdempotencyDefinition<ReturnCopyCommand>
{
    public IdempotencyDeclaration Idempotency => IdempotencyDeclaration.NotIdempotent();
}
