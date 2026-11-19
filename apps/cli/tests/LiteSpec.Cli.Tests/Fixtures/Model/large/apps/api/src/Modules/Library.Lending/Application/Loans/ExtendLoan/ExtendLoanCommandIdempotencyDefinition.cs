namespace Library.Lending.Application.Loans.ExtendLoan;

internal sealed class ExtendLoanCommandIdempotencyDefinition : IIdempotencyDefinition<ExtendLoanCommand>
{
    public IdempotencyDeclaration Idempotency => new Exempt<IdempotencyDeclaration>("Each call extends its own loan, and extending past the due date is refused.");
}
