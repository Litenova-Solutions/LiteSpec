using Claimwick.Platform.Http;
using Claimwick.Spending.Application.Expenses.ErasePersonalData;
using Claimwick.Spending.Contracts.Expenses;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.WebApi.Expenses.ErasePersonalData;

/// <implements>use-case/spending.erase-personal-data</implements>
internal sealed class ErasePersonalDataEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/expenses/{expenseId}:erase", HandleAsync)
            .WithName("ErasePersonalData")
            .WithSummary("Erase the personal data of a decided expense.")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static async Task<NoContent> HandleAsync(
        Guid expenseId,
        ICommandMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.SendAsync(new ErasePersonalDataCommand(ExpenseId.From(expenseId)), cancellationToken);

        return TypedResults.NoContent();
    }
}
