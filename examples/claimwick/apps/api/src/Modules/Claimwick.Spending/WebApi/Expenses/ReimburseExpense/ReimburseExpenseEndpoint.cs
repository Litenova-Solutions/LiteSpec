using Claimwick.Platform.Http;
using Claimwick.Spending.Application.Expenses.ReimburseExpense;
using Claimwick.Spending.Contracts.Expenses;
using LiteBus.Commands.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Claimwick.Spending.WebApi.Expenses.ReimburseExpense;

/// <implements>use-case/spending.reimburse-expense</implements>
internal sealed class ReimburseExpenseEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/expenses/{expenseId}:reimburse", HandleAsync)
            .WithName("ReimburseExpense")
            .WithSummary("Reimburse an approved expense.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<NoContent> HandleAsync(
        Guid expenseId,
        ICommandMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.SendAsync(new ReimburseExpenseCommand(ExpenseId.From(expenseId), DateTimeOffset.UtcNow), cancellationToken);

        return TypedResults.NoContent();
    }
}
