namespace Library.Lending.Application.ExtendLoan;

internal sealed class ExtendLoanCommandIdempotencyDefinition : IIdempotencyDefinition<ExtendLoanCommand>
{
    public IdempotencyDeclaration Idempotency => IdempotencyDeclaration.NotIdempotent();
}
