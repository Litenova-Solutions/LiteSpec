using Claimwick.Platform.Access;
using LiteBus.Messaging.Abstractions;

namespace Claimwick.Reporting.Application.ListReimbursedExpenses;

/// <summary>Authorizes the list-reimbursed-expenses query from its declared permission.</summary>
internal sealed class ListReimbursedExpensesQueryAuthorizationDefinition : IAuthorizationDefinition<ListReimbursedExpensesQuery>
{
    public static Permission Policy { get; } = new(ReportingPermissions.ListReimbursedExpenses);

    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(Policy.Id);
}
