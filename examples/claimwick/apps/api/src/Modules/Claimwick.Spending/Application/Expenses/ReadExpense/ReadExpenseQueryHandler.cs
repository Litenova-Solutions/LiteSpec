using Claimwick.Spending.Contracts.Expenses.ReadExpense;
using Claimwick.Spending.Domain.Expenses;
using LiteBus.Queries.Abstractions;
using Marten;

namespace Claimwick.Spending.Application.Expenses.ReadExpense;

/// <implements>use-case/spending.read-expense</implements>
internal sealed class ReadExpenseQueryHandler : IQueryHandler<ReadExpenseQuery, ReadExpenseResult?>
{
    private readonly IQuerySession _session;

    public ReadExpenseQueryHandler(IQuerySession session)
    {
        _session = session;
    }

    public async Task<ReadExpenseResult?> HandleAsync(
        ReadExpenseQuery query,
        CancellationToken cancellationToken)
    {
        var expense = await _session.Query<Expense>()
            .Where(expense => expense.Id == query.ExpenseId)
            .FirstOrDefaultAsync(cancellationToken);
        return expense is null ? null : new ReadExpenseResult(expense.Id, StateName(expense.State));
    }

    private static string StateName(ExpenseState state) => state switch
    {
        ExpenseSubmittedState => "Submitted",
        ExpenseApprovedState => "Approved",
        ExpenseRefusedState => "Refused",
        ExpenseReimbursedState => "Reimbursed",
        ExpenseErasedState => "Erased",
        _ => throw new InvalidOperationException($"Unknown expense state '{state.GetType().Name}'."),
    };
}
