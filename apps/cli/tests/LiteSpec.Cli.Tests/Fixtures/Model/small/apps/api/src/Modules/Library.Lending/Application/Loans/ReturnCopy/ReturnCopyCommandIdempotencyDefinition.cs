namespace Library.Lending.Application.Loans.ReturnCopy;

internal sealed class ReturnCopyCommandIdempotencyDefinition : IIdempotencyDefinition<ReturnCopyCommand>
{
    public IdempotencyDeclaration Idempotency => IdempotencyDeclaration.NotIdempotent();
}
