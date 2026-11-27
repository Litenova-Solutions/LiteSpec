using LiteBus.Messaging.Abstractions.Tenancy;

namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

/// <summary>Declares the submit-expense command single-tenant.</summary>
internal sealed class SubmitExpenseCommandTenancyDefinition : ITenancyDefinition<SubmitExpenseCommand>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
