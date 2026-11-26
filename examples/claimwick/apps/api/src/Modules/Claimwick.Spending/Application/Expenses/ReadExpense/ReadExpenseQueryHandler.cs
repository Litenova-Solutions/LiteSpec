using Claimwick.Kernel.Values;
using Claimwick.Spending.Contracts.Expenses;
using Claimwick.Spending.Contracts.Expenses.ReadExpense;
using Claimwick.Spending.Domain.Expenses;
using Claimwick.Spending.Domain.Expenses.Parts;
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
            .Where(expense => expense.Id == query.Payout.ExpenseId)
            .FirstOrDefaultAsync(cancellationToken);
        if (expense is null
            || expense.Total.Amount != query.Payout.Amount
            || expense.Total.Currency != query.Payout.Currency)
        {
            return null;
        }
        var subtotals = expense.Lines
            .GroupBy(line => line.Category)
            .Select(group => new CategoryTotal(group.Key, new Money(group.Sum(line => line.Amount.Amount), expense.Total.Currency)))
            .ToList();
        return new ReadExpenseResult(expense.Id, StateName(expense.State), new ExpenseTotal(expense.Total.Amount, expense.Total.Currency), new ExpenseSubtotals(subtotals, expense.Total));
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
