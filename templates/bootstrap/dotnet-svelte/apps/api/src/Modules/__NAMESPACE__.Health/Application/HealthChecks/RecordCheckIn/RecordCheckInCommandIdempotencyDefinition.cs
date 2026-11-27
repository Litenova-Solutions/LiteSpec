using LiteBus.Messaging.Abstractions;

namespace __NAMESPACE__.Health.Application.HealthChecks.RecordCheckIn;

/// <summary>
/// Declares the idempotency of recording a check-in: the command keys on its
/// check-in identifier, so a repeated send with the same identifier is
/// recognized as the same command.
/// </summary>
internal sealed class RecordCheckInCommandIdempotencyDefinition : IIdempotencyDefinition<RecordCheckInCommand>
{
    /// <inheritdoc />
    public IdempotencyDeclaration Idempotency =>
        IdempotencyDeclaration.KeyedBy<RecordCheckInCommand>(command => command.CheckInId.Value.ToString());
}
