using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.AttachReceipt;

/// <summary>Declares the attach-receipt command idempotent through the receipt state, which keeps the same receipt attached.</summary>
internal sealed class AttachReceiptCommandIdempotencyDefinition : IIdempotencyDefinition<AttachReceiptCommand>
{
    public IdempotencyDeclaration Idempotency { get; } = IdempotencyDeclaration.Idempotent(
        "A repeat attaches the same receipt to the same submitted expense, leaving the same receipt attached.");
}
