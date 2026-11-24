using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Receipts.AttachReceipt;

/// <summary>Keys the attach-receipt command by its receipt identity, returning the first result on a repeat.</summary>
internal sealed class AttachReceiptCommandIdempotencyDefinition : IIdempotencyDefinition<AttachReceiptCommand>
{
    public IdempotencyDeclaration Idempotency { get; } = IdempotencyDeclaration.Idempotent(
        static message => ((AttachReceiptCommand)message).ReceiptId.ToString(),
        scope: "account",
        replayResult: true);
}
