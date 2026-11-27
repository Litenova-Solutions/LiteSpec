using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Expenses.ReimburseExpense;

/// <summary>Declares the reimburse-expense command exempt from auditing.</summary>
internal sealed class ReimburseExpenseCommandAuditDefinition : IAuditDefinition<ReimburseExpenseCommand>
{
    public AuditDeclaration Audit { get; } = AuditDeclaration.Exempt("Payments leave their trail in the expense states and the payout batch.");
}
