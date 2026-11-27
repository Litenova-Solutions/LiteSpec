using Claimwick.Platform.Http;
using Claimwick.Spending.Application.Expenses.ExportExpenses;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Claimwick.Spending.WebApi.Expenses.ExportExpenses;

/// <implements>use-case/spending.export-expenses</implements>
internal sealed class ExportExpensesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/expenses:export", HandleAsync)
            .WithName("ExportExpenses")
            .WithSummary("Export the approved expenses of the period as one file.")
            .Produces<ExportExpensesResponseModel>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<Ok<ExportExpensesResponseModel>> HandleAsync(
        IQueryMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.QueryAsync(new ExportExpensesQuery(), cancellationToken);

        return TypedResults.Ok(new ExportExpensesResponseModel(
            result.Rows
                .Select(row => new ExportedExpenseResponseItemModel(
                    row.ExpenseId.Value,
                    row.Amount,
                    row.Currency,
                    row.Approver,
                    row.ApprovedAt))
                .ToList()));
    }
}
