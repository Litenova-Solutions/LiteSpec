using Claimwick.Reporting.Views.ReimbursedExpenses;
using LiteBus.Queries.Abstractions;

namespace Claimwick.Reporting.UseCases.ListReimbursedExpenses.Application;

/// <implements>use-case/reporting.list-reimbursed-expenses</implements>
/// <follows>litespec/backend/architecture#give-each-use-case-its-own-models</follows>
internal sealed class ListReimbursedExpensesQueryHandler(
    IReimbursedExpenseView view) : IQueryHandler<ListReimbursedExpensesQuery, ListReimbursedExpensesQueryResult>
{
    public async Task<ListReimbursedExpensesQueryResult> HandleAsync(
        ListReimbursedExpensesQuery query,
        CancellationToken cancellationToken) =>
        new(await view.ListAsync(cancellationToken));
}
