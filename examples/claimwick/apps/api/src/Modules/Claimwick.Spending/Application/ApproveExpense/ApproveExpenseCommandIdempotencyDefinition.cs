using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.ApproveExpense;

/// <summary>Declares the approve-expense command idempotent through the expense state, which refuses a second approval.</summary>
internal sealed class ApproveExpenseCommandIdempotencyDefinition : IIdempotencyDefinition<ApproveExpenseCommand>
{
    public IdempotencyDeclaration Idempotency { get; } = IdempotencyDeclaration.Idempotent(
        "The first approval moves the expense out of the submitted state, so the aggregate refuses a second approval.");
}
