using Claimwick.Platform.Access;
using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Expenses.ExportExpenses;

/// <summary>Authorizes the export-expenses query from its declared permission.</summary>
internal sealed class ExportExpensesQueryAuthorizationDefinition : IAuthorizationDefinition<ExportExpensesQuery>
{
    public static Permission Policy { get; } = new(SpendingPermissions.ExportExpenses);

    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(Policy.Id);
}
