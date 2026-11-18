using Claimwick.Spending.Contracts.Expenses.Events;
using Marten;

namespace Claimwick.Reporting.Application.ReadModels.ReimbursedExpenses;

/// <summary>Adds each reimbursed expense to the read model when the spending module announces it.</summary>
/// <implements>read-model/reporting.reimbursed-expense</implements>
/// <follows>litespec/core/automation#declare-each-read-model-field-from-a-published-field</follows>
internal sealed class ReimbursedExpenseOnExpenseReimbursedProjection : IEventHandler<ExpenseReimbursedModuleEvent>
{
    private readonly IDocumentSession _session;

    public ReimbursedExpenseOnExpenseReimbursedProjection(IDocumentSession session)
    {
        _session = session;
    }

    public async Task HandleAsync(ExpenseReimbursedModuleEvent moduleEvent, CancellationToken cancellationToken)
    {
        _session.Store(new ReimbursedExpense(
            moduleEvent.ExpenseId,
            moduleEvent.EmployeeId,
            moduleEvent.Amount,
            moduleEvent.Currency,
            moduleEvent.ReimbursedAt));
        await _session.SaveChangesAsync(cancellationToken);
    }
}
