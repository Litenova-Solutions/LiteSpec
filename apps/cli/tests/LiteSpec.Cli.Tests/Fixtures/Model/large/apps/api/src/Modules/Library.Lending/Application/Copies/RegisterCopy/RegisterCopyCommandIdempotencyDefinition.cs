namespace Library.Lending.Application.Copies.RegisterCopy;

internal sealed class RegisterCopyCommandIdempotencyDefinition : IIdempotencyDefinition<RegisterCopyCommand>
{
    public IdempotencyDeclaration Idempotency => IdempotencyDeclaration.NotIdempotent();
}
