namespace Library.Application.Lending.Holds.PlaceHold;

internal sealed class PlaceHoldCommandIdempotencyDefinition : IIdempotencyDefinition<PlaceHoldCommand>
{
    public IdempotencyDeclaration Idempotency => IdempotencyDeclaration.NotIdempotent();
}
