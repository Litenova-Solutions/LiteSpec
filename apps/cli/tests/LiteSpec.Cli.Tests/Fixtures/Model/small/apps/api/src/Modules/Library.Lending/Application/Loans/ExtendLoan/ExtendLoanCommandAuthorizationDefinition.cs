namespace Library.Lending.Application.Loans.ExtendLoan;

internal sealed class ExtendLoanCommandAuthorizationDefinition : IAuthorizationDefinition<ExtendLoanCommand>
{
    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(AccessPolicies.Permission("borrow-copy"));
}
