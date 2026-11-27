namespace Library.Lending.Application.Loans.ListLoans;

internal sealed class ListLoansQueryTenancyDefinition : ITenancyDefinition<ListLoansQuery>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
