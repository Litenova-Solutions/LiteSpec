using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Policies.ChangeReceiptThreshold;

/// <summary>Declares the change-receipt-threshold command idempotent through the policy state, which keeps the same value in place.</summary>
internal sealed class ChangeReceiptThresholdCommandIdempotencyDefinition : IIdempotencyDefinition<ChangeReceiptThresholdCommand>
{
    public IdempotencyDeclaration Idempotency { get; } = IdempotencyDeclaration.Idempotent(
        "A repeat sets the same threshold on the policy, leaving the same value in place.");
}
