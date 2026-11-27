using LiteBus.Messaging.Abstractions.Tenancy;

namespace Claimwick.Reporting.Application.ListReimbursedExpenses;

/// <summary>Declares the list-reimbursed-expenses query single-tenant.</summary>
internal sealed class ListReimbursedExpensesQueryTenancyDefinition : ITenancyDefinition<ListReimbursedExpensesQuery>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
