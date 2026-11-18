namespace Library.Lending.Application.Holds.CancelHold;

internal sealed class CancelHoldCommandIdempotencyDefinition : IIdempotencyDefinition<CancelHoldCommand>
{
    public IdempotencyDeclaration Idempotency => IdempotencyDeclaration.NotIdempotent();
}
