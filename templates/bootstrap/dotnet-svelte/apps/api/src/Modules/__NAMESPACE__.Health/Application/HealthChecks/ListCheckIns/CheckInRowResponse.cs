using __NAMESPACE__.Health.Domain.HealthChecks;

namespace __NAMESPACE__.Health.Application.HealthChecks.ListCheckIns;

/// <summary>
/// One listed check-in: its identifier, the moment it was recorded, its note and its state.
/// </summary>
/// <param name="CheckInId">The check-in identifier.</param>
/// <param name="RecordedAt">The moment the check-in was recorded.</param>
/// <param name="Note">The note recorded with the check-in, if any.</param>
/// <param name="State">The state code the check-in is in.</param>
internal sealed record CheckInRowResponse(CheckInId CheckInId, DateTimeOffset RecordedAt, string? Note, string State);
