using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.UseCases.RefuseExpense.Application;

/// <summary>Declares the refuse-expense command idempotent through the expense state, which refuses a second refusal.</summary>
internal sealed class RefuseExpenseCommandIdempotencyDefinition : IIdempotencyDefinition<RefuseExpenseCommand>
{
    public IdempotencyDeclaration Idempotency { get; } = IdempotencyDeclaration.Idempotent(
        "The first refusal moves the expense out of the submitted state, so the aggregate refuses a second refusal.");
}
