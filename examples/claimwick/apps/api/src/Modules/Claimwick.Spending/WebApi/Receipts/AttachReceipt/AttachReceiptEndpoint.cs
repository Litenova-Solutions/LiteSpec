using Claimwick.Platform.Http;
using Claimwick.Spending.Application.Receipts.AttachReceipt;
using Claimwick.Spending.Contracts.Expenses;
using LiteBus.Commands.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Claimwick.Spending.WebApi.Receipts.AttachReceipt;

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
        await mediator.SendAsync(new AttachReceiptCommand(receiptId, ExpenseId.From(expenseId), DateTimeOffset.UtcNow), cancellationToken);

        return TypedResults.NoContent();
    }
}
