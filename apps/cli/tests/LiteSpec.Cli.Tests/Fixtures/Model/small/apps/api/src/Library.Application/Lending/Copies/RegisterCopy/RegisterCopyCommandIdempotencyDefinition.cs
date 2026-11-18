namespace Library.Application.Lending.Copies.RegisterCopy;

internal sealed class RegisterCopyCommandIdempotencyDefinition : IIdempotencyDefinition<RegisterCopyCommand>
{
    public IdempotencyDeclaration Idempotency => IdempotencyDeclaration.NotIdempotent();
}
