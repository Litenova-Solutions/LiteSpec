namespace Library.Lending.Application.Loans.ReturnCopy;

internal sealed class ReturnCopyCommandAuthorizationDefinition : IAuthorizationDefinition<ReturnCopyCommand>
{
    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(AccessPolicies.Permission("borrow-copy"));
}
