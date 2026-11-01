using Claimwick.Application.Spending.Policies.ChangeReceiptThreshold;
using LiteBus.Commands.Abstractions;

namespace Claimwick.WebApi.Endpoints.Spending.Policies.ChangeReceiptThreshold;

/// <implements>use-case/spending.change-receipt-threshold</implements>
internal sealed class ChangeReceiptThresholdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/policies/receipt-threshold", HandleAsync)
            .WithName("ChangeReceiptThreshold")
            .WithSummary("Change the receipt threshold.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<NoContent> HandleAsync(
        decimal threshold,
        string currency,
        ICommandMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.SendAsync(new ChangeReceiptThresholdCommand(threshold, currency), cancellationToken);

        return TypedResults.NoContent();
    }
}
