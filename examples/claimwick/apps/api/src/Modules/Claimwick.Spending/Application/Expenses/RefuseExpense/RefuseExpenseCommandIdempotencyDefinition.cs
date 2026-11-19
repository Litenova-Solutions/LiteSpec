using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Expenses.RefuseExpense;

/// <summary>Declares that refuse expense needs no idempotency key: the command recognizes a repeat by state, since a stored key would outlive that state.</summary>
internal sealed class RefuseExpenseCommandIdempotencyDefinition : IMessageDefinition<RefuseExpenseCommand>
{
    /// <inheritdoc />
    public void Describe(IMessageDeclarations declarations)
    {
        ArgumentNullException.ThrowIfNull(declarations);
        declarations.Exempt<IdempotencyDeclaration>("recognized by state: the aggregate state already holds the outcome this command sets");
    }
}
