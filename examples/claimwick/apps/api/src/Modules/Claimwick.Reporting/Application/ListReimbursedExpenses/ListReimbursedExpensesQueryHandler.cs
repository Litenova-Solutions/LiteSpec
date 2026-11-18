using Claimwick.Reporting.Application.ReadModels.ReimbursedExpenses;
using Claimwick.Reporting.Ports.Spending;
using LiteBus.Queries.Abstractions;

namespace Claimwick.Reporting.Application.ListReimbursedExpenses;

/// <implements>use-case/reporting.list-reimbursed-expenses</implements>
/// <follows>litespec/backend/architecture#give-each-use-case-its-own-models</follows>
internal sealed class ListReimbursedExpensesQueryHandler : IQueryHandler<ListReimbursedExpensesQuery, ListReimbursedExpensesQueryResult>
{
    private readonly IReimbursedExpenseReadModel _readModel;
    private readonly ISpending _spending;

    public ListReimbursedExpensesQueryHandler(IReimbursedExpenseReadModel readModel, ISpending spending)
    {
        _readModel = readModel;
        _spending = spending;
    }

    public async Task<ListReimbursedExpensesQueryResult> HandleAsync(
        ListReimbursedExpensesQuery query,
        CancellationToken cancellationToken)
    {
        var expenses = await _readModel.ListAsync(cancellationToken);
        var confirmed = new List<ReimbursedExpense>();

        foreach (var expense in expenses)
        {
            if (await _spending.ExpenseExistsAsync(expense.Id, cancellationToken))
            {
                confirmed.Add(expense);
            }
        }

        return new ListReimbursedExpensesQueryResult(confirmed);
    }
}
