using Claimwick.Application.Spending.Expenses.ErasePersonalData;
using LiteBus.Commands.Abstractions;

namespace Claimwick.WebApi.Endpoints.Spending.Expenses.ErasePersonalData;

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
        await mediator.SendAsync(new ErasePersonalDataCommand(expenseId), cancellationToken);

        return TypedResults.NoContent();
    }
}
