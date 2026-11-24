using Claimwick.Platform.Access;
using Claimwick.Spending.Contracts.Expenses.ReadExpense;
using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Expenses.ReadExpense;

/// <summary>Authorizes the read-expense query from its declared permission.</summary>
internal sealed class ReadExpenseQueryAuthorizationDefinition : IAuthorizationDefinition<ReadExpenseQuery>
{
    public static Permission Policy { get; } = new(SpendingPermissions.ReadExpense);

    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(Policy.Id);
}
