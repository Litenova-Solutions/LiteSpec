using Claimwick.Platform.Http;
using Claimwick.Spending.UseCases.ReimburseExpense.Application;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.UseCases.ReimburseExpense.WebApi;

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
        await mediator.SendAsync(new ReimburseExpenseCommand(expenseId, DateTimeOffset.UtcNow), cancellationToken);

        return TypedResults.NoContent();
    }
}
