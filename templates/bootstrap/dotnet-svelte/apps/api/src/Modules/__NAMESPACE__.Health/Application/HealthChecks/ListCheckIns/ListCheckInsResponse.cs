namespace __NAMESPACE__.Health.Application.HealthChecks.ListCheckIns;

/// <summary>
/// The listed check-ins, newest first.
/// </summary>
/// <param name="CheckIns">The check-in rows.</param>
internal sealed record ListCheckInsResponse(IReadOnlyList<CheckInRowResponse> CheckIns);
