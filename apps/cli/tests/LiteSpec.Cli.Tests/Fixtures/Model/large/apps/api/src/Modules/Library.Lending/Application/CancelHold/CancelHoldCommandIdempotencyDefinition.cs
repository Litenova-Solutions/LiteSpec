namespace Library.Lending.Application.CancelHold;

internal sealed class CancelHoldCommandIdempotencyDefinition : IIdempotencyDefinition<CancelHoldCommand>
{
    public IdempotencyDeclaration Idempotency => IdempotencyDeclaration.NotIdempotent();
}
