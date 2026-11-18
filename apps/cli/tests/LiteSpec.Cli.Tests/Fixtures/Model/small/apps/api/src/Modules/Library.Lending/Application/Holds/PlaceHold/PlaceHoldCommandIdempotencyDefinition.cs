namespace Library.Lending.Application.Holds.PlaceHold;

internal sealed class PlaceHoldCommandIdempotencyDefinition : IIdempotencyDefinition<PlaceHoldCommand>
{
    public IdempotencyDeclaration Idempotency => IdempotencyDeclaration.NotIdempotent();
}
