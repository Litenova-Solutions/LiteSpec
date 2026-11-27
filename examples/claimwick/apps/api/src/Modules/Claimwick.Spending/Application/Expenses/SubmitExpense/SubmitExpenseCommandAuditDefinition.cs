using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

/// <summary>Declares the submit-expense command audited, with the created expense as its target.</summary>
internal sealed class SubmitExpenseCommandAuditDefinition : IAuditDefinition<SubmitExpenseCommand>
{
    public AuditDeclaration Audit { get; } = AuditDeclaration.Audited("use-case/spending.submit-expense")
        .WithTargetFrom<SubmitExpenseCommand>(command => command.ExpenseId.ToString());
}
