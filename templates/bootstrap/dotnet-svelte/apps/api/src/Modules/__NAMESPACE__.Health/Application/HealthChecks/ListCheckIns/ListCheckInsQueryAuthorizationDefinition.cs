using LiteBus.Messaging.Abstractions;
using __NAMESPACE__.Platform.Access;

namespace __NAMESPACE__.Health.Application.HealthChecks.ListCheckIns;

/// <summary>
/// Declares who may list check-ins: the signed-in person.
/// </summary>
internal sealed class ListCheckInsQueryAuthorizationDefinition : IAuthorizationDefinition<ListCheckInsQuery>
{
    /// <inheritdoc />
    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(AccessPolicies.SignedIn);
}
