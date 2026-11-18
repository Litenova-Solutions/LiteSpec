namespace Library.Lending.Application.ReturnCopy;

internal sealed class ReturnCopyCommandIdempotencyDefinition : IIdempotencyDefinition<ReturnCopyCommand>
{
    public IdempotencyDeclaration Idempotency => IdempotencyDeclaration.NotIdempotent();
}
