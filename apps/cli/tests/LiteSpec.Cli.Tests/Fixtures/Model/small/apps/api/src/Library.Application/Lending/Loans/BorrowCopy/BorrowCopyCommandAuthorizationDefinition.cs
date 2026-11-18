namespace Library.Application.Lending.Loans.BorrowCopy;

internal sealed class BorrowCopyCommandAuthorizationDefinition : IAuthorizationDefinition<BorrowCopyCommand>
{
    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(AccessPolicies.Permission("borrow-copy"));
}
