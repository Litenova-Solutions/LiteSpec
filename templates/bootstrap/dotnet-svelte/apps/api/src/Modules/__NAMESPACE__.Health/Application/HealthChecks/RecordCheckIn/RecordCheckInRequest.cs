namespace __NAMESPACE__.Health.Application.HealthChecks.RecordCheckIn;

/// <summary>
/// The posted check-in: the note recorded with it, if any.
/// </summary>
/// <param name="Note">The note recorded with the check-in, if any.</param>
internal sealed record RecordCheckInRequest(string? Note);
