using System.Security.Claims;
using Claimwick.Application.Spending.Expenses.ApproveExpense;
using LiteBus.Commands.Abstractions;

namespace Claimwick.WebApi.Endpoints.Spending.Expenses.ApproveExpense;

/// <implements>use-case/spending.approve-expense</implements>
internal sealed class ApproveExpenseEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/expenses/{expenseId}:approve", HandleAsync)
            .WithName("ApproveExpense")
            .WithSummary("Approve a submitted expense.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<NoContent> HandleAsync(
        Guid expenseId,
        ClaimsPrincipal user,
        ICommandMediator mediator,
        CancellationToken cancellationToken)
    {
        // The approver is the signed-in manager, read from verified claims, never from the request body.
        var approverId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await mediator.SendAsync(new ApproveExpenseCommand(expenseId, approverId), cancellationToken);

        return TypedResults.NoContent();
    }
}
