using LiteBus.Queries.Abstractions;
using Claimwick.Reporting.UseCases.ListReimbursedExpenses.Application;

namespace Claimwick.Reporting.UseCases.ListReimbursedExpenses.WebApi;

/// <implements>use-case/reporting.list-reimbursed-expenses</implements>
internal sealed class ListReimbursedExpensesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/reimbursed-expenses", HandleAsync)
            .WithName("ListReimbursedExpenses")
            .WithSummary("List the reimbursed expenses with the employee each one was paid to, newest first.")
            .Produces<ListReimbursedExpensesResponseModel>(StatusCodes.Status200OK);
    }

    private static async Task<Ok<ListReimbursedExpensesResponseModel>> HandleAsync(
        IQueryMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.QueryAsync(new ListReimbursedExpensesQuery(), cancellationToken);

        return TypedResults.Ok(new ListReimbursedExpensesResponseModel(
            result.Expenses
                .Select(expense => new ReimbursedExpenseResponseItemModel(
                    expense.Id,
                    expense.EmployeeId,
                    expense.Amount,
                    expense.Currency,
                    expense.ReimbursedAt))
                .ToList()));
    }
}
