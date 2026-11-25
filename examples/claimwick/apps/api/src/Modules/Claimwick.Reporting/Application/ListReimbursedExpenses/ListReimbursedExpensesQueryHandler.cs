using Claimwick.Reporting.Application.ReadModels.ReimbursedExpenses;
using Claimwick.Spending.Contracts.Expenses.ReadExpense;
using LiteBus.Queries.Abstractions;

namespace Claimwick.Reporting.Application.ListReimbursedExpenses;

/// <implements>use-case/reporting.list-reimbursed-expenses</implements>
/// <follows>litespec/backend/architecture#give-each-use-case-its-own-models</follows>
internal sealed class ListReimbursedExpensesQueryHandler : IQueryHandler<ListReimbursedExpensesQuery, ListReimbursedExpensesQueryResult>
{
    private readonly IReimbursedExpenseReadModel _readModel;
    private readonly IQueryMediator _queries;

    public ListReimbursedExpensesQueryHandler(IReimbursedExpenseReadModel readModel, IQueryMediator queries)
    {
        _readModel = readModel;
        _queries = queries;
    }

    public async Task<ListReimbursedExpensesQueryResult> HandleAsync(
        ListReimbursedExpensesQuery query,
        CancellationToken cancellationToken)
    {
        var expenses = await _readModel.ListAsync(cancellationToken);
        var confirmed = new List<ReimbursedExpenseRow>();

        foreach (var expense in expenses)
        {
            if (await _queries.QueryAsync(new ReadExpenseQuery(new ClaimedPayout(expense.Id, expense.Amount, expense.Currency)), cancellationToken) is not null)
            {
                confirmed.Add(new ReimbursedExpenseRow(expense.Id, expense.EmployeeId, expense.Amount, expense.Currency, expense.ReimbursedAt));
            }
        }

        return new ListReimbursedExpensesQueryResult(confirmed);
    }
}
