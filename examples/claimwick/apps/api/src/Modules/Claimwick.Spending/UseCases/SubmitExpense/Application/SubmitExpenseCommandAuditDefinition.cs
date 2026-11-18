using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.UseCases.SubmitExpense.Application;

/// <summary>Declares the submit-expense command exempt from auditing.</summary>
internal sealed class SubmitExpenseCommandAuditDefinition : IAuditDefinition<SubmitExpenseCommand>
{
    public AuditDeclaration Audit { get; } = AuditDeclaration.Exempt("Submissions leave their trail in the expense states and events.");
}
