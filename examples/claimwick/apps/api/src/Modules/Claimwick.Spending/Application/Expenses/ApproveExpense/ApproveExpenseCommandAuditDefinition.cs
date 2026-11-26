using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Expenses.ApproveExpense;

/// <summary>Declares the approve-expense command exempt from auditing.</summary>
internal sealed class ApproveExpenseCommandAuditDefinition : IAuditDefinition<ApproveExpenseCommand>
{
    public AuditDeclaration Audit { get; } = AuditDeclaration.Exempt("Approvals leave their trail in the expense states and events.");
}
