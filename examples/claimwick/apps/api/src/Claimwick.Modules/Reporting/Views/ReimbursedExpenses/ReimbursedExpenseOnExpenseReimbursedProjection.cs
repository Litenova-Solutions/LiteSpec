using Claimwick.Spending.Contracts.IntegrationEvents;
using Marten;

namespace Claimwick.Reporting.Views.ReimbursedExpenses;

/// <summary>Adds each reimbursed expense to the view when the spending module announces it.</summary>
/// <implements>view/reporting.reimbursed-expense</implements>
/// <follows>litespec/core/automation#declare-each-view-field-from-a-published-field</follows>
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
