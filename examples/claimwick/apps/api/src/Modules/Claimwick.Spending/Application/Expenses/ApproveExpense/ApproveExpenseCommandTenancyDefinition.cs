using LiteBus.Messaging.Abstractions.Tenancy;

namespace Claimwick.Spending.Application.Expenses.ApproveExpense;

/// <summary>Declares the approve-expense command single-tenant.</summary>
internal sealed class ApproveExpenseCommandTenancyDefinition : ITenancyDefinition<ApproveExpenseCommand>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
