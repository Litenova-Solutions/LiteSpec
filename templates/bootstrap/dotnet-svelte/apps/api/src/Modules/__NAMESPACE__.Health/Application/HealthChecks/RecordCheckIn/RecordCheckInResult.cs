using __NAMESPACE__.Health.Domain.HealthChecks;

namespace __NAMESPACE__.Health.Application.HealthChecks.RecordCheckIn;

/// <summary>
/// The recorded check-in: its identifier with the state the record call left it in.
/// </summary>
/// <param name="CheckInId">The recorded check-in.</param>
/// <param name="State">The state code the record call left the check-in in.</param>
internal sealed record RecordCheckInResult(CheckInId CheckInId, string State);
