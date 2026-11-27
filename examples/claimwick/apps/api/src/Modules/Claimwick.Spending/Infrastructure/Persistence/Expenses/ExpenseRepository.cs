using Claimwick.Spending.Contracts.Expenses;
using Claimwick.Spending.Domain.Expenses;
using Claimwick.Spending.Domain.Expenses.Parts;
using Marten;

namespace Claimwick.Spending.Infrastructure.Persistence.Expenses;

/// <summary>Loads and stages expenses through the scoped document session.</summary>
/// <implements>aggregate/spending.expense</implements>
internal sealed class ExpenseRepository : IExpenseRepository
{
    private readonly IDocumentSession _session;

    public ExpenseRepository(IDocumentSession session)
    {
        _session = session;
    }

    public async Task<Expense> GetAsync(ExpenseId id, CancellationToken cancellationToken)
    {
        var expense = await _session.LoadAsync<Expense>(id, cancellationToken);
        return expense ?? throw new InvalidOperationException($"No expense with id '{id}'.");
    }

    public void Store(Expense expense)
    {
        _session.Store(expense);
    }

    public void Delete(ExpenseId id)
    {
        _session.Delete<Expense>(id);
    }

    public async Task<IReadOnlyList<ExpenseId>> SelectExpiredAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddYears(-7);
        return await _session.Query<Expense>()
            .Where(expense => expense.State is ExpenseApprovedState && ((ExpenseApprovedState)expense.State).DecidedAt < cutoff
                || expense.State is ExpenseRefusedState && ((ExpenseRefusedState)expense.State).DecidedAt < cutoff
                || expense.State is ExpenseReimbursedState && ((ExpenseReimbursedState)expense.State).ReimbursedAt < cutoff)
            .Select(expense => expense.Id)
            .ToListAsync(cancellationToken);
    }
}
