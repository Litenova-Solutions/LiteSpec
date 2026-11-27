using LiteBus.Messaging.Abstractions.Tenancy;

namespace Claimwick.Spending.Application.Expenses.ReimburseExpense;

/// <summary>Declares the reimburse-expense command single-tenant.</summary>
internal sealed class ReimburseExpenseCommandTenancyDefinition : ITenancyDefinition<ReimburseExpenseCommand>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
