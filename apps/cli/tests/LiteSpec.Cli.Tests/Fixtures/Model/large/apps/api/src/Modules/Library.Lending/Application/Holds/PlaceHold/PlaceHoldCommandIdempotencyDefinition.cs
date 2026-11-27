namespace Library.Lending.Application.Holds.PlaceHold;

internal sealed class PlaceHoldCommandIdempotencyDefinition : IIdempotencyDefinition<PlaceHoldCommand>
{
    public IdempotencyDeclaration Idempotency => new Exempt<IdempotencyDeclaration>("Each call places its own hold, and placing a duplicate hold is refused.");
}
