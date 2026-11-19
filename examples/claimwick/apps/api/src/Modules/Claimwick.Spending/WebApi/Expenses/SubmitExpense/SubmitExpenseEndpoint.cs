using System.Security.Claims;
using Claimwick.Platform.Http;
using Claimwick.Spending.Application.Expenses.SubmitExpense;
using Claimwick.Spending.Contracts.Expenses;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.WebApi.Expenses.SubmitExpense;

/// <implements>use-case/spending.submit-expense</implements>
internal sealed class SubmitExpenseEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/expenses", HandleAsync)
            .WithName("SubmitExpense")
            .WithSummary("Submit a work expense.")
            .Produces<SubmitExpenseResponseModel>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<Results<Created<SubmitExpenseResponseModel>, ProblemHttpResult>> HandleAsync(
        SubmitExpenseRequestModel request,
        ClaimsPrincipal user,
        ICommandMediator mediator,
        CancellationToken cancellationToken)
    {
        // The submitter is the signed-in employee, read from verified claims, never from the request body.
        var employeeId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await mediator.SendAsync(
            new SubmitExpenseCommand(ExpenseId.From(Guid.CreateVersion7()), employeeId, request.Amount, request.Currency, request.ReceiptId),
            cancellationToken);

        return TypedResults.Created(
            $"/api/expenses/{result.ExpenseId}",
            new SubmitExpenseResponseModel(result.ExpenseId.Value));
    }
}
