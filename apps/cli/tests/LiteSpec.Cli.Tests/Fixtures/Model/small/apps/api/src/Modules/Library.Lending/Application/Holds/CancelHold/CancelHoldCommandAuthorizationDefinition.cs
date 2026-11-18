namespace Library.Lending.Application.Holds.CancelHold;

internal sealed class CancelHoldCommandAuthorizationDefinition : IAuthorizationDefinition<CancelHoldCommand>
{
    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(AccessPolicies.Permission("place-hold"));
}
