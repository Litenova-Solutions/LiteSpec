using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Expenses.ErasePersonalData;

/// <summary>Declares the erase-personal-data command idempotent through the expense state, since a repeat finds nothing to erase.</summary>
internal sealed class ErasePersonalDataCommandIdempotencyDefinition : IIdempotencyDefinition<ErasePersonalDataCommand>
{
    public IdempotencyDeclaration Idempotency { get; } = IdempotencyDeclaration.Idempotent(
        "The first call deletes the expense, so a repeat finds nothing to erase.");
}
