using LiteBus.Commands.Abstractions;
using __NAMESPACE__.Health.Domain.HealthChecks;

namespace __NAMESPACE__.Health.Application.HealthChecks.RecordCheckIn;

/// <summary>
/// Records a check-in with the moment and note the caller gives.
/// </summary>
/// <param name="CheckInId">The check-in identifier.</param>
/// <param name="RecordedAt">The moment the check-in was recorded.</param>
/// <param name="Note">The note recorded with the check-in, if any.</param>
internal sealed record RecordCheckInCommand(CheckInId CheckInId, DateTimeOffset RecordedAt, string? Note)
    : ICommand<RecordCheckInResult>;
