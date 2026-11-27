using LiteBus.Messaging.Abstractions;
using __NAMESPACE__.Platform.Access;

namespace __NAMESPACE__.Health.Application.HealthChecks.RecordCheckIn;

/// <summary>
/// Declares who may record a check-in: the signed-in person.
/// </summary>
internal sealed class RecordCheckInCommandAuthorizationDefinition : IAuthorizationDefinition<RecordCheckInCommand>
{
    /// <inheritdoc />
    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(AccessPolicies.SignedIn);
}
