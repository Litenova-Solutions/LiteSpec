using LiteBus.Messaging.Abstractions.Tenancy;

namespace Claimwick.Spending.Application.Expenses.ListExpenses;

/// <summary>Declares the list-expenses query single-tenant.</summary>
internal sealed class ListExpensesQueryTenancyDefinition : ITenancyDefinition<ListExpensesQuery>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
