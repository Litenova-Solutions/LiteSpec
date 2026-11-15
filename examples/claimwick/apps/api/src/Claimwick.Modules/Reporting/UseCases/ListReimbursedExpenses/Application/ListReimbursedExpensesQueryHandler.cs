using Claimwick.Reporting.ReadModels.ReimbursedExpenses;
using LiteBus.Queries.Abstractions;

namespace Claimwick.Reporting.UseCases.ListReimbursedExpenses.Application;

/// <implements>use-case/reporting.list-reimbursed-expenses</implements>
/// <follows>litespec/backend/architecture#give-each-use-case-its-own-models</follows>
internal sealed class ListReimbursedExpensesQueryHandler(
    IReimbursedExpenseReadModel readModel) : IQueryHandler<ListReimbursedExpensesQuery, ListReimbursedExpensesQueryResult>
{
    public async Task<ListReimbursedExpensesQueryResult> HandleAsync(
        ListReimbursedExpensesQuery query,
        CancellationToken cancellationToken) =>
        new(await readModel.ListAsync(cancellationToken));
}
