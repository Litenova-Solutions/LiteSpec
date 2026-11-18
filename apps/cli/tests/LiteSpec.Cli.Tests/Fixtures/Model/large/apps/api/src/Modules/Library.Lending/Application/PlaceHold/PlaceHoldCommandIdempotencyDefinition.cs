namespace Library.Lending.Application.PlaceHold;

internal sealed class PlaceHoldCommandIdempotencyDefinition : IIdempotencyDefinition<PlaceHoldCommand>
{
    public IdempotencyDeclaration Idempotency => IdempotencyDeclaration.NotIdempotent();
}
