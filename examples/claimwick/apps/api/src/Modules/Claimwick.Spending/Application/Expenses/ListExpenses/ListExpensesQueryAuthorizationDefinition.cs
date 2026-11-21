using Claimwick.Platform.Access;
using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Expenses.ListExpenses;

/// <summary>Authorizes the list-expenses query from its declared permission.</summary>
internal sealed class ListExpensesQueryAuthorizationDefinition : IAuthorizationDefinition<ListExpensesQuery>
{
    public static Permission Policy { get; } = new("permission/list-expenses");

    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(Policy.Id);
}
