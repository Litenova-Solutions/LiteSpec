using Marten;

namespace Claimwick.Reporting.ReadModels.ReimbursedExpenses;

/// <summary>Reads the reimbursed expenses from the read session.</summary>
/// <implements>read-model/reporting.reimbursed-expense</implements>
/// <follows>litespec/backend/application#read-another-modules-state-through-a-read-model</follows>
internal sealed class ReimbursedExpenseReadModel : IReimbursedExpenseReadModel
{
    private readonly IQuerySession _session;

    public ReimbursedExpenseReadModel(IQuerySession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<ReimbursedExpense>> ListAsync(CancellationToken cancellationToken) =>
        await _session.Query<ReimbursedExpense>()
            .OrderByDescending(expense => expense.ReimbursedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
}
