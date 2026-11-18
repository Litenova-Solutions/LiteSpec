using Claimwick.Spending.Contracts.ModuleEvents;
using Marten;

namespace Claimwick.Reporting.ReadModels.ReimbursedExpenses;

/// <summary>Clears the employee of a reimbursed expense when the spending module erases its personal data.</summary>
/// <implements>read-model/reporting.reimbursed-expense</implements>
/// <follows>litespec/core/automation#erase-a-read-model-personal-field-with-its-owner</follows>
internal sealed class ReimbursedExpenseOnExpensePersonalDataErasedProjection : IEventHandler<ExpensePersonalDataErased>
{
    private readonly IDocumentSession _session;

    public ReimbursedExpenseOnExpensePersonalDataErasedProjection(IDocumentSession session)
    {
        _session = session;
    }

    public async Task HandleAsync(ExpensePersonalDataErased moduleEvent, CancellationToken cancellationToken)
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
