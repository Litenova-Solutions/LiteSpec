using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.UseCases.RefuseExpense.Application;

/// <summary>Declares the refuse-expense command exempt from auditing.</summary>
internal sealed class RefuseExpenseCommandAuditDefinition : IAuditDefinition<RefuseExpenseCommand>
{
    public AuditDeclaration Audit { get; } = AuditDeclaration.Exempt("Refusals leave their trail in the expense states and events.");
}
