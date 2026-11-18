namespace Library.Lending.Application.BorrowCopy;

internal sealed class BorrowCopyCommandAuthorizationDefinition : IAuthorizationDefinition<BorrowCopyCommand>
{
    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(AccessPolicies.Permission("borrow-copy"));
}
