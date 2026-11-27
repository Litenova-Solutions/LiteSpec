using LiteBus.Messaging.Abstractions.Tenancy;

namespace Claimwick.Spending.Application.Expenses.ExportExpenses;

/// <summary>Declares the export-expenses query single-tenant.</summary>
internal sealed class ExportExpensesQueryTenancyDefinition : ITenancyDefinition<ExportExpensesQuery>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
