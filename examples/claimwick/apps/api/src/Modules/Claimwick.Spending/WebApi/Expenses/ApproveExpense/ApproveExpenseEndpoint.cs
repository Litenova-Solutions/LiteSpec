using System.Security.Claims;
using Claimwick.Platform.Http;
using Claimwick.Spending.Application.Expenses.ApproveExpense;
using Claimwick.Spending.Contracts.Expenses;
using LiteBus.Commands.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Claimwick.Spending.WebApi.Expenses.ApproveExpense;

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
        await mediator.SendAsync(new ApproveExpenseCommand(ExpenseId.From(expenseId), approverId, DateTimeOffset.UtcNow), cancellationToken);

        return TypedResults.NoContent();
    }
}
