using Claimwick.Spending.Domain.Expense;
using Marten;

namespace Claimwick.Spending.Infrastructure.Persistence;

/// <summary>Loads and stages expenses through the scoped document session.</summary>
/// <implements>aggregate/spending.expense</implements>
internal sealed class ExpenseRepository : IExpenseRepository
{
    private readonly IDocumentSession _session;

    public ExpenseRepository(IDocumentSession session)
    {
        _session = session;
    }

    public async Task<Expense> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var expense = await _session.LoadAsync<Expense>(id, cancellationToken);
        return expense ?? throw new InvalidOperationException($"No expense with id '{id}'.");
    }

    public void Store(Expense expense)
    {
        _session.Store(expense);
    }

    public void Delete(Guid id)
    {
        _session.Delete<Expense>(id);
    }
}
