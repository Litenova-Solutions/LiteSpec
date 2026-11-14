using Claimwick.Spending.Contracts.IntegrationEvents;
using Marten;

namespace Claimwick.Reporting.Views.ReimbursedExpenses;

/// <summary>Clears the employee of a reimbursed expense when the spending module erases its personal data.</summary>
/// <implements>view/reporting.reimbursed-expense</implements>
/// <follows>litespec/core/automation#erase-a-viewed-personal-field-with-its-owner</follows>
internal sealed class ReimbursedExpenseOnExpensePersonalDataErasedProjection(
    IDocumentSession session) : IEventHandler<ExpensePersonalDataErased>
{
    public async Task HandleAsync(ExpensePersonalDataErased integrationEvent, CancellationToken cancellationToken)
    {
        var expense = await session.LoadAsync<ReimbursedExpense>(integrationEvent.ExpenseId, cancellationToken);
        if (expense is null)
        {
            return;
        }

        session.Store(expense with { EmployeeId = null });
        await session.SaveChangesAsync(cancellationToken);
    }
}
