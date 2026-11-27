namespace Library.Lending.Application.Loans.ExtendLoan;

internal sealed class ExtendLoanCommandTenancyDefinition : ITenancyDefinition<ExtendLoanCommand>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
