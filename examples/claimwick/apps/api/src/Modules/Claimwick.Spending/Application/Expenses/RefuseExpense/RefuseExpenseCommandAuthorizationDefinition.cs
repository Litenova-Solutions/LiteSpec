using Claimwick.Platform.Access;
using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Expenses.RefuseExpense;

/// <summary>Authorizes the refuse-expense command from its declared permission.</summary>
internal sealed class RefuseExpenseCommandAuthorizationDefinition : IAuthorizationDefinition<RefuseExpenseCommand>
{
    public static Permission Policy { get; } = new(SpendingPermissions.ApproveExpense);

    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(Policy.Id);
}
