namespace Library.Lending.ReturnCopy;

internal sealed class ReturnCopyCommandAuthorizationDefinition : IAuthorizationDefinition<ReturnCopyCommand>
{
    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(AccessPolicies.Permission("borrow-copy"));
}
