using Claimwick.Spending.Contracts.Expenses.Events;
using Marten;

namespace Claimwick.Reporting.Application.ReadModels.ReimbursedExpenses;

/// <summary>Clears the employee of a reimbursed expense when the spending module erases its personal data.</summary>
/// <implements>read-model/reporting.reimbursed-expense</implements>
/// <follows>litespec/core/automation#erase-a-read-model-personal-field-with-its-owner</follows>
internal sealed class ReimbursedExpenseOnExpensePersonalDataErasedProjection : IEventHandler<ExpensePersonalDataErasedModuleEvent>
{
    private readonly IDocumentSession _session;

    public ReimbursedExpenseOnExpensePersonalDataErasedProjection(IDocumentSession session)
    {
        _session = session;
    }

    public async Task HandleAsync(ExpensePersonalDataErasedModuleEvent moduleEvent, CancellationToken cancellationToken)
    {
        var expense = await _session.LoadAsync<ReimbursedExpense>(moduleEvent.ExpenseId, cancellationToken);
        if (expense is null)
        {
            return;
        }

        _session.Store(expense with { EmployeeId = null });
        await _session.SaveChangesAsync(cancellationToken);
    }
}
