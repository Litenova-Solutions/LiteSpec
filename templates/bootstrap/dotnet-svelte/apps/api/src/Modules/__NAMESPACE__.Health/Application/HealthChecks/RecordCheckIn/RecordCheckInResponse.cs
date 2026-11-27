using __NAMESPACE__.Health.Domain.HealthChecks;

namespace __NAMESPACE__.Health.Application.HealthChecks.RecordCheckIn;

/// <summary>
/// The recorded check-in: its identifier with its state.
/// </summary>
/// <param name="CheckInId">The recorded check-in.</param>
/// <param name="State">The state code the record call left the check-in in.</param>
internal sealed record RecordCheckInResponse(CheckInId CheckInId, string State);
