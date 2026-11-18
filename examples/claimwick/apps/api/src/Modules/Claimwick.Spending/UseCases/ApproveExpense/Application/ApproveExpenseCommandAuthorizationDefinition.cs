using Claimwick.Platform.Access;
using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.UseCases.ApproveExpense.Application;

/// <summary>Authorizes the approve-expense command from its declared permission.</summary>
internal sealed class ApproveExpenseCommandAuthorizationDefinition : IAuthorizationDefinition<ApproveExpenseCommand>
{
    public static Permission Policy { get; } = new("permission/approve-expense");

    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(Policy.Id);
}
