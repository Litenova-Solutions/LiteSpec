namespace Library.Lending.Application.Holds.PlaceHold;

internal sealed class PlaceHoldCommandAuthorizationDefinition : IAuthorizationDefinition<PlaceHoldCommand>
{
    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(AccessPolicies.Permission("place-hold"));
}
