using LiteBus.Messaging.Abstractions.Tenancy;

namespace Claimwick.Spending.Application.Expenses.RefuseExpense;

/// <summary>Declares the refuse-expense command single-tenant.</summary>
internal sealed class RefuseExpenseCommandTenancyDefinition : ITenancyDefinition<RefuseExpenseCommand>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
