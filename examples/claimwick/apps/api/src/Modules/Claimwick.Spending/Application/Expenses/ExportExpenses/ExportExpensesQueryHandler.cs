using Claimwick.Spending.Domain.Expenses;
using LiteBus.Queries.Abstractions;
using Marten;

namespace Claimwick.Spending.Application.Expenses.ExportExpenses;

/// <implements>use-case/spending.export-expenses</implements>
internal sealed class ExportExpensesQueryHandler : IQueryHandler<ExportExpensesQuery, ExportExpensesQueryResult>
{
    private readonly IQuerySession _session;

    public ExportExpensesQueryHandler(IQuerySession session)
    {
        _session = session;
    }

    public async Task<ExportExpensesQueryResult> HandleAsync(
        ExportExpensesQuery query,
        CancellationToken cancellationToken)
    {
        var approved = await _session.Query<Expense>()
            .Where(expense => expense.State is ExpenseApprovedState)
            .ToListAsync(cancellationToken);
        Expense.RequireApprovedForExport(approved.Count);

        var rows = new List<ExportedExpenseRow>();

        foreach (var expense in approved)
        {
            var state = (ExpenseApprovedState)expense.State;
            rows.Add(new ExportedExpenseRow(
                expense.Id,
                expense.Total.Amount,
                expense.Total.Currency,
                state.ApprovedBy,
                state.DecidedAt));
        }

        return new ExportExpensesQueryResult(rows);
    }
}
