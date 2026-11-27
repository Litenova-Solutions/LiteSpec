namespace Library.Lending.Application.Holds.CancelHold;

internal sealed class CancelHoldCommandIdempotencyDefinition : IIdempotencyDefinition<CancelHoldCommand>
{
    public IdempotencyDeclaration Idempotency => new Exempt<IdempotencyDeclaration>("Each call cancels its own hold, and canceling an ended hold is refused.");
}
