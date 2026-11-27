using LiteBus.Messaging.Abstractions;
using __NAMESPACE__.Platform.Access;

namespace __NAMESPACE__.Health.Application.HealthChecks.ReadCheckIn;

/// <summary>
/// Declares who may read a check-in: the signed-in person.
/// </summary>
internal sealed class ReadCheckInQueryAuthorizationDefinition : IAuthorizationDefinition<ReadCheckInQuery>
{
    /// <inheritdoc />
    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(AccessPolicies.SignedIn);
}
