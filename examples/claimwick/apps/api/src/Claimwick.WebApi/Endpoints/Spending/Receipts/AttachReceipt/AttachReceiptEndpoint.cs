using Claimwick.Application.Spending.Receipts.AttachReceipt;
using LiteBus.Commands.Abstractions;

namespace Claimwick.WebApi.Endpoints.Spending.Receipts.AttachReceipt;

/// <implements>use-case/spending.attach-receipt</implements>
internal sealed class AttachReceiptEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/receipts", HandleAsync)
            .WithName("AttachReceipt")
            .WithSummary("Attach a receipt to a submitted expense.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<NoContent> HandleAsync(
        Guid receiptId,
        Guid expenseId,
        ICommandMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.SendAsync(new AttachReceiptCommand(receiptId, expenseId), cancellationToken);

        return TypedResults.NoContent();
    }
}
