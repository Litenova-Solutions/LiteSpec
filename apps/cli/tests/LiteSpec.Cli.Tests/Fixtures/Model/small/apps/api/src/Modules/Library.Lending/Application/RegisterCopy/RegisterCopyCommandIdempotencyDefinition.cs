namespace Library.Lending.Application.RegisterCopy;

internal sealed class RegisterCopyCommandIdempotencyDefinition : IIdempotencyDefinition<RegisterCopyCommand>
{
    public IdempotencyDeclaration Idempotency => IdempotencyDeclaration.NotIdempotent();
}
