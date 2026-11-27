using Claimwick.Platform.Access;
using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Expenses.ReimburseExpense;

/// <summary>Authorizes the reimburse-expense command from its declared permission, with a sign-in younger than 15 minutes.</summary>
internal sealed class ReimburseExpenseCommandAuthorizationDefinition : IAuthorizationDefinition<ReimburseExpenseCommand>
{
    public static RecentSignIn Policy { get; } = new($"RecentSignIn({SpendingPermissions.PostReimbursement},PT15M)");

    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(Policy.Id);
}
