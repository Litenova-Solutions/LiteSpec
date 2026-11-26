using Claimwick.Platform.Http;
using Claimwick.Reporting.Application.ListReimbursedExpenses;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Claimwick.Reporting.WebApi.ListReimbursedExpenses;

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
                    expense.Id.Value,
                    expense.EmployeeId,
                    expense.Amount,
                    expense.Currency,
                    expense.ReimbursedAt))
                .ToList()));
    }
}
