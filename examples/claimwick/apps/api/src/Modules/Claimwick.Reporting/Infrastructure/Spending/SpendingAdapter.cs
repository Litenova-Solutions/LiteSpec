using Claimwick.Reporting.Ports.Spending;
using Claimwick.Spending.Contracts.Expenses.ReadExpense;
using LiteBus.Queries.Abstractions;

namespace Claimwick.Reporting.Infrastructure.Spending;

/// <summary>Sends the spending module's public queries and maps their results to Reporting's answers.</summary>
/// <follows>litespec/backend/application#reach-another-module-through-one-port-per-provider</follows>
internal sealed class SpendingAdapter : ISpending
{
    private readonly IQueryMediator _mediator;

    public SpendingAdapter(IQueryMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Confirms one expense still exists in the spending module.</summary>
    public async Task<bool> ExpenseExistsAsync(Guid expenseId, CancellationToken cancellationToken)
    {
        var result = await _mediator.QueryAsync(new ReadExpenseQuery(expenseId), cancellationToken);
        return result is not null;
    }
}
