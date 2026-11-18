namespace Library.Application.Lending.Loans.ExtendLoan;

internal sealed class ExtendLoanCommandIdempotencyDefinition : IIdempotencyDefinition<ExtendLoanCommand>
{
    public IdempotencyDeclaration Idempotency => IdempotencyDeclaration.NotIdempotent();
}
