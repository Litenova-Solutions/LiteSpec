using Marten;

namespace Claimwick.Reporting.Views.ReimbursedExpenses;

/// <summary>Reads the reimbursed expenses from the read session.</summary>
/// <implements>view/reporting.reimbursed-expense</implements>
/// <follows>litespec/backend/application#read-another-modules-state-through-a-view</follows>
internal sealed class ReimbursedExpenseView(IQuerySession session) : IReimbursedExpenseView
{
    public async Task<IReadOnlyList<ReimbursedExpense>> ListAsync(CancellationToken cancellationToken) =>
        await session.Query<ReimbursedExpense>()
            .OrderByDescending(expense => expense.ReimbursedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
}
