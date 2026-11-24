using Claimwick.Platform.Access;
using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Policies.ChangeReceiptThreshold;

/// <summary>Authorizes the change-receipt-threshold command from its declared permission.</summary>
internal sealed class ChangeReceiptThresholdCommandAuthorizationDefinition : IAuthorizationDefinition<ChangeReceiptThresholdCommand>
{
    public static Permission Policy { get; } = new(SpendingPermissions.ManagePolicy);

    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(Policy.Id);
}
