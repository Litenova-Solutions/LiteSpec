namespace Library.Lending.Application.CancelHold;

internal sealed class CancelHoldCommandAuthorizationDefinition : IAuthorizationDefinition<CancelHoldCommand>
{
    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(AccessPolicies.Permission("place-hold"));
}
