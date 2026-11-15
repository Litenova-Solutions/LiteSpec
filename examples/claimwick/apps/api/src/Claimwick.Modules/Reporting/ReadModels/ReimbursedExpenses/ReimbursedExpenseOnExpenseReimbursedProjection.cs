using Claimwick.Spending.Contracts.IntegrationEvents;
using Marten;

namespace Claimwick.Reporting.ReadModels.ReimbursedExpenses;

/// <summary>Adds each reimbursed expense to the read model when the spending module announces it.</summary>
/// <implements>read-model/reporting.reimbursed-expense</implements>
/// <follows>litespec/core/automation#declare-each-read-model-field-from-a-published-field</follows>
internal sealed class ReimbursedExpenseOnExpenseReimbursedProjection(
    IDocumentSession session) : IEventHandler<ExpenseReimbursed>
{
    public async Task HandleAsync(ExpenseReimbursed integrationEvent, CancellationToken cancellationToken)
    {
        session.Store(new ReimbursedExpense(
            integrationEvent.ExpenseId,
            integrationEvent.EmployeeId,
            integrationEvent.Amount,
            integrationEvent.Currency,
            integrationEvent.ReimbursedAt));
        await session.SaveChangesAsync(cancellationToken);
    }
}
