using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.SubmitExpense;

/// <summary>Keys the submit-expense command by its request identity, returning the first result on a repeat.</summary>
internal sealed class SubmitExpenseCommandIdempotencyDefinition : IIdempotencyDefinition<SubmitExpenseCommand>
{
    public IdempotencyDeclaration Idempotency { get; } = IdempotencyDeclaration.Idempotent(
        static message => ((SubmitExpenseCommand)message).ExpenseId.ToString(),
        scope: "account",
        replayResult: true);
}
