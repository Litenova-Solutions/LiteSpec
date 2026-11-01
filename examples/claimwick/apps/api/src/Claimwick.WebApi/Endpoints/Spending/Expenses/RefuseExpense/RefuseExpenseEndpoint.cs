using System.Security.Claims;
using Claimwick.Application.Spending.Expenses.RefuseExpense;
using LiteBus.Commands.Abstractions;

namespace Claimwick.WebApi.Endpoints.Spending.Expenses.RefuseExpense;

/// <implements>use-case/spending.refuse-expense</implements>
internal sealed class RefuseExpenseEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/expenses/{expenseId}:refuse", HandleAsync)
            .WithName("RefuseExpense")
            .WithSummary("Refuse a submitted expense with a reason.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<NoContent> HandleAsync(
        Guid expenseId,
        string reason,
        ClaimsPrincipal user,
        ICommandMediator mediator,
        CancellationToken cancellationToken)
    {
        // The approver is the signed-in manager, read from verified claims, never from the request body.
        var approverId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await mediator.SendAsync(new RefuseExpenseCommand(expenseId, approverId, reason), cancellationToken);

        return TypedResults.NoContent();
    }
}
