using Claimwick.Platform.Access;
using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.UseCases.ChangeReceiptThreshold.Application;

/// <summary>Authorizes the change-receipt-threshold command from its declared permission.</summary>
internal sealed class ChangeReceiptThresholdCommandAuthorizationDefinition : IAuthorizationDefinition<ChangeReceiptThresholdCommand>
{
    public static Permission Policy { get; } = new("permission/manage-policy");

    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(Policy.Id);
}
