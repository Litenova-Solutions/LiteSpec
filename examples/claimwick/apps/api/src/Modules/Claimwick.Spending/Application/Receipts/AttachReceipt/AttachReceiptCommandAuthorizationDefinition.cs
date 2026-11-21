using Claimwick.Platform.Access;
using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Receipts.AttachReceipt;

/// <summary>Authorizes the attach-receipt command from its declared permission.</summary>
internal sealed class AttachReceiptCommandAuthorizationDefinition : IAuthorizationDefinition<AttachReceiptCommand>
{
    public static Permission Policy { get; } = new("permission/request-expense");

    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(Policy.Id);
}
