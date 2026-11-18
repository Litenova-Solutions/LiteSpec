namespace Library.Lending.Application.ListLoans;

internal sealed class ListLoansQueryAuthorizationDefinition : IAuthorizationDefinition<ListLoansQuery>
{
    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(AccessPolicies.Permission("read-loans"));
}
