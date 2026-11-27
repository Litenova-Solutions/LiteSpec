namespace __NAMESPACE__.Health.Application.CheckIns.ReadCheckIns;

/// <summary>The listed check-ins, newest first.</summary>
/// <param name="CheckIns">The check-in rows.</param>
public sealed record ReadCheckInsResult(IReadOnlyList<CheckInRow> CheckIns);
