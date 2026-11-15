using Claimwick.Spending.Contracts.IntegrationEvents;
using Marten;

namespace Claimwick.Reporting.ReadModels.ReimbursedExpenses;

/// <summary>Clears the employee of a reimbursed expense when the spending module erases its personal data.</summary>
/// <implements>read-model/reporting.reimbursed-expense</implements>
/// <follows>litespec/core/automation#erase-a-read-model-personal-field-with-its-owner</follows>
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
