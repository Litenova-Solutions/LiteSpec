using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.UseCases.ReimburseExpense.Application;

/// <summary>Declares the reimburse-expense command idempotent through the expense state, which refuses a second payment.</summary>
internal sealed class ReimburseExpenseCommandIdempotencyDefinition : IIdempotencyDefinition<ReimburseExpenseCommand>
{
    public IdempotencyDeclaration Idempotency { get; } = IdempotencyDeclaration.Idempotent(
        "The first payment moves the expense to reimbursed, so the aggregate refuses a second payment.");
}
