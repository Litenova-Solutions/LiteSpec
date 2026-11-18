using LiteBus.Queries.Abstractions;
using Marten;

namespace Claimwick.Spending.Application.ListExpenses;

/// <implements>use-case/spending.list-expenses</implements>
internal sealed class ListExpensesQueryHandler : IQueryHandler<ListExpensesQuery, ListExpensesQueryResult>
{
    private readonly IQuerySession _session;

    public ListExpensesQueryHandler(IQuerySession session)
    {
        _session = session;
    }

    public async Task<ListExpensesQueryResult> HandleAsync(
        ListExpensesQuery query,
        CancellationToken cancellationToken)
    {
        var expenses = await _session.Query<ExpenseSummary>()
            .Where(summary => summary.EmployeeId == query.EmployeeId)
            .OrderByDescending(summary => summary.SubmittedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        return new ListExpensesQueryResult(expenses);
    }
}
