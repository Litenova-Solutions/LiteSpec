namespace Library.Lending.Application.Copies.RegisterCopy;

internal sealed class RegisterCopyCommandIdempotencyDefinition : IIdempotencyDefinition<RegisterCopyCommand>
{
    public IdempotencyDeclaration Idempotency => new Exempt<IdempotencyDeclaration>("Each call registers its own copy, and registering the same barcode twice is refused.");
}
