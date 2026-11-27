using Claimwick.Spending.Contracts.Expenses.ReadExpense;
using LiteBus.Messaging.Abstractions.Tenancy;

namespace Claimwick.Spending.Application.Expenses.ReadExpense;

/// <summary>Declares the read-expense query single-tenant.</summary>
internal sealed class ReadExpenseQueryTenancyDefinition : ITenancyDefinition<ReadExpenseQuery>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
